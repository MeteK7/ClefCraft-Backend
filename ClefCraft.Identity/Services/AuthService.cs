using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Identity.Models;
using ClefCraft.Application.Exceptions;
using ClefCraft.Identity.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtSettings _jwtSettings;
        private readonly ClefCraftIdentityDbContext _context;

        public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IOptions<JwtSettings> jwtSettings, ClefCraftIdentityDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtSettings = jwtSettings.Value;
            _context = context;
        }
        public async Task<AuthResponse> Login(AuthRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                throw new NotFoundException($"User with {request.Email} not found.", request.Email);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (result.Succeeded == false)
            {
                throw new BadRequestException($"Credentials for '{request.Email} aren't valid'.");
            }

            // Keep the table bounded without a background job: a user's expired tokens are
            // useless (revoked or not), so drop them whenever that user signs in again.
            var utcNow = DateTime.UtcNow;
            var expired = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id && t.ExpiresAt <= utcNow)
                .ToListAsync();
            _context.RefreshTokens.RemoveRange(expired);

            var sessionExpiresAt = utcNow.AddHours(_jwtSettings.SessionMaxHours);
            var (rawRefreshToken, refreshToken) = CreateRefreshToken(user.Id, utcNow, sessionExpiresAt);
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return await BuildAuthResponse(user, rawRefreshToken, refreshToken);
        }

        public async Task<AuthResponse?> Refresh(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            var utcNow = DateTime.UtcNow;
            var tokenHash = HashToken(refreshToken);
            var existing = await _context.RefreshTokens
                .SingleOrDefaultAsync(t => t.TokenHash == tokenHash);

            // ExpiresAt is the idle deadline (never later than the session cap); an idle-expired
            // or capped session must log in again even if the token was never revoked.
            if (existing == null || existing.ExpiresAt <= utcNow || existing.SessionExpiresAt <= utcNow)
                return null;

            if (existing.RevokedAt != null)
            {
                // A rotated-away token was presented again: either it was stolen or the client
                // is misbehaving. Either way, end every session this user has.
                var active = await _context.RefreshTokens
                    .Where(t => t.UserId == existing.UserId && t.RevokedAt == null)
                    .ToListAsync();
                foreach (var token in active)
                    token.RevokedAt = utcNow;
                await _context.SaveChangesAsync();
                return null;
            }

            var user = await _userManager.FindByIdAsync(existing.UserId);
            if (user == null)
                return null;

            // Rotation renews the idle deadline but never the session cap set at login.
            var (rawReplacement, replacement) = CreateRefreshToken(user.Id, utcNow, existing.SessionExpiresAt);
            existing.RevokedAt = utcNow;
            existing.ReplacedByTokenHash = replacement.TokenHash;
            _context.RefreshTokens.Add(replacement);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another request rotated this same token first. That's a race, not theft,
                // so don't revoke everything — just refuse this one.
                return null;
            }

            return await BuildAuthResponse(user, rawReplacement, replacement);
        }

        public async Task Logout(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            var tokenHash = HashToken(refreshToken);
            var existing = await _context.RefreshTokens
                .SingleOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (existing == null || existing.RevokedAt != null)
                return;

            existing.RevokedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Revoked concurrently by a refresh — the token is dead either way.
            }
        }

        private async Task<AuthResponse> BuildAuthResponse(ApplicationUser user, string rawRefreshToken, RefreshToken refreshToken)
        {
            JwtSecurityToken jwtSecurityToken = await GenerateToken(user);

            return new AuthResponse
            {
                Id = user.Id,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                Email = user.Email,
                UserName = user.UserName,
                RefreshToken = rawRefreshToken,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                SessionExpiresAt = refreshToken.SessionExpiresAt
            };
        }

        private (string Raw, RefreshToken Entity) CreateRefreshToken(string userId, DateTime utcNow, DateTime sessionExpiresAt)
        {
            var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
            var idleDeadline = utcNow.AddMinutes(_jwtSettings.RefreshTokenIdleMinutes);

            return (raw, new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(raw),
                CreatedAt = utcNow,
                ExpiresAt = idleDeadline < sessionExpiresAt ? idleDeadline : sessionExpiresAt,
                SessionExpiresAt = sessionExpiresAt
            });
        }

        // Unsalted SHA-256 is sufficient here: the input is 512 bits of CSPRNG output, so there is
        // nothing to brute-force, and a deterministic hash is what makes the lookup indexable.
        private static string HashToken(string rawToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));


        public async Task<RegistrationResponse> Register(RegistrationRequest request)
        {
            var user = new ApplicationUser
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.UserName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                return new RegistrationResponse() { UserId = user.Id };
            }
            else
            {
                StringBuilder str = new StringBuilder();
                foreach (var err in result.Errors)
                {
                    str.AppendFormat("•{0}\n", err.Description);
                }
                throw new BadRequestException($"{str}");
            }
        }

        private async Task<JwtSecurityToken> GenerateToken(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var roleClaims = roles.Select(q => new Claim(ClaimTypes.Role, q)).ToList();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id)
            }
            .Union(userClaims)
            .Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));

            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                signingCredentials: signingCredentials);
            return jwtSecurityToken;
        }
    }
}
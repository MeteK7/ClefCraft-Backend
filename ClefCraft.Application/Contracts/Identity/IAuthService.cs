using ClefCraft.Application.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Identity
{
    public interface IAuthService
    {
        /// <summary>
        /// Signs in with email and password. Returns null for an unknown email, a wrong password or a
        /// locked-out account alike, so the response never reveals which accounts exist.
        /// </summary>
        Task<AuthResponse?> Login(AuthRequest request);
        Task<RegistrationResponse> Register(RegistrationRequest request);

        /// <summary>Rotates the refresh token. Returns null when it is unknown, expired, revoked or lost a race.</summary>
        Task<AuthResponse?> Refresh(string refreshToken);

        /// <summary>Revokes the refresh token. Idempotent; unknown tokens are ignored.</summary>
        Task Logout(string refreshToken);
    }
}

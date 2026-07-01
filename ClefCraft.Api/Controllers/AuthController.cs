using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authenticationService;
        private readonly IUserService _userService;

        public AuthController(IAuthService authenticationService, IUserService userService)
        {
            _authenticationService = authenticationService;
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(AuthRequest request)
        {
            return Ok(await _authenticationService.Login(request));
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegistrationResponse>> Register(RegistrationRequest request)
        {
            return Ok(await _authenticationService.Register(request));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> Me()
        {
            var userId = _userService.UserId;
            var user = await _userService.GetUser(userId);

            return Ok(new
            {
                user.Id,
                user.Firstname,
                user.Lastname,
                fullName = $"{user.Firstname} {user.Lastname}",
                user.Email
            });
        }
    }
}

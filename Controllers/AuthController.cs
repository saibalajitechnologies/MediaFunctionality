using FunctionalitiesWebAPI.Models;
using FunctionalitiesWebAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FunctionalitiesWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service)
        {
            _service = service;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken token)
        {
            return Ok(await _service.Login(request, token));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            return Ok(await _service.Register(request));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(string refreshToken)
        {
            return Ok(await _service.RefreshToken(refreshToken));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            return Ok(await _service.ForgotPassword(email));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(string token, string password)
        {
            await _service.ResetPassword(token, password);
            return Ok();
        }
    }
}

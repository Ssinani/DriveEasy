using Microsoft.AspNetCore.Mvc;
using VehicleRental.API.DTOs;
using VehicleRental.API.Services;

namespace VehicleRental.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST /api/auth/login
        // 200 OK: credentials valid — returns JWT token and user info
        // 401 Unauthorized: email or password incorrect, or account inactive
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(dto);

            if (result == null)
                return Unauthorized(new { error = "Invalid email or password." });

            return Ok(result);
        }

        // POST /api/auth/register
        // 201 Created: new customer account created — returns JWT token
        // 400 Bad Request: validation error
        // 409 Conflict: email already registered
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _authService.RegisterAsync(dto);
                return CreatedAtAction(nameof(Login), result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.API.DTOs;
using VehicleRental.API.Services;

namespace VehicleRental.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /api/users
        // 200 OK: all users returned [Admin only]
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        // GET /api/users/{id}
        // 200 OK: user returned (customer can only view own profile)
        // 403 Forbidden: customer accessing another user's profile
        // 404 Not Found: user does not exist
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserReadDto>> GetById(int id)
        {
            if (!User.IsInRole("Admin") && id != CurrentUserId)
                return Forbid();

            var user = await _userService.GetByIdAsync(id);

            if (user == null)
                return NotFound(new { error = $"User with id {id} was not found." });

            return Ok(user);
        }

        // GET /api/users/me
        // 200 OK: current user's own profile
        [HttpGet("me")]
        public async Task<ActionResult<UserReadDto>> GetMe()
        {
            var user = await _userService.GetByIdAsync(CurrentUserId);

            if (user == null)
                return NotFound(new { error = "User profile not found." });

            return Ok(user);
        }

        // GET /api/users/role/{role}
        // 200 OK: users with the given role returned [Admin only]
        [HttpGet("role/{role}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetByRole(string role)
        {
            var users = await _userService.GetByRoleAsync(role);
            return Ok(users);
        }

        // PUT /api/users/{id}
        // 200 OK: profile updated (customer can only update own profile)
        // 403 Forbidden: customer updating another user's profile
        // 404 Not Found: user does not exist
        [HttpPut("{id:int}")]
        public async Task<ActionResult<UserReadDto>> Update(int id, [FromBody] UserUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!User.IsInRole("Admin") && id != CurrentUserId)
                return Forbid();

            var updated = await _userService.UpdateAsync(id, dto);

            if (updated == null)
                return NotFound(new { error = $"User with id {id} was not found." });

            return Ok(updated);
        }

        // PATCH /api/users/{id}/deactivate
        // 204 No Content: user deactivated [Admin only]
        // 404 Not Found: user does not exist
        [HttpPatch("{id:int}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _userService.DeactivateAsync(id);

            if (!result)
                return NotFound(new { error = $"User with id {id} was not found." });

            return NoContent();
        }

        // PATCH /api/users/{id}/role
        // 204 No Content: role changed [Admin only]
        // 400 Bad Request: invalid role value
        // 404 Not Found: user does not exist
        [HttpPatch("{id:int}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _userService.ChangeRoleAsync(id, dto.Role);

                if (!result)
                    return NotFound(new { error = $"User with id {id} was not found." });

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

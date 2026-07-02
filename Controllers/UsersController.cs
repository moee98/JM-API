using JMAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JMAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public UsersController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(await CreateUserResponseAsync(user));
        }

        [HttpGet("name/{id}")]
        public async Task<IActionResult> GetUserName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("User id is required.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user.Name);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var responses = new List<object>();
            foreach (var user in _userManager.Users.ToList())
            {
                responses.Add(await CreateUserResponseAsync(user));
            }

            return Ok(responses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("User id is required.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(await CreateUserResponseAsync(user));
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Current password and new password are required.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            return NoContent();
        }

        [HttpPost("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetUserRole(string id, [FromBody] SetUserRoleRequest request)
        {
            if (request is null || (request.Role != "Admin" && request.Role != "User"))
            {
                return BadRequest("Role must be 'Admin' or 'User'.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == currentUserId && request.Role != "Admin")
            {
                // A lone admin demoting themselves would lock everyone out of
                // user management until someone edits the database directly.
                return BadRequest("You cannot remove your own Admin role.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, request.Role);

            return Ok(await CreateUserResponseAsync(user));
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
            {
                return BadRequest("You cannot deactivate your own account.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            return Ok(await CreateUserResponseAsync(user));
        }

        [HttpPost("{id}/reactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReactivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEndDateAsync(user, null);

            return Ok(await CreateUserResponseAsync(user));
        }

        private async Task<object> CreateUserResponseAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            return new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.Name,
                user.PhoneNumber,
                Roles = roles,
                IsActive = !isLockedOut
            };
        }
    }
}

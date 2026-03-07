using JMAPI.Models;
using JMAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JMAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TokenService _tokenService;

        public AuthController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            TokenService tokenService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthModel model)
        {
            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Name = model.Name
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(createResult.Errors);
            }

            const string defaultRole = "User";
            if (!await _roleManager.RoleExistsAsync(defaultRole))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(defaultRole));
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return StatusCode(500, roleResult.Errors);
                }
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, defaultRole);
            if (!addRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return BadRequest(addRoleResult.Errors);
            }

            return Ok(CreateUserResponse(user));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized();
            }

            var claims = await BuildClaimsAsync(user);

            var accessToken = _tokenService.GenerateAccessToken(claims);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return StatusCode(500, updateResult.Errors);
            }

            return Ok(new
            {
                token = accessToken,
                refreshToken,
                user = CreateUserResponse(user)
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] Token tokenModel)
        {
            if (tokenModel is null)
            {
                return BadRequest("Invalid client request");
            }

            ClaimsPrincipal principal;
            try
            {
                principal = _tokenService.GetPrincipalFromExpiredToken(tokenModel.AccessToken);
            }
            catch
            {
                return Unauthorized();
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null ||
                user.RefreshToken != tokenModel.RefreshToken ||
                user.RefreshTokenExpiryTime == null ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized();
            }

            var claims = await BuildClaimsAsync(user);
            var newAccessToken = _tokenService.GenerateAccessToken(claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return StatusCode(500, updateResult.Errors);
            }

            return Ok(new
            {
                token = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        private async Task<List<Claim>> BuildClaimsAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            if (!string.IsNullOrWhiteSpace(user.Name))
            {
                claims.Add(new Claim(ClaimTypes.Name, user.Name));
            }

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            return claims;
        }

        private static object CreateUserResponse(AppUser user) => new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.Name,
            user.PhoneNumber
        };
    }
}

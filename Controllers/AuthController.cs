using JMAPI.Interfaces;
using JMAPI.Models;
using JMAPI.Services;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            TokenService tokenService,
            IHostEnvironment env,
            IConfiguration config,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _env = env;
            _config = config;
            _emailService = emailService;
        }

        // Whether the JWT cookie requires HTTPS. Defaults to true in production
        // and false in development. Override via "Cookies:Secure" in appsettings.
        private bool CookieSecure =>
            _config.GetValue<bool?>("Cookies:Secure") ?? !_env.IsDevelopment();

        // Sets the JWT access token as an HTTP-only cookie so it cannot be
        // read or stolen by JavaScript (XSS protection).
        private void SetJwtCookie(string accessToken)
        {
            Response.Cookies.Append("jwt", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = CookieSecure,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromMinutes(15),
                Path = "/"
            });
        }

        private void ClearJwtCookie()
        {
            Response.Cookies.Delete("jwt", new CookieOptions
            {
                HttpOnly = true,
                Secure = CookieSecure,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });
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
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return StatusCode(500, updateResult.Errors);
            }

            // Set access token as HTTP-only cookie — never exposed to JavaScript
            SetJwtCookie(accessToken);

            return Ok(new
            {
                refreshToken,
                user = CreateUserResponse(user)
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest("Refresh token is required.");
            }

            // Read the expired access token from the HTTP-only cookie
            var expiredAccessToken = Request.Cookies["jwt"];
            if (string.IsNullOrWhiteSpace(expiredAccessToken))
            {
                return Unauthorized();
            }

            ClaimsPrincipal principal;
            try
            {
                principal = _tokenService.GetPrincipalFromExpiredToken(expiredAccessToken);
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
                user.RefreshToken != request.RefreshToken ||
                user.RefreshTokenExpiryTime == null ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized();
            }

            var newClaims = await BuildClaimsAsync(user);
            var newAccessToken = _tokenService.GenerateAccessToken(newClaims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            // Reset the expiry on every successful refresh so the session is a true
            // sliding window (an active user is never logged out), not a fixed
            // expiry counted from the original login.
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return StatusCode(500, updateResult.Errors);
            }

            // Rotate the access token cookie
            SetJwtCookie(newAccessToken);

            return Ok(new { refreshToken = newRefreshToken });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Invalidate the stored refresh token so it cannot be reused
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                    await _userManager.UpdateAsync(user);
                }
            }

            ClearJwtCookie();
            return NoContent();
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var frontendUrl = _config["Frontend:BaseUrl"]?.TrimEnd('/')
                        ?? throw new InvalidOperationException("Frontend:BaseUrl is not configured.");
                    var resetLink =
                        $"{frontendUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

                    await _emailService.SendPasswordResetAsync(user.Email, resetLink);
                }
                catch
                {
                    // Swallow send failures here so the response below doesn't
                    // become a way to distinguish "account exists but email
                    // failed" from "account doesn't exist".
                }
            }

            // Always return the same response whether or not the email is
            // registered, to avoid leaking which accounts exist.
            return Ok(new { message = "If that email is registered, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Email, token, and new password are required.");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Same generic error whether the account exists or the token
                // is simply invalid - avoids leaking account existence.
                return BadRequest("Invalid or expired reset link.");
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code.Contains("Token", StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("Invalid or expired reset link.");
                }
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            return NoContent();
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

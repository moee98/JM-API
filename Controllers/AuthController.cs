using JMAPI.Interfaces;
using JMAPI.Models;
using JMAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
        private readonly RefreshTokenService _refreshTokenService;
        private readonly IHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            TokenService tokenService,
            RefreshTokenService refreshTokenService,
            IHostEnvironment env,
            IConfiguration config,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _env = env;
            _config = config;
            _emailService = emailService;
        }

        // Whether the JWT cookie requires HTTPS. Defaults to true in production
        // and false in development. Override via "Cookies:Secure" in appsettings.
        private bool CookieSecure =>
            _config.GetValue<bool?>("Cookies:Secure") ?? !_env.IsDevelopment();

        private string? DeviceLabel => Request.Headers.UserAgent.ToString();

        // Sets the JWT access token as an HTTP-only cookie so it cannot be
        // read or stolen by JavaScript (XSS protection).
        private void SetJwtCookie(string accessToken, bool isPersistent, DateTime sessionExpiresAt)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = CookieSecure,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };

            // The cookie is only transport - the JWT carries its own 15-minute
            // expiry and is rejected by the auth middleware the moment it
            // lapses. Giving the cookie the same 15-minute lifetime used to
            // delete it at exactly the point /auth/refresh needed it, which
            // signed out anyone who left the app idle for a quarter of an hour.
            // For a non-persistent session we deliberately omit MaxAge so the
            // cookie dies with the browser session.
            if (isPersistent)
            {
                var maxAge = sessionExpiresAt - DateTime.UtcNow;
                options.MaxAge = maxAge > TimeSpan.Zero ? maxAge : TimeSpan.Zero;
            }

            Response.Cookies.Append("jwt", accessToken, options);
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

            if (await _userManager.IsLockedOutAsync(user))
            {
                // Distinct from the generic 401 above: the caller already proved
                // they know the password, so this doesn't leak account
                // existence - it's just telling a legitimate user why they're
                // blocked.
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Your account has been deactivated. Contact an administrator."
                });
            }

            var claims = await BuildClaimsAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(claims);

            var session = await _refreshTokenService.IssueAsync(user, model.RememberMe, DeviceLabel);

            // Set access token as HTTP-only cookie — never exposed to JavaScript
            SetJwtCookie(accessToken, session.IsPersistent, session.ExpiresAt);

            return Ok(new
            {
                refreshToken = session.RawToken,
                rememberMe = session.IsPersistent,
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

            // The refresh token alone identifies the session. This deliberately
            // does not read the jwt cookie: a returning user's cookie may well
            // have been dropped by the browser, and requiring it here is what
            // used to turn "idle for a while" into "signed out".
            var session = await _refreshTokenService.ValidateAndRotateAsync(request.RefreshToken, DeviceLabel);
            if (!session.Succeeded || session.User is null)
            {
                ClearJwtCookie();
                return Unauthorized();
            }

            var user = session.User;

            // Re-checked on every refresh so deactivating an account takes
            // effect within one access-token lifetime rather than never - the
            // kill switch that makes indefinite sessions safe.
            if (await _userManager.IsLockedOutAsync(user))
            {
                await _refreshTokenService.RevokeAllAsync(user.Id, RefreshTokenRevocationReason.SecuritySweep);
                ClearJwtCookie();
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Your account has been deactivated. Contact an administrator."
                });
            }

            var newClaims = await BuildClaimsAsync(user);
            var newAccessToken = _tokenService.GenerateAccessToken(newClaims);

            // Rotate the access token cookie
            SetJwtCookie(newAccessToken, session.IsPersistent, session.ExpiresAt);

            return Ok(new
            {
                refreshToken = session.RawToken,
                rememberMe = session.IsPersistent
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LogoutRequest? request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
                {
                    // Sign out just this device, leaving the user's other
                    // sessions alone.
                    await _refreshTokenService.RevokeAsync(request.RefreshToken, userId);
                }
                else
                {
                    // No token supplied (older clients) - fall back to signing
                    // out everywhere rather than leaving a session live.
                    await _refreshTokenService.RevokeAllAsync(userId);
                }
            }

            ClearJwtCookie();
            return NoContent();
        }

        /// <summary>Signs the caller out of every device.</summary>
        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await _refreshTokenService.RevokeAllAsync(userId);
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

            // Sessions now outlive a password change by default, so a reset has
            // to sweep them - otherwise resetting a password wouldn't actually
            // evict whoever the user was resetting it because of.
            await _refreshTokenService.RevokeAllAsync(user.Id);

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

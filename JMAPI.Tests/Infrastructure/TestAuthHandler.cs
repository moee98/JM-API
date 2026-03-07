using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace JMAPI.Tests.Infrastructure;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-UserId";
    public const string EmailHeader = "X-Test-Email";
    public const string NameHeader = "X-Test-Name";
    public const string RolesHeader = "X-Test-Roles";
    public const string DefaultUserId = "test-user-id";
    public const string DefaultEmail = "test.user@example.com";
    public const string DefaultName = "Test User";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers[UserIdHeader].FirstOrDefault() ?? DefaultUserId;
        var email = Request.Headers[EmailHeader].FirstOrDefault() ?? DefaultEmail;
        var name = Request.Headers[NameHeader].FirstOrDefault() ?? DefaultName;
        var roleHeader = Request.Headers[RolesHeader].ToString();
        var roles = string.IsNullOrWhiteSpace(roleHeader)
            ? new[] { "User" }
            : roleHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

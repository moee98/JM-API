using FluentAssertions;
using JMAPI.Database;
using JMAPI.Models;
using JMAPI.Services;
using JMAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace JMAPI.Tests.Controllers;

public sealed class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_CreatesIdentityUser_AndAssignsDefaultRole()
    {
        var email = $"register-{Guid.NewGuid():N}@example.com";
        var request = new AuthModel
        {
            Email = email,
            Password = CustomWebApplicationFactory.DefaultPassword,
            Name = "Registered User",
            PhoneNumber = "01234567890"
        };

        var response = await _client.PostAsJsonAsync("/api/Auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);

        await _factory.ExecuteScopeAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var createdUser = await userManager.FindByEmailAsync(email);

            createdUser.Should().NotBeNull();
            var roles = await userManager.GetRolesAsync(createdUser!);
            roles.Should().Contain("User");
        });
    }

    [Fact]
    public async Task Login_ReturnsTokenRefreshToken_AndSafeUserPayload()
    {
        var email = await SeedUserAsync("Login User");

        var response = await _client.PostAsJsonAsync("/api/Auth/login", new AuthModel
        {
            Email = email,
            Password = CustomWebApplicationFactory.DefaultPassword,
            RememberMe = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // The access token travels only as an HTTP-only cookie; the body
        // carries the refresh token and the safe user payload.
        var jwtCookie = GetJwtCookie(response);
        jwtCookie.Should().NotBeNull();
        jwtCookie!.ToLowerInvariant().Should().Contain("httponly");

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        payload.Should().NotBeNull();
        payload!.RefreshToken.Should().NotBeNullOrWhiteSpace();
        payload.RememberMe.Should().BeTrue();
        payload.User.Email.Should().Be(email);
        payload.User.Name.Should().Be("Login User");
    }

    /// <summary>
    /// The cookie is only transport for the JWT, which carries its own
    /// 15-minute expiry. Giving the cookie that same lifetime meant the browser
    /// deleted it at exactly the point /auth/refresh needed it, signing out
    /// anyone idle for a quarter of an hour.
    /// </summary>
    [Fact]
    public async Task Login_WithRememberMe_IssuesCookieThatOutlivesTheAccessToken()
    {
        var email = await SeedUserAsync();

        var response = await LoginRawAsync(_client, email, rememberMe: true);

        var jwtCookie = GetJwtCookie(response);
        jwtCookie.Should().NotBeNull();

        var maxAge = ParseMaxAge(jwtCookie!);
        maxAge.Should().NotBeNull("a remembered session needs a persistent cookie");
        maxAge!.Value.Should().BeGreaterThan(TimeSpan.FromDays(1),
            "the cookie must outlive the 15-minute access token by a wide margin");
    }

    [Fact]
    public async Task Login_WithoutRememberMe_IssuesBrowserSessionCookie()
    {
        var email = await SeedUserAsync();

        var response = await LoginRawAsync(_client, email, rememberMe: false);

        var jwtCookie = GetJwtCookie(response);
        jwtCookie.Should().NotBeNull();
        ParseMaxAge(jwtCookie!).Should().BeNull("the cookie should die with the browser session");
        jwtCookie!.ToLowerInvariant().Should().NotContain("expires");

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        payload!.RememberMe.Should().BeFalse();
    }

    /// <summary>
    /// The regression that used to sign idle users out: refresh must identify
    /// the session from the refresh token alone, with no jwt cookie present.
    /// </summary>
    [Fact]
    public async Task Refresh_SucceedsWhenTheBrowserHasDroppedTheJwtCookie()
    {
        var email = await SeedUserAsync();
        var session = await LoginAsync(_client, email);

        // A brand new client has an empty cookie container - exactly the state
        // of a browser that discarded the cookie while the app was closed.
        var cookielessClient = _factory.CreateClient();

        var response = await cookielessClient.PostAsJsonAsync("/api/Auth/refresh", new RefreshRequest
        {
            RefreshToken = session.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await response.Content.ReadFromJsonAsync<RefreshResponse>();
        refreshed!.RefreshToken.Should().NotBeNullOrWhiteSpace().And.NotBe(session.RefreshToken);
        GetJwtCookie(response).Should().NotBeNull("a fresh access cookie should be issued");
    }

    [Fact]
    public async Task Refresh_LeavesOtherDevicesSignedIn()
    {
        var email = await SeedUserAsync();

        var desktop = await LoginAsync(_factory.CreateClient(), email);
        var phone = await LoginAsync(_factory.CreateClient(), email);

        desktop.RefreshToken.Should().NotBe(phone.RefreshToken);

        // Refreshing on the phone must not disturb the desktop session, which
        // the old single-column design could not manage.
        var phoneRefresh = await RefreshAsync(phone.RefreshToken);
        phoneRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var desktopRefresh = await RefreshAsync(desktop.RefreshToken);
        desktopRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Two browser tabs can hit refresh within milliseconds of each other. The
    /// loser of that race presents a token that was rotated moments ago and
    /// must still be served rather than signed out.
    /// </summary>
    [Fact]
    public async Task Refresh_WithJustRotatedToken_IsServedInsideTheGraceWindow()
    {
        var email = await SeedUserAsync();
        var session = await LoginAsync(_client, email);

        var first = await RefreshAsync(session.RefreshToken);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // The original token is now revoked, but only just.
        var second = await RefreshAsync(session.RefreshToken);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await second.Content.ReadFromJsonAsync<RefreshResponse>();
        payload!.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_WithLongDeadToken_RevokesEverySessionForThatUser()
    {
        var email = await SeedUserAsync();

        var deviceA = await LoginAsync(_factory.CreateClient(), email);
        var deviceB = await LoginAsync(_factory.CreateClient(), email);

        await RefreshAsync(deviceA.RefreshToken);

        // Push the rotation well outside the grace window so replaying the old
        // value reads as a leaked token rather than a tab race.
        await BackdateRevocationAsync(deviceA.RefreshToken, RefreshTokenService.RotationGrace + TimeSpan.FromMinutes(5));

        var replay = await RefreshAsync(deviceA.RefreshToken);
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Reuse detection is a security stop: every session for the user goes.
        var deviceBAfter = await RefreshAsync(deviceB.RefreshToken);
        deviceBAfter.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_IsRejected()
    {
        var response = await RefreshAsync(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesOnlyThePresentedSession()
    {
        var userId = Guid.NewGuid().ToString();
        var email = $"logout-{Guid.NewGuid():N}@example.com";
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync(userId, email, "Logout User", "User");

        var desktop = await LoginAsync(_factory.CreateClient(), email);
        var phone = await LoginAsync(_factory.CreateClient(), email);

        var response = await authenticatedClient.PostAsJsonAsync("/api/Auth/logout", new LogoutRequest
        {
            RefreshToken = desktop.RefreshToken
        });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await RefreshAsync(desktop.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(phone.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutAll_RevokesEverySession()
    {
        var userId = Guid.NewGuid().ToString();
        var email = $"logout-all-{Guid.NewGuid():N}@example.com";
        var authenticatedClient = await _factory.CreateAuthenticatedClientAsync(userId, email, "Logout All User", "User");

        var desktop = await LoginAsync(_factory.CreateClient(), email);
        var phone = await LoginAsync(_factory.CreateClient(), email);

        var response = await authenticatedClient.PostAsync("/api/Auth/logout-all", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await RefreshAsync(desktop.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(phone.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshTokens_AreStoredHashed_NeverInPlaintext()
    {
        var email = await SeedUserAsync();
        var session = await LoginAsync(_client, email);

        await _factory.ExecuteScopeAsync(async services =>
        {
            var db = services.GetRequiredService<AppDbContext>();
            var stored = await db.RefreshTokens.Select(t => t.TokenHash).ToListAsync();

            stored.Should().NotContain(session.RefreshToken);
            stored.Should().Contain(RefreshTokenService.Hash(session.RefreshToken));
        });
    }

    private async Task<string> SeedUserAsync(string name = "Test User")
    {
        var email = $"auth-{Guid.NewGuid():N}@example.com";
        await _factory.SeedIdentityUserAsync(Guid.NewGuid().ToString(), email, name, "User");
        return email;
    }

    private static Task<HttpResponseMessage> LoginRawAsync(HttpClient client, string email, bool rememberMe) =>
        client.PostAsJsonAsync("/api/Auth/login", new AuthModel
        {
            Email = email,
            Password = CustomWebApplicationFactory.DefaultPassword,
            RememberMe = rememberMe
        });

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email, bool rememberMe = true)
    {
        var response = await LoginRawAsync(client, email, rememberMe);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        payload.Should().NotBeNull();
        return payload!;
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        _factory.CreateClient().PostAsJsonAsync("/api/Auth/refresh", new RefreshRequest
        {
            RefreshToken = refreshToken
        });

    private Task BackdateRevocationAsync(string rawToken, TimeSpan by) =>
        _factory.ExecuteScopeAsync(async services =>
        {
            var db = services.GetRequiredService<AppDbContext>();
            var hash = RefreshTokenService.Hash(rawToken);
            var row = await db.RefreshTokens.SingleAsync(t => t.TokenHash == hash);

            row.RevokedAt = DateTime.UtcNow - by;
            await db.SaveChangesAsync();
        });

    private static string? GetJwtCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;

        return cookies.FirstOrDefault(cookie =>
            cookie.StartsWith("jwt=", StringComparison.OrdinalIgnoreCase) &&
            !cookie.StartsWith("jwt=;", StringComparison.OrdinalIgnoreCase));
    }

    private static TimeSpan? ParseMaxAge(string setCookieHeader)
    {
        var segment = setCookieHeader
            .Split(';', StringSplitOptions.TrimEntries)
            .FirstOrDefault(part => part.StartsWith("max-age=", StringComparison.OrdinalIgnoreCase));

        if (segment is null) return null;

        return int.TryParse(segment["max-age=".Length..], out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;
    }

    private sealed record UserResponse(string Id, string UserName, string Email, string? Name, string? PhoneNumber);

    private sealed record LoginResponse(string RefreshToken, bool RememberMe, UserResponse User);

    private sealed record RefreshResponse(string RefreshToken, bool RememberMe);
}

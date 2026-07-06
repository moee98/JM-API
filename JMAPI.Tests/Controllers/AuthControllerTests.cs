using FluentAssertions;
using JMAPI.Models;
using JMAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
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
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await _factory.SeedIdentityUserAsync(Guid.NewGuid().ToString(), email, "Login User", "User");

        var response = await _client.PostAsJsonAsync("/api/Auth/login", new AuthModel
        {
            Email = email,
            Password = CustomWebApplicationFactory.DefaultPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // The access token travels only as an HTTP-only cookie; the body
        // carries the refresh token and the safe user payload.
        response.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeTrue();
        setCookies.Should().Contain(cookie =>
            cookie.StartsWith("jwt=") &&
            !cookie.StartsWith("jwt=;") &&
            cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        payload.Should().NotBeNull();
        payload!.RefreshToken.Should().NotBeNullOrWhiteSpace();
        payload.User.Email.Should().Be(email);
        payload.User.Name.Should().Be("Login User");
    }

    private sealed record UserResponse(string Id, string UserName, string Email, string? Name, string? PhoneNumber);

    private sealed record LoginResponse(string RefreshToken, UserResponse User);
}

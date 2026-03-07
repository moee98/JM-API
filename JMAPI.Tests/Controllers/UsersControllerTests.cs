using FluentAssertions;
using JMAPI.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace JMAPI.Tests.Controllers;

public sealed class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsIdentityUserFromClaims()
    {
        var userId = Guid.NewGuid().ToString();
        var email = $"me-{Guid.NewGuid():N}@example.com";
        var client = await _factory.CreateAuthenticatedClientAsync(userId, email, "Current User", "User");

        var response = await client.GetAsync("/api/Users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<UserResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(userId);
        payload.Email.Should().Be(email);
        payload.Name.Should().Be("Current User");
    }

    [Fact]
    public async Task GetUserById_ReturnsSafeUserPayload()
    {
        var userId = Guid.NewGuid().ToString();
        var email = $"lookup-{Guid.NewGuid():N}@example.com";
        var client = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid().ToString(), "caller@example.com", "Caller", "User");
        await _factory.SeedIdentityUserAsync(userId, email, "Lookup User", "User");

        var response = await client.GetAsync($"/api/Users/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<UserResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(userId);
        payload.Email.Should().Be(email);
        payload.Name.Should().Be("Lookup User");
    }

    private sealed record UserResponse(string Id, string UserName, string Email, string? Name, string? PhoneNumber);
}

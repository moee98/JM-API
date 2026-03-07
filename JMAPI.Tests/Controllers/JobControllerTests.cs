using FluentAssertions;
using JMAPI.Database;
using JMAPI.Models;
using JMAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace JMAPI.Tests.Controllers;

public sealed class JobControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public JobControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetJobById_ReturnsJobDetails_AndUsesCurrentClaimUserId()
    {
        var ownerUserId = Guid.NewGuid().ToString();
        var callerUserId = Guid.NewGuid().ToString();

        await _factory.SeedIdentityUserAsync(ownerUserId, "owner@example.com", "Owner User", "User");
        await _factory.SeedIdentityUserAsync(callerUserId, "caller@example.com", "Caller User", "User");

        var dependencies = await _factory.SeedJobDependenciesAsync();
        var jobId = await _factory.SeedJobAsync(ownerUserId, dependencies.CustomerId, dependencies.VehicleId, dependencies.ServiceId);

        var client = await _factory.CreateAuthenticatedClientAsync(callerUserId, "caller@example.com", "Caller User", "User");
        var response = await client.GetAsync($"/api/Job/{jobId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JobResponse>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(jobId);
        payload.Customer.Should().NotBeNull();
        payload.Vehicle.Should().NotBeNull();
        payload.JobServices.Should().HaveCount(1);
        payload.AppUserId.Should().Be(callerUserId);
    }

    [Fact]
    public async Task UpdateJob_UpdatesRootFields_AndNestedVehicleAndServices()
    {
        var userId = Guid.NewGuid().ToString();
        await _factory.SeedIdentityUserAsync(userId, "job-updater@example.com", "Job Updater", "User");

        var dependencies = await _factory.SeedJobDependenciesAsync();
        var jobId = await _factory.SeedJobAsync(userId, dependencies.CustomerId, dependencies.VehicleId, dependencies.ServiceId);
        var client = await _factory.CreateAuthenticatedClientAsync(userId, "job-updater@example.com", "Job Updater", "User");

        var updateRequest = new Job
        {
            Id = jobId,
            Description = "Updated job",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            Status = "Completed",
            DueDate = DateTime.UtcNow.AddDays(2),
            Notes = "Updated notes",
            IsActive = false,
            Paid = true,
            ServiceCharge = 150,
            VehicleId = dependencies.VehicleId,
            CustomerId = dependencies.CustomerId,
            AppUserId = userId,
            Vehicle = new Vehicle
            {
                Id = dependencies.VehicleId,
                Make = "Honda",
                Model = "Civic",
                LicensePlate = "UPDATED123",
                Colour = "Red",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            },
            JobServices =
            {
                new JobServices
                {
                    ServiceId = dependencies.ServiceId,
                    Price = 55,
                    Completed = true
                }
            }
        };

        var response = await client.PutAsJsonAsync($"/api/Job/{jobId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await _factory.ExecuteScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            var saved = await dbContext.Jobs
                .Include(job => job.Vehicle)
                .Include(job => job.JobServices)
                .FirstAsync(job => job.Id == jobId);

            saved.Description.Should().Be("Updated job");
            saved.Status.Should().Be("Completed");
            saved.Notes.Should().Be("Updated notes");
            saved.Paid.Should().BeTrue();
            saved.ServiceCharge.Should().Be(150);
            saved.IsActive.Should().BeFalse();
            saved.Vehicle.Should().NotBeNull();
            saved.Vehicle!.Make.Should().Be("Honda");
            saved.Vehicle.Model.Should().Be("Civic");
            saved.Vehicle.LicensePlate.Should().Be("UPDATED123");
            saved.JobServices.Should().ContainSingle();
            saved.JobServices.Single().Price.Should().Be(55);
            saved.JobServices.Single().Completed.Should().BeTrue();
        });
    }

    private sealed record JobResponse(
        int Id,
        string? Description,
        string Status,
        string Notes,
        string? AppUserId,
        CustomerResponse? Customer,
        VehicleResponse? Vehicle,
        List<JobServiceResponse> JobServices);

    private sealed record CustomerResponse(int Id, string Name, string Email, string PhoneNumber);

    private sealed record VehicleResponse(int Id, string Make, string Model, string LicensePlate, string Colour);

    private sealed record JobServiceResponse(int id, int ServiceId, float Price, bool Completed);
}

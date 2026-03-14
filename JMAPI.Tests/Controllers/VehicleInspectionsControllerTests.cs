using FluentAssertions;
using JMAPI.Database;
using JMAPI.Models;
using JMAPI.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace JMAPI.Tests.Controllers;

public sealed class VehicleInspectionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public VehicleInspectionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UploadVehicleInspectionAttachments_SavesFilesInDatabaseAndReturnsDownloadableAttachments()
    {
        var (_, vehicleId, _) = await _factory.SeedJobDependenciesAsync();
        var client = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid().ToString(), "inspection@example.com", "Inspection User", "User");

        var createRequest = new VehicleInspection
        {
            VehicleId = vehicleId,
            InspectionDate = DateTime.UtcNow,
            InspectionResult = "Passed",
            Comments = "Vehicle is in good condition.",
            PathToImages = []
        };

        var createResponse = await client.PostAsJsonAsync("/api/VehicleInspections", createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdInspection = await createResponse.Content.ReadFromJsonAsync<VehicleInspectionResponse>();
        createdInspection.Should().NotBeNull();

        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        using var uploadRequest = new MultipartFormDataContent();
        using var imageContent = new ByteArrayContent(imageBytes);
        using var pdfContent = new ByteArrayContent(pdfBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        pdfContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        uploadRequest.Add(imageContent, "Files", "inspection-image.png");
        uploadRequest.Add(pdfContent, "Files", "inspection-report.pdf");

        var uploadResponse = await client.PostAsync($"/api/VehicleInspections/{createdInspection!.Id}/attachments", uploadRequest);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var attachments = await uploadResponse.Content.ReadFromJsonAsync<List<AttachmentSummaryResponse>>();
        attachments.Should().NotBeNull();
        var uploadedAttachments = attachments!;
        uploadedAttachments.Should().HaveCount(2);
        uploadedAttachments.Select(x => x.FileName).Should().Contain(new[] { "inspection-image.png", "inspection-report.pdf" });

        var getResponse = await client.GetAsync($"/api/VehicleInspections/{createdInspection.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var savedInspection = await getResponse.Content.ReadFromJsonAsync<VehicleInspectionResponse>();
        savedInspection.Should().NotBeNull();
        savedInspection!.Attachments.Should().HaveCount(2);

        var pdfAttachment = uploadedAttachments.Single(x => x.FileName == "inspection-report.pdf");
        var downloadResponse = await client.GetAsync($"/api/VehicleInspections/{createdInspection.Id}/attachments/{pdfAttachment.Id}");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await downloadResponse.Content.ReadAsByteArrayAsync()).Should().Equal(pdfBytes);

        await _factory.ExecuteScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            var savedAttachments = dbContext.VehicleInspectionAttachments
                .Where(x => x.VehicleInspectionId == createdInspection.Id)
                .OrderBy(x => x.FileName)
                .ToList();

            savedAttachments.Should().HaveCount(2);
            savedAttachments[0].Data.Should().NotBeEmpty();
            savedAttachments[1].Data.Should().NotBeEmpty();
        });
    }
}

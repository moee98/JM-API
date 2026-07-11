using FluentAssertions;
using JMAPI.Database;
using JMAPI.Models;
using JMAPI.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace JMAPI.Tests.Controllers;

public sealed class ExpenseItemsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ExpenseItemsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostExpenseItem_CreatesExpenseItem()
    {
        var categoryId = await _factory.SeedExpenseCategoryAsync("Cleaning");
        var client = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid().ToString(), "expense@example.com", "Expense User", "User");

        var request = new ExpenseItem
        {
            Description = "Cleaning supplies",
            Amount = 45.5f,
            DateIncurred = DateTime.UtcNow.Date,
            ExpenseCategoryId = categoryId,
            ReceiptImagePath = "receipt.png",
            IsReimbursed = false,
            PaymentMethod = "Card",
            PaidTo = "Acme Supplies"
        };

        var response = await client.PostAsJsonAsync("/api/ExpenseItems", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ExpenseItemResponse>();
        payload.Should().NotBeNull();
        payload!.Description.Should().Be("Cleaning supplies");
        payload.ExpenseCategoryId.Should().Be(categoryId);
        payload.PaidTo.Should().Be("Acme Supplies");
    }

    [Fact]
    public async Task UpdateExpenseItem_UpdatesExistingExpenseItem()
    {
        var categoryId = await _factory.SeedExpenseCategoryAsync("Fuel");
        var expenseItemId = await _factory.ExecuteScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            var item = new ExpenseItem
            {
                Description = "Fuel top-up",
                Amount = 20,
                DateIncurred = DateTime.UtcNow.Date,
                ExpenseCategoryId = categoryId,
                IsReimbursed = false,
                PaymentMethod = "Cash"
            };

            dbContext.ExpenseItems.Add(item);
            await dbContext.SaveChangesAsync();
            return item.Id;
        });

        var client = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid().ToString(), "expense-update@example.com", "Expense Updater", "User");
        var updateRequest = new ExpenseItem
        {
            Id = expenseItemId,
            Description = "Fuel receipt updated",
            Amount = 30,
            DateIncurred = DateTime.UtcNow.Date,
            ExpenseCategoryId = categoryId,
            IsReimbursed = true,
            PaymentMethod = "Card",
            PaidTo = "Shell Garage"
        };

        var response = await client.PutAsJsonAsync($"/api/ExpenseItems/{expenseItemId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await _factory.ExecuteScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            var saved = await dbContext.ExpenseItems.FindAsync(expenseItemId);

            saved.Should().NotBeNull();
            saved!.Description.Should().Be("Fuel receipt updated");
            saved.Amount.Should().Be(30);
            saved.IsReimbursed.Should().BeTrue();
            saved.PaymentMethod.Should().Be("Card");
            saved.PaidTo.Should().Be("Shell Garage");
        });
    }

    [Fact]
    public async Task UploadExpenseItemAttachment_SavesFileInDatabaseAndReturnsDownloadableAttachment()
    {
        var categoryId = await _factory.SeedExpenseCategoryAsync("Travel");
        var client = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid().ToString(), "expense-attachment@example.com", "Expense Attachment User", "User");

        var createRequest = new ExpenseItem
        {
            Description = "Parking receipt",
            Amount = 15,
            DateIncurred = DateTime.UtcNow.Date,
            ExpenseCategoryId = categoryId,
            IsReimbursed = false,
            PaymentMethod = "Card"
        };

        var createResponse = await client.PostAsJsonAsync("/api/ExpenseItems", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdExpenseItem = await createResponse.Content.ReadFromJsonAsync<ExpenseItemResponse>();
        createdExpenseItem.Should().NotBeNull();

        var receiptBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        using var uploadRequest = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(receiptBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        uploadRequest.Add(fileContent, "Files", "parking-receipt.pdf");

        var uploadResponse = await client.PostAsync($"/api/ExpenseItems/{createdExpenseItem!.Id}/attachments", uploadRequest);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var attachments = await uploadResponse.Content.ReadFromJsonAsync<List<AttachmentSummaryResponse>>();
        attachments.Should().NotBeNull();
        var uploadedAttachments = attachments!;
        uploadedAttachments.Should().ContainSingle();
        uploadedAttachments[0].FileName.Should().Be("parking-receipt.pdf");
        uploadedAttachments[0].ContentType.Should().Be("application/pdf");

        var getResponse = await client.GetAsync($"/api/ExpenseItems/{createdExpenseItem.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var savedExpenseItem = await getResponse.Content.ReadFromJsonAsync<ExpenseItemResponse>();
        savedExpenseItem.Should().NotBeNull();
        savedExpenseItem!.Attachments.Should().ContainSingle();
        savedExpenseItem.Attachments[0].Id.Should().Be(uploadedAttachments[0].Id);

        var downloadResponse = await client.GetAsync($"/api/ExpenseItems/{createdExpenseItem.Id}/attachments/{uploadedAttachments[0].Id}");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await downloadResponse.Content.ReadAsByteArrayAsync()).Should().Equal(receiptBytes);

        await _factory.ExecuteScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            var savedAttachment = await dbContext.ExpenseItemAttachments.FindAsync(uploadedAttachments[0].Id);

            savedAttachment.Should().NotBeNull();
            savedAttachment!.Data.Should().Equal(receiptBytes);
        });
    }

    private sealed record ExpenseItemResponse(
        int Id,
        string Description,
        float Amount,
        DateTime DateIncurred,
        int ExpenseCategoryId,
        string? ReceiptImagePath,
        bool IsReimbursed,
        string PaymentMethod,
        string? PaidTo,
        IReadOnlyList<AttachmentSummaryResponse> Attachments);
}

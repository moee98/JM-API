using FluentAssertions;
using JMAPI.Database;
using JMAPI.Models;
using JMAPI.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
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
            PaymentMethod = "Card"
        };

        var response = await client.PostAsJsonAsync("/api/ExpenseItems", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ExpenseItemResponse>();
        payload.Should().NotBeNull();
        payload!.Description.Should().Be("Cleaning supplies");
        payload.ExpenseCategoryId.Should().Be(categoryId);
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
            PaymentMethod = "Card"
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
        string PaymentMethod);
}

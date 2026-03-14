namespace JMAPI.Models
{
    public sealed class ExpenseItemResponse
    {
        public int Id { get; init; }
        public required string Description { get; init; }
        public float Amount { get; init; }
        public DateTime DateIncurred { get; init; }
        public int ExpenseCategoryId { get; init; }
        public string? ReceiptImagePath { get; init; }
        public bool IsReimbursed { get; init; }
        public required string PaymentMethod { get; init; }
        public IReadOnlyList<AttachmentSummaryResponse> Attachments { get; init; } = [];
    }
}

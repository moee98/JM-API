using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json.Serialization;

namespace JMAPI.Models
{
    public class ExpenseItem
    {
        public int Id { get; set; }
        public required string Description { get; set; }
        public float Amount { get; set; }
        public DateTime DateIncurred { get; set; }
        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }
        public string? ReceiptImagePath { get; set; }
        public required bool IsReimbursed { get; set; }
        public required string PaymentMethod { get; set; }
        public string? PaidTo { get; set; }
        [JsonIgnore]
        public IList<ExpenseItemAttachment> Attachments { get; set; } = [];
    }
}

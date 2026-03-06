using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace JMAPI.Models
{
    public class ExpenseItem
    {
        public int Id { get; set; }
        public required string Description { get; set; }
        public float Amount { get; set; }
        public DateTime DateIncurred { get; set; }
        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; } // Navigation property to the category   
        public string? ReceiptImagePath { get; set; } // Path to the receipt image file
        public required bool IsReimbursed { get; set; } // Indicates if the expense has been reimbursed
        public required string PaymentMethod { get; set; } // Method of payment (e.g., "Cash", "Credit Card", etc.)
    }
}

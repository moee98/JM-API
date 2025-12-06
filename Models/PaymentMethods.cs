namespace JMAPI.Models
{
    public class PaymentMethods
    {
        public int Id { get; set; } // Unique identifier for the payment method
        public required string MethodName { get; set; } // Name of the payment method (e.g., "Credit Card", "PayPal")
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp when the payment method was added
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Timestamp when the payment method was last updated
        public bool IsActive { get; set; } = true; // Indicates if the payment method is currently active

    }
}

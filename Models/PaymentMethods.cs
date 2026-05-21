namespace JMAPI.Models
{
    public class PaymentMethods
    {
        public int Id { get; set; }
        public required string MethodName { get; set; }
        public bool IsActive { get; set; } = true;
        public int JobId { get; set; }
        public long Amount { get; set; }
        public string? SquarePaymentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

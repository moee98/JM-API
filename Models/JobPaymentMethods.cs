namespace JMAPI.Models
{
    public class JobPaymentMethods
    {
        public int Id { get; set; } // Unique identifier for the job payment method
        public int JobId { get; set; } // Foreign key referencing the associated job
        public Job? Job { get; set; } // Navigation property to the Job entity
        public int PaymentMethodId { get; set; } // Foreign key referencing the payment method
        public PaymentMethods? PaymentMethod { get; set; } // Navigation property to the PaymentMethods entity
        public bool IsActive { get; set; } = true; // Indicates if the payment method is currently active or valid
    }
}

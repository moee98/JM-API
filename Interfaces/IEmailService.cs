namespace JMAPI.Interfaces
{
    public interface IEmailService
    {
        Task SendInvoiceAsync(int jobId);
        Task SendPaymentReminderAsync(int jobId);
        Task SendPasswordResetAsync(string toEmail, string resetLink);
        Task SendBookingConfirmationAsync(int jobId);
        Task SendBookingReminderAsync(int jobId);
    }
}

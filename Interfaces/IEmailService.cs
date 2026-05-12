namespace JMAPI.Interfaces
{
    public interface IEmailService
    {
        Task SendInvoiceAsync(int jobId);
        Task SendPaymentReminderAsync(int jobId);
    }
}

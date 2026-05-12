using JMAPI.Interfaces;
using JMAPI.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text;

namespace JMAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IJobService _jobService;
        private readonly IConfiguration _config;

        public EmailService(IJobService jobService, IConfiguration config)
        {
            _jobService = jobService;
            _config = config;
        }

        public async Task SendInvoiceAsync(int jobId)
        {
            var job = await _jobService.GetByIdWithDetailsAsync(jobId)
                ?? throw new InvalidOperationException($"Job {jobId} not found.");

            if (string.IsNullOrWhiteSpace(job.Customer?.Email))
                throw new InvalidOperationException("Customer does not have an email address on file.");

            var from = _config["Smtp:From"]
                ?? throw new InvalidOperationException("Smtp:From is not configured.");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(job.Customer.Email));
            message.Subject = $"Your Invoice – Job #{job.Id}";

            var builder = new BodyBuilder { HtmlBody = BuildInvoiceHtml(job) };
            message.Body = builder.ToMessageBody();

            var host = _config["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is not configured.");
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendPaymentReminderAsync(int jobId)
        {
            var job = await _jobService.GetByIdWithDetailsAsync(jobId)
                ?? throw new InvalidOperationException($"Job {jobId} not found.");

            if (string.IsNullOrWhiteSpace(job.Customer?.Email))
                throw new InvalidOperationException("Customer does not have an email address on file.");

            var from = _config["Smtp:From"]
                ?? throw new InvalidOperationException("Smtp:From is not configured.");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(job.Customer.Email));
            message.Subject = $"Payment Reminder – Invoice #{job.Id}";

            var builder = new BodyBuilder { HtmlBody = BuildReminderHtml(job) };
            message.Body = builder.ToMessageBody();

            var host = _config["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is not configured.");
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private static string BuildReminderHtml(Job job)
        {
            var invoiceHtml = BuildInvoiceHtml(job);
            var dueDate = job.DueDate.ToString("dd MMMM yyyy");
            var banner = $@"
<div style=""background:#fff3cd;border:1px solid #ffc107;border-radius:6px;padding:16px 24px;margin:0 0 24px;"">
  <p style=""margin:0;font-size:14px;color:#856404;"">
    <strong>Payment Reminder:</strong> This invoice was due on <strong>{dueDate}</strong>.
    Please arrange payment at your earliest convenience. Contact us if you have any questions.
  </p>
</div>";

            // Inject the banner just after the opening of .body div
            return invoiceHtml.Replace(@"<div class=""body"">", $@"<div class=""body"">{banner}");
        }

        private static string BuildInvoiceHtml(Job job)
        {
            var services = job.JobServices ?? new List<JobServices>();
            float subtotal = services.Sum(js => js.Price) + job.ServiceCharge;
            float vat = MathF.Round(subtotal * 0.20f, 2);
            float total = MathF.Round(subtotal + vat, 2);

            var sb = new StringBuilder();

            sb.Append(@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<meta name=""viewport"" content=""width=device-width,initial-scale=1"" />
<style>
  body{font-family:Arial,Helvetica,sans-serif;background:#f4f4f4;margin:0;padding:0;}
  .wrapper{max-width:640px;margin:32px auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.1);}
  .header{background:#1a1a2e;color:#fff;padding:32px 40px;}
  .header h1{margin:0 0 4px;font-size:26px;}
  .header p{margin:0;font-size:13px;color:#aaa;}
  .body{padding:32px 40px;}
  .section-title{font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.08em;color:#888;margin:24px 0 8px;}
  .info-grid{display:grid;grid-template-columns:1fr 1fr;gap:8px 24px;margin-bottom:4px;}
  .info-label{font-size:11px;color:#888;margin-bottom:2px;}
  .info-value{font-size:14px;font-weight:600;color:#111;}
  table{width:100%;border-collapse:collapse;margin-top:8px;}
  th{font-size:11px;text-transform:uppercase;letter-spacing:.06em;color:#888;padding:8px 0;border-bottom:1px solid #eee;text-align:left;}
  th:last-child,td:last-child{text-align:right;}
  td{font-size:13px;color:#333;padding:10px 0;border-bottom:1px solid #f0f0f0;}
  .totals{margin-top:16px;text-align:right;}
  .totals .row{display:flex;justify-content:flex-end;gap:24px;font-size:13px;padding:4px 0;color:#555;}
  .totals .total-row{font-size:16px;font-weight:700;color:#111;padding-top:8px;border-top:2px solid #111;margin-top:4px;}
  .footer{background:#f9f9f9;padding:20px 40px;text-align:center;font-size:12px;color:#888;border-top:1px solid #eee;}
</style>
</head>
<body>
<div class=""wrapper"">
  <div class=""header"">
    <h1>Invoice</h1>
    <p>Job #");
            sb.Append(job.Id);
            sb.Append(@" &bull; ");
            sb.Append(job.CreatedAt.ToString("dd MMMM yyyy"));
            sb.Append(@"</p>
  </div>
  <div class=""body"">
    <div class=""section-title"">Customer</div>
    <div class=""info-grid"">
      <div><div class=""info-label"">Name</div><div class=""info-value"">");
            sb.Append(System.Web.HttpUtility.HtmlEncode(job.Customer?.Name ?? "-"));
            sb.Append(@"</div></div>
      <div><div class=""info-label"">Email</div><div class=""info-value"">");
            sb.Append(System.Web.HttpUtility.HtmlEncode(job.Customer?.Email ?? "-"));
            sb.Append(@"</div></div>
      <div><div class=""info-label"">Phone</div><div class=""info-value"">");
            sb.Append(System.Web.HttpUtility.HtmlEncode(job.Customer?.PhoneNumber ?? "-"));
            sb.Append(@"</div></div>
    </div>");

            if (job.Vehicle != null)
            {
                sb.Append(@"
    <div class=""section-title"">Vehicle</div>
    <div class=""info-grid"">
      <div><div class=""info-label"">Registration</div><div class=""info-value"">");
                sb.Append(System.Web.HttpUtility.HtmlEncode(job.Vehicle.LicensePlate));
                sb.Append(@"</div></div>
      <div><div class=""info-label"">Make / Model</div><div class=""info-value"">");
                sb.Append(System.Web.HttpUtility.HtmlEncode($"{job.Vehicle.Make} {job.Vehicle.Model}".Trim()));
                sb.Append(@"</div></div>");
                if (!string.IsNullOrEmpty(job.Vehicle.Colour))
                {
                    sb.Append(@"
      <div><div class=""info-label"">Colour</div><div class=""info-value"">");
                    sb.Append(System.Web.HttpUtility.HtmlEncode(job.Vehicle.Colour));
                    sb.Append("</div></div>");
                }
                sb.Append("</div>");
            }

            sb.Append(@"
    <div class=""section-title"">Services</div>
    <table>
      <thead><tr><th>Description</th><th>Price</th></tr></thead>
      <tbody>");

            foreach (var js in services)
            {
                sb.Append("<tr><td>");
                sb.Append(System.Web.HttpUtility.HtmlEncode(js.Service?.Name ?? "Service"));
                sb.Append("</td><td>&pound;");
                sb.Append(js.Price.ToString("0.00"));
                sb.Append("</td></tr>");
            }

            if (job.ServiceCharge > 0)
            {
                sb.Append("<tr><td>Service Charge</td><td>&pound;");
                sb.Append(job.ServiceCharge.ToString("0.00"));
                sb.Append("</td></tr>");
            }

            sb.Append(@"</tbody></table>
    <div class=""totals"">
      <div class=""row""><span>Subtotal</span><span>&pound;");
            sb.Append(subtotal.ToString("0.00"));
            sb.Append(@"</span></div>
      <div class=""row""><span>VAT (20%)</span><span>&pound;");
            sb.Append(vat.ToString("0.00"));
            sb.Append(@"</span></div>
      <div class=""row total-row""><span>Total</span><span>&pound;");
            sb.Append(total.ToString("0.00"));
            sb.Append(@"</span></div>
    </div>
  </div>
  <div class=""footer"">Thank you for your business. Please contact us if you have any questions.</div>
</div>
</body>
</html>");

            return sb.ToString();
        }
    }
}

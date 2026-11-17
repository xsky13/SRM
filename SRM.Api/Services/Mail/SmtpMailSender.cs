using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SRM.Api.Services.Mail
{
    public class SmtpMailSender(IConfiguration config, ILogger<SmtpMailSender> logger) : IMailSender
    {
        private const string FromAddress = "aljapeter05@gmail.com";

        public async Task SendAsync(QueuedMail mail, CancellationToken ct)
        {
            var password = config["GMAIL_PASSWORD"]
                ?? throw new InvalidOperationException("GMAIL_PASSWORD is not configured.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Reservas Moreno", FromAddress));
            message.To.Add(new MailboxAddress(mail.ToName ?? "Cliente", mail.To));
            message.Subject = mail.Title;
            message.Body = new TextPart("plain") { Text = mail.Body };

            using var client = new SmtpClient { Timeout = 15_000 };

            // any failure propagates to MailSenderService, which retries; `using` closes the connection
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(FromAddress, password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Mail sent to {To}", mail.To);
        }
    }
}

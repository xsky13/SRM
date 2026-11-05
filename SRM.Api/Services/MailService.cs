using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SRM.Api.Services.Interfaces;

namespace SRM.Api.Services
{
    public class MailService : IMailService
    {
        private static readonly string pwd = Environment.GetEnvironmentVariable("GMAIL_PASSWORD")
                                     ?? throw new InvalidOperationException("GMAIL_PASSWORD environment variable is not set.");


        public async Task SendMail(string msg, string title, string destinationEmail, string? destUser)
        {
            // 1. Create the email message using MimeKit
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Reservas Moreno", "aljapeter05@gmail.com"));
            message.To.Add(new MailboxAddress(destUser ?? "Al cliente", destinationEmail));
            message.Subject = title;

            message.Body = new TextPart("plain")
            {
                Text = msg
            };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync("aljapeter05@gmail.com", pwd);

                await client.SendAsync(message);
                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}

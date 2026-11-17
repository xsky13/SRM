using SRM.Api.Services.Interfaces;

namespace SRM.Api.Services.Mail
{
    public class QueuedMailService(MailQueue queue, ILogger<QueuedMailService> logger) : IMailService
    {
        public Task SendMail(string msg, string title, string destinationEmail, string? destUser)
        {
            if (!queue.TryEnqueue(new QueuedMail(msg, title, destinationEmail, destUser)))
                logger.LogError("Cola de mails llena: se descarto el mail a {To} ({Title})", destinationEmail, title);

            return Task.CompletedTask;
        }
    }
}

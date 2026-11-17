namespace SRM.Api.Services.Mail
{
    public interface IMailSender
    {
        Task SendAsync(QueuedMail mail, CancellationToken ct);
    }
}

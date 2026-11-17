namespace SRM.Api.Services.Mail
{
    public record QueuedMail(string Body, string Title, string To, string? ToName = "Cliente");
}

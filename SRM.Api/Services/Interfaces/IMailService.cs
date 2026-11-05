namespace SRM.Api.Services.Interfaces
{
    public interface IMailService
    {
        Task SendMail(string msg, string title, string destinationEmail, string? destUser);
    }
}

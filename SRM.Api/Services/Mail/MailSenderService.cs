namespace SRM.Api.Services.Mail
{
    public class MailSenderService(
    MailQueue queue, IServiceScopeFactory scopeFactory, ILogger<MailSenderService> logger) : BackgroundService
    {
        private const int MaxAttempts = 3;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var mail in queue.ReadAllAsync(stoppingToken))
            {
                for (var attempt = 1; attempt <= MaxAttempts; attempt++)
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var sender = scope.ServiceProvider.GetRequiredService<IMailSender>();
                        await sender.SendAsync(mail, stoppingToken);
                        break;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Mail a {To} fallo (intento {Attempt}/{Max})", mail.To, attempt, MaxAttempts);

                        if (attempt == MaxAttempts)
                            logger.LogError("Mail a {To} ({Title}) descartado tras {Max} intentos", mail.To, mail.Title, MaxAttempts);
                        else
                            await Task.Delay(TimeSpan.FromSeconds(2 * attempt), stoppingToken);
                    }
                }
            }
        }
    }
}

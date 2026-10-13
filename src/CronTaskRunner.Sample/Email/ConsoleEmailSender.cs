using Microsoft.Extensions.Logging;

namespace CronTaskRunner.Sample.Email;

/// <summary>
/// Заглушка для демонстрации — пишет «письмо» в лог вместо реальной отправки.
/// Используется, пока в конфиге не включён Smtp:Enabled.
/// </summary>
public sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "EMAIL -> {To} | {Subject} | isHtml={IsHtml} | {Body}",
            message.To, message.Subject, message.IsHtml, message.Body);
        return Task.CompletedTask;
    }
}

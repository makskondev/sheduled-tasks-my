using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CronTaskRunner.Sample.Email;

/// <summary>
/// Реальная отправка почты через SMTP (System.Net.Mail).
/// Настройки — секция "Smtp" в appsettings.json (см. SmtpOptions).
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var smtp = options.Value;

        using var mail = new MailMessage(smtp.From, message.To, message.Subject, message.Body)
        {
            IsBodyHtml = message.IsHtml,
        };

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl,
        };

        if (!string.IsNullOrEmpty(smtp.User))
        {
            client.Credentials = new NetworkCredential(smtp.User, smtp.Password);
        }

        logger.LogInformation("Отправляем письмо -> {To} | {Subject}", message.To, message.Subject);

        await client.SendMailAsync(mail, cancellationToken);
    }
}

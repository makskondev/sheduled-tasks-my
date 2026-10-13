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

        if (message.To.Count == 0)
        {
            throw new ArgumentException("У письма не задано ни одного получателя", nameof(message));
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(smtp.From),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml,
        };

        foreach (var recipient in message.To)
        {
            mail.To.Add(recipient);
        }

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl,
        };

        if (!string.IsNullOrEmpty(smtp.User))
        {
            client.Credentials = new NetworkCredential(smtp.User, smtp.Password);
        }

        logger.LogInformation("Отправляем письмо -> {To} | {Subject}", string.Join(", ", message.To), message.Subject);

        await client.SendMailAsync(mail, cancellationToken);
    }
}

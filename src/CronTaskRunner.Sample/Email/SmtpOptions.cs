namespace CronTaskRunner.Sample.Email;

/// <summary>Настройки SMTP — секция "Smtp" в appsettings.json.</summary>
public sealed class SmtpOptions
{
    /// <summary>false — письма только пишутся в лог (ConsoleEmailSender).</summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;

    /// <summary>Пустой — отправка без аутентификации.</summary>
    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}

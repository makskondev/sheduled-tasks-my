using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CronTaskRunner.Sample.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует IEmailSender: SMTP, если Smtp:Enabled = true, иначе запись писем в лог.
    /// </summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Smtp");
        services.Configure<SmtpOptions>(section);

        if (section.GetValue<bool>(nameof(SmtpOptions.Enabled)))
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, ConsoleEmailSender>();
        }

        return services;
    }
}

using CronTaskRunner.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CronTaskRunner.Sample.ExchangeRates;

public static class ExchangeRatesServiceCollectionExtensions
{
    /// <summary>Регистрирует задачу проверки курса под ключом "ExchangeRateCheck" и её зависимости.</summary>
    public static IServiceCollection AddExchangeRateCheck(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScheduledTask<ExchangeRateCheckTask>("ExchangeRateCheck");

        // Заглушка для демонстрации — замените на реальный провайдер курсов.
        services.AddScoped<IExchangeRateProvider, FakeExchangeRateProvider>();

        return services;
    }
}

using CronTaskRunner.Core.Extensions;
using CronTaskRunner.Sample.Deals.Mssql;
using CronTaskRunner.Sample.Deals.Oracle;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CronTaskRunner.Sample.Deals;

public static class DealsServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует задачу сверки сделок под ключом "DealsReconciliation" и её источники:
    /// MSSQL (dbo.Deals) и Oracle (SVOD.DEAL_REG) через EF Core либо, при
    /// DealsReconciliation:UseInMemorySources = true, демо-источники в памяти.
    /// Параметры самой сверки (To, LookbackDays) задаются в Options события расписания;
    /// UseInMemorySources — глобальный переключатель регистрации, поэтому остаётся
    /// отдельной секцией и меняется только рестартом.
    /// </summary>
    public static IServiceCollection AddDealsReconciliation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScheduledTask<DealsReconciliationTask>("DealsReconciliation");

        if (configuration.GetValue<bool>("DealsReconciliation:UseInMemorySources"))
        {
            // Фабрика вызывается в scope каждого запуска — демо-сделки всегда за вчера.
            services.AddScoped<IDealsSource>(sp => new InMemoryDealsSource("СистемаА", DemoDeals.SystemA(Yesterday(sp))));
            services.AddScoped<IDealsSource>(sp => new InMemoryDealsSource("СистемаБ", DemoDeals.SystemB(Yesterday(sp))));
            return services;
        }

        services.AddDbContext<MssqlDealsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("MssqlDeals")));
        services.AddDbContext<OracleDealsDbContext>(options =>
            options.UseOracle(configuration.GetConnectionString("OracleDeals")));

        services.AddScoped<IDealsSource, MssqlDealsSource>();
        services.AddScoped<IDealsSource, OracleDealsSource>();

        return services;
    }

    private static DateTime Yesterday(IServiceProvider services) =>
        services.GetRequiredService<TimeProvider>().GetLocalNow().Date.AddDays(-1);
}

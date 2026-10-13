using CronTaskRunner.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Точка входа: при старте проверяет конфигурацию событий и для каждого
/// события запускает свой независимый CronEventRunner (все — конкурентно).
/// Ошибка в одном раннере не роняет остальные — она логируется, и
/// останавливается только этот раннер.
/// </summary>
public sealed class ScheduledEventsDispatcher(
    IOptionsMonitor<ScheduledEventsOptions> optionsMonitor,
    ScheduledTaskRegistry registry,
    CronEventRunner eventRunner,
    ILogger<ScheduledEventsDispatcher> logger) : BackgroundService
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var errors = ScheduledEventsValidator.Validate(optionsMonitor.CurrentValue, registry);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Некорректная конфигурация событий расписания:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(e => " - " + e)));
        }

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Набор событий фиксируется при старте: добавление события требует
        // рестарта, а Enabled и CronExpressions существующего события
        // раннер перечитывает из конфигурации сам.
        var eventNames = optionsMonitor.CurrentValue.Events.Select(e => e.Name).ToList();

        if (eventNames.Count == 0)
        {
            logger.LogWarning("В конфигурации нет ни одного события расписания");
        }

        return Task.WhenAll(eventNames.Select(name => RunEventSafelyAsync(name, stoppingToken)));
    }

    private async Task RunEventSafelyAsync(string eventName, CancellationToken stoppingToken)
    {
        try
        {
            await eventRunner.RunAsync(eventName, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "[{Event}] Раннер события аварийно завершился", eventName);
        }
    }
}

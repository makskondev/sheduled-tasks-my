using CronTaskRunner.Core.Abstractions;
using CronTaskRunner.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Следит за расписанием одного события (по имени) и запускает его задачу
/// при каждом срабатывании cron.
/// <para>
/// Ожидание идёт отрезками не длиннее <see cref="ConfigRefreshInterval"/>:
/// после каждого отрезка определение события перечитывается из
/// IOptionsMonitor, поэтому изменения Enabled/CronExpressions в
/// appsettings.json подхватываются без рестарта не позже чем через минуту.
/// </para>
/// <para>
/// Задача стартует в отдельном потоке, чтобы её выполнение не мешало
/// отсчитывать следующее срабатывание. Параллельные запуски одной задачи
/// не допускаются: если предыдущий запуск ещё идёт, срабатывание
/// пропускается. При остановке хоста раннер дожидается текущего запуска.
/// </para>
/// </summary>
public sealed class CronEventRunner(
    ScheduledTaskRegistry registry,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<ScheduledEventsOptions> optionsMonitor,
    TimeProvider timeProvider,
    ILogger<CronEventRunner> logger)
{
    internal static readonly TimeSpan ConfigRefreshInterval = TimeSpan.FromMinutes(1);

    public async Task RunAsync(string eventName, CancellationToken stoppingToken)
    {
        var schedule = new ScheduleCache();
        DateTimeOffset? announced = DateTimeOffset.MinValue;
        Task? running = null;
        var wasEnabled = true;

        // Срабатывания ищутся строго после cursor: так одно и то же
        // срабатывание не запустится дважды, даже если таймер проснулся
        // на миллисекунду раньше.
        var cursor = timeProvider.GetUtcNow();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var definition = FindDefinition(eventName);
                if (definition is null)
                {
                    logger.LogWarning("[{Event}] Событие удалено из конфигурации, раннер остановлен", eventName);
                    return;
                }

                schedule.Refresh(definition.CronExpressions, ex => logger.LogError(
                    ex, "[{Event}] Расписание из конфигурации не применено", eventName));

                var now = timeProvider.GetUtcNow();

                var justEnabled = definition.Enabled && !wasEnabled;
                wasEnabled = definition.Enabled;

                if (justEnabled)
                {
                    // Срабатывания, пришедшиеся на время, пока событие было
                    // выключено, не запускаем.
                    cursor = now;
                }
                else if (cursor < now - ConfigRefreshInterval)
                {
                    // Хост спал или прыгнули системные часы: пропущенные
                    // срабатывания не догоняем пачкой, берём только последнюю минуту.
                    cursor = now - ConfigRefreshInterval;
                }

                var next = definition.Enabled
                    ? schedule.Current?.GetNextOccurrence(cursor, timeProvider.LocalTimeZone)
                    : null;

                if (next is { } due && due <= now)
                {
                    cursor = due;
                    running = StartTask(eventName, definition.TaskKey, running, stoppingToken);
                    continue;
                }

                // Между cursor и now срабатываний не было. Сдвигаем cursor, чтобы
                // выражение, добавленное в конфиг «задним числом», не запустило
                // задачу за уже прошедшее время.
                cursor = now;

                if (next != announced)
                {
                    LogNext(eventName, next);
                    announced = next;
                }

                var delay = next is { } upcoming && upcoming - now < ConfigRefreshInterval
                    ? upcoming - now
                    : ConfigRefreshInterval;

                await Task.Delay(delay, timeProvider, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // штатная остановка хоста
        }
        finally
        {
            if (running is not null)
            {
                await running;
            }
        }
    }

    private Task StartTask(string eventName, string taskKey, Task? running, CancellationToken stoppingToken)
    {
        if (running is { IsCompleted: false })
        {
            logger.LogWarning(
                "[{Event}] Предыдущий запуск задачи {TaskKey} ещё выполняется, срабатывание пропущено",
                eventName, taskKey);
            return running;
        }

        // Не await'им: иначе следующее срабатывание ждало бы конца задачи.
        return Task.Run(() => ExecuteTaskAsync(eventName, taskKey, stoppingToken));
    }

    private async Task ExecuteTaskAsync(string eventName, string taskKey, CancellationToken stoppingToken)
    {
        var startedAt = timeProvider.GetTimestamp();

        try
        {
            // Scope живёт ровно столько, сколько выполняется задача, —
            // важно для scoped-зависимостей вроде DbContext.
            await using var scope = scopeFactory.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<ScheduledEventContext>().EventName = eventName;
            var task = (IScheduledTask)scope.ServiceProvider.GetRequiredService(registry.Resolve(taskKey));

            logger.LogInformation("[{Event}] Старт задачи {Task}", eventName, task.Name);
            await task.RunAsync(stoppingToken);
            logger.LogInformation(
                "[{Event}] Задача {Task} завершена за {Elapsed}",
                eventName, task.Name, timeProvider.GetElapsedTime(startedAt));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning("[{Event}] Задача {TaskKey} прервана остановкой хоста", eventName, taskKey);
        }
        catch (Exception ex)
        {
            // Задача запущена без await — исключение, не пойманное здесь, никто не увидит.
            logger.LogError(ex, "[{Event}] Ошибка выполнения задачи {TaskKey}", eventName, taskKey);
        }
    }

    private ScheduledEventDefinition? FindDefinition(string eventName)
    {
        return optionsMonitor.CurrentValue.Events
            .FirstOrDefault(e => string.Equals(e.Name, eventName, StringComparison.OrdinalIgnoreCase));
    }

    private void LogNext(string eventName, DateTimeOffset? next)
    {
        if (next is null)
        {
            logger.LogInformation("[{Event}] Событие выключено или у расписания нет будущих срабатываний", eventName);
            return;
        }

        var nextLocal = TimeZoneInfo.ConvertTime(next.Value, timeProvider.LocalTimeZone);
        logger.LogInformation(
            "[{Event}] Следующий запуск в {Next} (через {Delay})",
            eventName, nextLocal, next.Value - timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Последнее успешно разобранное расписание события. Разбор повторяется
    /// только при изменении выражений в конфиге, поэтому ошибка в новом
    /// конфиге логируется один раз, а событие продолжает работать по
    /// прежнему расписанию.
    /// </summary>
    private sealed class ScheduleCache
    {
        private string? _signature;

        public CronSchedule? Current { get; private set; }

        public void Refresh(IReadOnlyCollection<string> expressions, Action<FormatException> onError)
        {
            var signature = string.Join('\n', expressions);
            if (signature == _signature)
            {
                return;
            }

            _signature = signature;
            try
            {
                Current = CronSchedule.Parse(expressions);
            }
            catch (FormatException ex)
            {
                onError(ex);
            }
        }
    }
}

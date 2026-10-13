using CronTaskRunner.Core.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CronTaskRunner.Core.Configuration;

/// <summary>
/// Реализация <see cref="IEventOptions{TOptions}"/>: биндит подсекцию Options
/// события, из которого запущена задача. Нет события или секции Options —
/// значения по умолчанию из класса настроек.
/// </summary>
internal sealed class EventOptions<TOptions>(ScheduledEventContext context, ScheduledEventsConfigSource source)
    : IEventOptions<TOptions>
    where TOptions : class, new()
{
    private readonly Lazy<TOptions> _value = new(() => Bind(context, source));

    public TOptions Value => _value.Value;

    private static TOptions Bind(ScheduledEventContext context, ScheduledEventsConfigSource source)
    {
        if (string.IsNullOrEmpty(context.EventName))
        {
            throw new InvalidOperationException(
                $"IEventOptions<{typeof(TOptions).Name}> доступен только внутри запуска задачи по событию расписания");
        }

        return source.FindOptions(context.EventName)?.Get<TOptions>() ?? new TOptions();
    }
}

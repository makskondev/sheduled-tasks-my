namespace CronTaskRunner.Core.Configuration;

/// <summary>
/// Контекст запуска задачи: из какого события расписания она запущена.
/// Живёт в DI-scope одного запуска; имя события проставляет CronEventRunner
/// до создания задачи.
/// </summary>
public sealed class ScheduledEventContext
{
    /// <summary>Имя события (поле Name элемента ScheduledEvents:Events[]).</summary>
    public string EventName { get; internal set; } = string.Empty;
}

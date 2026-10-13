namespace CronTaskRunner.Core.Abstractions;

/// <summary>
/// Настройки задачи из элемента события ScheduledEvents:Events[]:Options,
/// запустившего текущий запуск. Заменяет отдельную секцию конфига на каждую
/// задачу: расписание и параметры задачи лежат в одном месте, а одну и ту же
/// задачу можно запускать разными событиями с разными настройками.
/// <para>
/// Регистрируется как scoped: значение читается из конфигурации один раз за
/// запуск (как IOptionsSnapshot), правки appsettings.json действуют со
/// следующего запуска.
/// </para>
/// </summary>
/// <typeparam name="TOptions">Тип настроек задачи.</typeparam>
public interface IEventOptions<TOptions>
    where TOptions : class, new()
{
    TOptions Value { get; }
}

namespace CronTaskRunner.Core.Configuration;

/// <summary>
/// Описание одного события расписания из конфигурации:
/// когда запускать (набор cron-выражений) и какую задачу.
/// <para>
/// Настройки самой задачи лежат в подсекции <c>Options</c> элемента события
/// и в эту модель не биндятся: их тип знает только задача, она получает их
/// через <see cref="Abstractions.IEventOptions{TOptions}"/>.
/// </para>
/// </summary>
public class ScheduledEventDefinition
{
    /// <summary>Имя события (для логов).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Включено ли событие. Можно выключить без пересборки.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Ключ задачи — соответствует ключу, под которым задача
    /// зарегистрирована через AddScheduledTask&lt;T&gt;(key).
    /// </summary>
    public string TaskKey { get; set; } = string.Empty;

    /// <summary>
    /// Расписание в виде набора cron-выражений (5-полевой формат:
    /// минута час день-месяца месяц день-недели).
    /// Задача запускается по срабатыванию любого из них.
    /// </summary>
    public List<string> CronExpressions { get; set; } = new();
}

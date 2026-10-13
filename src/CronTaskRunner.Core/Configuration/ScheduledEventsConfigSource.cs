using Microsoft.Extensions.Configuration;

namespace CronTaskRunner.Core.Configuration;

/// <summary>
/// Доступ к «живой» секции ScheduledEvents: по имени события находит его
/// подсекцию Options. Секция читается из IConfiguration при каждом обращении,
/// поэтому изменения appsettings.json подхватываются без рестарта.
/// </summary>
public sealed class ScheduledEventsConfigSource(IConfigurationSection root)
{
    /// <summary>Имя подсекции элемента события с настройками задачи.</summary>
    public const string OptionsKey = "Options";

    /// <summary>
    /// Подсекция Options события с указанным именем (регистр не важен).
    /// Если события нет, возвращается null; если у события нет Options —
    /// пустая секция (биндер отдаст значения по умолчанию).
    /// </summary>
    public IConfigurationSection? FindOptions(string eventName)
    {
        var item = root.GetSection(nameof(ScheduledEventsOptions.Events))
            .GetChildren()
            .FirstOrDefault(e => string.Equals(
                e[nameof(ScheduledEventDefinition.Name)], eventName, StringComparison.OrdinalIgnoreCase));

        return item?.GetSection(OptionsKey);
    }
}

using CronTaskRunner.Core.Configuration;

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Проверка конфигурации событий при старте хоста: ошибки в именах,
/// ключах задач и cron-выражениях включённых событий должны ронять запуск
/// сразу, а не всплывать в логах в момент срабатывания.
/// </summary>
internal static class ScheduledEventsValidator
{
    public static IReadOnlyList<string> Validate(ScheduledEventsOptions options, ScheduledTaskRegistry registry)
    {
        var errors = new List<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < options.Events.Count; i++)
        {
            var definition = options.Events[i];
            var label = string.IsNullOrWhiteSpace(definition.Name) ? $"Events[{i}]" : $"'{definition.Name}'";

            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                errors.Add($"{label}: не задано имя события");
            }
            else if (!names.Add(definition.Name))
            {
                errors.Add($"{label}: имя события повторяется");
            }

            // Выключенное событие не запускается — его задача может быть
            // не зарегистрирована в этом окружении. Если событие включат на лету
            // с ошибкой, раннер залогирует её.
            if (!definition.Enabled)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(definition.TaskKey))
            {
                errors.Add($"{label}: не задан TaskKey");
            }
            else if (!registry.Contains(definition.TaskKey))
            {
                errors.Add($"{label}: задача с ключом '{definition.TaskKey}' не зарегистрирована через AddScheduledTask<T>(key)");
            }

            try
            {
                CronSchedule.Parse(definition.CronExpressions);
            }
            catch (FormatException ex)
            {
                errors.Add($"{label}: {ex.Message}");
            }
        }

        return errors;
    }
}

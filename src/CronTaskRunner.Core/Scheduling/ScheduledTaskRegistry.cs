namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Реестр «ключ задачи из конфига» -> «CLR-тип задачи», собирается
/// из всех вызовов AddScheduledTask&lt;T&gt;(key). Ключи регистронезависимы.
/// </summary>
public sealed class ScheduledTaskRegistry
{
    private readonly Dictionary<string, Type> _map = new(StringComparer.OrdinalIgnoreCase);

    public ScheduledTaskRegistry(IEnumerable<TaskRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            if (!_map.TryAdd(registration.Key, registration.TaskType))
            {
                throw new InvalidOperationException(
                    $"Задача с ключом '{registration.Key}' уже зарегистрирована " +
                    $"(тип {_map[registration.Key].Name}). Ключи задач в AddScheduledTask<T>(key) должны быть уникальны.");
            }
        }
    }

    public bool Contains(string taskKey) => _map.ContainsKey(taskKey);

    public Type Resolve(string taskKey)
    {
        if (!_map.TryGetValue(taskKey, out var type))
        {
            throw new InvalidOperationException(
                $"Задача с ключом '{taskKey}' не зарегистрирована. " +
                $"Используйте services.AddScheduledTask<T>(\"{taskKey}\") при старте приложения.");
        }

        return type;
    }
}

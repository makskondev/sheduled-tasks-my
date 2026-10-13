using Microsoft.Extensions.Options;

namespace CronTaskRunner.Core.Tests.Scheduling;

/// <summary>IOptionsMonitor, значение которого тест меняет «на лету», как при перезагрузке appsettings.json.</summary>
internal sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    private volatile object _value = value!;

    public T CurrentValue
    {
        get => (T)_value;
        set => _value = value!;
    }

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

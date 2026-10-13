using CronTaskRunner.Core.Abstractions;

namespace CronTaskRunner.Sample.Tests.Fakes;

internal sealed class TestEventOptions<T>(T value) : IEventOptions<T>
    where T : class, new()
{
    public T Value => value;
}

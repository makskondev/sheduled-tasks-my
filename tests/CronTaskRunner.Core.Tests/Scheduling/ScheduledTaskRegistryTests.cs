using CronTaskRunner.Core.Scheduling;

namespace CronTaskRunner.Core.Tests.Scheduling;

public class ScheduledTaskRegistryTests
{
    [Fact]
    public void Resolve_IgnoresKeyCase()
    {
        var registry = new ScheduledTaskRegistry([new TaskRegistration("MyTask", typeof(string))]);

        Assert.Equal(typeof(string), registry.Resolve("mytask"));
        Assert.True(registry.Contains("MYTASK"));
    }

    [Fact]
    public void Constructor_DuplicateKey_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new ScheduledTaskRegistry(
        [
            new TaskRegistration("MyTask", typeof(string)),
            new TaskRegistration("mytask", typeof(int)),
        ]));

        Assert.Contains("mytask", ex.Message);
    }

    [Fact]
    public void Resolve_UnknownKey_Throws()
    {
        var registry = new ScheduledTaskRegistry([]);

        Assert.Throws<InvalidOperationException>(() => registry.Resolve("Missing"));
    }
}

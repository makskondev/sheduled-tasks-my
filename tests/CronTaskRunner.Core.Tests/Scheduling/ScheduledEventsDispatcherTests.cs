using CronTaskRunner.Core.Configuration;
using CronTaskRunner.Core.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CronTaskRunner.Core.Tests.Scheduling;

public class ScheduledEventsDispatcherTests
{
    [Fact]
    public async Task StartAsync_InvalidConfig_FailsHostStartWithAllErrors()
    {
        var options = new TestOptionsMonitor<ScheduledEventsOptions>(new ScheduledEventsOptions
        {
            Events =
            [
                new ScheduledEventDefinition { Name = "A", TaskKey = "Unknown", CronExpressions = ["bad"] },
            ],
        });
        var registry = new ScheduledTaskRegistry([]);
        var runner = new CronEventRunner(
            registry,
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            options,
            TimeProvider.System,
            NullLogger<CronEventRunner>.Instance);
        using var dispatcher = new ScheduledEventsDispatcher(
            options, registry, runner, NullLogger<ScheduledEventsDispatcher>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.StartAsync(CancellationToken.None));

        Assert.Contains("'Unknown' не зарегистрирована", ex.Message);
        Assert.Contains("'bad'", ex.Message);
    }
}

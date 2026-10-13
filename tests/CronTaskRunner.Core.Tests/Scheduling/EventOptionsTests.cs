using CronTaskRunner.Core.Abstractions;
using CronTaskRunner.Core.Configuration;
using CronTaskRunner.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CronTaskRunner.Core.Tests.Scheduling;

public class EventOptionsTests
{
    [Fact]
    public void BindsOptionsOfTheEventThatStartedTheRun()
    {
        using var services = Build(new()
        {
            ["ScheduledEvents:Events:0:Name"] = "Nightly",
            ["ScheduledEvents:Events:0:Options:To:0"] = "night@test",
            ["ScheduledEvents:Events:0:Options:To:1"] = "night2@test",
            ["ScheduledEvents:Events:0:Options:Days"] = "1",
            ["ScheduledEvents:Events:1:Name"] = "Weekly",
            ["ScheduledEvents:Events:1:Options:To:0"] = "week@test",
            ["ScheduledEvents:Events:1:Options:Days"] = "7",
        });

        var nightly = Resolve(services, "Nightly");
        var weekly = Resolve(services, "Weekly");

        Assert.Equal(["night@test", "night2@test"], nightly.To);
        Assert.Equal(1, nightly.Days);
        Assert.Equal(["week@test"], weekly.To);
        Assert.Equal(7, weekly.Days);
    }

    [Fact]
    public void EventNameLookup_IgnoresCase()
    {
        using var services = Build(new()
        {
            ["ScheduledEvents:Events:0:Name"] = "Nightly",
            ["ScheduledEvents:Events:0:Options:To:0"] = "night@test",
        });

        Assert.Equal(["night@test"], Resolve(services, "NIGHTLY").To);
    }

    [Fact]
    public void EventWithoutOptions_GivesDefaults()
    {
        using var services = Build(new()
        {
            ["ScheduledEvents:Events:0:Name"] = "Nightly",
        });

        var options = Resolve(services, "Nightly");

        Assert.Empty(options.To);
        Assert.Equal(1, options.Days);
    }

    [Fact]
    public void ConfigChange_IsVisibleToNextRunButNotToRunInProgress()
    {
        var source = new Dictionary<string, string?>
        {
            ["ScheduledEvents:Events:0:Name"] = "Nightly",
            ["ScheduledEvents:Events:0:Options:To:0"] = "old@test",
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(source).Build();
        using var services = BuildFrom(config);

        using var firstRun = services.CreateScope();
        SetEvent(firstRun, "Nightly");
        var firstOptions = firstRun.ServiceProvider.GetRequiredService<IEventOptions<TestOptions>>();
        Assert.Equal(["old@test"], firstOptions.Value.To);

        config["ScheduledEvents:Events:0:Options:To:0"] = "new@test";

        Assert.Equal(["old@test"], firstOptions.Value.To);

        using var nextRun = services.CreateScope();
        SetEvent(nextRun, "Nightly");
        Assert.Equal(["new@test"], nextRun.ServiceProvider.GetRequiredService<IEventOptions<TestOptions>>().Value.To);
    }

    [Fact]
    public void OutsideEventRun_Throws()
    {
        using var services = Build(new());
        using var scope = services.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<IEventOptions<TestOptions>>();

        Assert.Throws<InvalidOperationException>(() => options.Value);
    }

    private static TestOptions Resolve(ServiceProvider services, string eventName)
    {
        using var scope = services.CreateScope();
        SetEvent(scope, eventName);
        return scope.ServiceProvider.GetRequiredService<IEventOptions<TestOptions>>().Value;
    }

    // Имя события в реальности проставляет CronEventRunner (internal set).
    private static void SetEvent(IServiceScope scope, string eventName) =>
        scope.ServiceProvider.GetRequiredService<ScheduledEventContext>().EventName = eventName;

    private static ServiceProvider Build(Dictionary<string, string?> values) =>
        BuildFrom(new ConfigurationBuilder().AddInMemoryCollection(values).Build());

    private static ServiceProvider BuildFrom(IConfiguration configuration) =>
        new ServiceCollection().AddCronTaskRunner(configuration).BuildServiceProvider();

    public sealed class TestOptions
    {
        public List<string> To { get; set; } = [];

        public int Days { get; set; } = 1;
    }
}

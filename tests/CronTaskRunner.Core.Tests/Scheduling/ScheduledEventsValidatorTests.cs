using CronTaskRunner.Core.Configuration;
using CronTaskRunner.Core.Scheduling;

namespace CronTaskRunner.Core.Tests.Scheduling;

public class ScheduledEventsValidatorTests
{
    private readonly ScheduledTaskRegistry _registry = new([new TaskRegistration("Known", typeof(string))]);

    [Fact]
    public void Validate_CorrectConfig_NoErrors()
    {
        var errors = Validate(Event("A"), Event("B"));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_DuplicateNames_IgnoringCase()
    {
        var errors = Validate(Event("Nightly"), Event("nightly"));

        Assert.Equal(["'nightly': имя события повторяется"], errors);
    }

    [Fact]
    public void Validate_EmptyName()
    {
        var errors = Validate(Event(""));

        Assert.Equal(["Events[0]: не задано имя события"], errors);
    }

    [Fact]
    public void Validate_UnknownTaskKey()
    {
        var errors = Validate(Event("A", taskKey: "Unknown"));

        Assert.Single(errors, e => e.Contains("'Unknown' не зарегистрирована"));
    }

    [Fact]
    public void Validate_InvalidCron()
    {
        var errors = Validate(Event("A", cron: "not a cron"));

        Assert.Single(errors, e => e.StartsWith("'A': Некорректное cron-выражение 'not a cron'"));
    }

    [Fact]
    public void Validate_DisabledEvent_SkipsTaskKeyAndCronChecks()
    {
        var disabled = Event("A", taskKey: "Unknown", cron: "not a cron");
        disabled.Enabled = false;

        Assert.Empty(Validate(disabled));
    }

    private IReadOnlyList<string> Validate(params ScheduledEventDefinition[] events) =>
        ScheduledEventsValidator.Validate(new ScheduledEventsOptions { Events = [.. events] }, _registry);

    private static ScheduledEventDefinition Event(string name, string taskKey = "Known", string cron = "0 3 * * *") =>
        new() { Name = name, TaskKey = taskKey, CronExpressions = [cron] };
}

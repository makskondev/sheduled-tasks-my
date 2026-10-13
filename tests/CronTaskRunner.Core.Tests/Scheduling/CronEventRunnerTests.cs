using System.Collections.Concurrent;
using CronTaskRunner.Core.Abstractions;
using CronTaskRunner.Core.Configuration;
using CronTaskRunner.Core.Extensions;
using CronTaskRunner.Core.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace CronTaskRunner.Core.Tests.Scheduling;

public class CronEventRunnerTests
{
    private const string EventName = "Event";
    private const string EveryMinute = "* * * * *";

    private static readonly DateTimeOffset StartTime = new(2026, 10, 8, 10, 0, 30, TimeSpan.Zero);

    [Fact]
    public async Task RunsTaskOncePerOccurrence()
    {
        await using var harness = Harness.Start(Event(EveryMinute));

        await harness.AdvanceAsync(TimeSpan.FromSeconds(29));
        Assert.Empty(harness.Probe.StartedAt);

        await harness.AdvanceUntilAsync(() => harness.Probe.StartedAt.Count == 1);
        Assert.InRange(harness.Probe.StartedAt.Single(), StartTime.AddSeconds(30), StartTime.AddSeconds(40));

        await harness.AdvanceAsync(TimeSpan.FromSeconds(30));
        Assert.Single(harness.Probe.StartedAt);
    }

    [Fact]
    public async Task TaskScope_KnowsEventNameThatStartedIt()
    {
        await using var harness = Harness.Start(Event(EveryMinute));

        await harness.AdvanceUntilAsync(() => harness.Probe.EventNames.Count == 1);

        Assert.Equal(EventName, harness.Probe.EventNames.Single());
    }

    [Fact]
    public async Task DisabledEvent_StartsAfterBeingEnabledWithoutRestart()
    {
        await using var harness = Harness.Start(Event(EveryMinute, enabled: false));

        await harness.AdvanceAsync(TimeSpan.FromMinutes(3));
        Assert.Empty(harness.Probe.StartedAt);

        harness.Options.CurrentValue = Options(Event(EveryMinute));

        await harness.AdvanceUntilAsync(() => harness.Probe.StartedAt.Count == 1);
    }

    [Fact]
    public async Task EnabledEvent_DoesNotRunOccurrenceFromDisabledPeriod()
    {
        await using var harness = Harness.Start(Event("0 3 * * *", enabled: false));

        // Раннер увидел выключенное событие в 10:00:30 и перечитает конфиг в 10:01:30.
        // Срабатывание 10:01 приходится на время, пока событие ещё выключено,
        // включаем его в 10:01:05.
        await harness.AdvanceAsync(TimeSpan.FromSeconds(35));
        harness.Options.CurrentValue = Options(Event("1 10 * * *"));

        await harness.AdvanceAsync(TimeSpan.FromMinutes(2));
        Assert.Empty(harness.Probe.StartedAt);
    }

    [Fact]
    public async Task ClockJump_DoesNotReplayMissedOccurrences()
    {
        await using var harness = Harness.Start(Event(EveryMinute));
        await harness.AdvanceUntilAsync(() => harness.Probe.StartedAt.Count == 1);

        // Хост «проспал» два часа: системные часы прыгнули вперёд разом.
        harness.Time.Advance(TimeSpan.FromHours(2));
        await harness.AdvanceAsync(TimeSpan.FromSeconds(30));

        Assert.InRange(harness.Probe.StartedAt.Count, 2, 3);
    }

    [Fact]
    public async Task ChangedCronExpression_AppliedWithoutRestart()
    {
        await using var harness = Harness.Start(Event("0 3 * * *"));

        await harness.AdvanceAsync(TimeSpan.FromSeconds(10));
        harness.Options.CurrentValue = Options(Event(EveryMinute));

        await harness.AdvanceUntilAsync(() => harness.Probe.StartedAt.Count == 1, TimeSpan.FromMinutes(3));
    }

    [Fact]
    public async Task InvalidCronAfterReload_KeepsPreviousSchedule()
    {
        await using var harness = Harness.Start(Event(EveryMinute));

        await harness.AdvanceAsync(TimeSpan.FromSeconds(10));
        harness.Options.CurrentValue = Options(Event("not a cron"));

        await harness.AdvanceUntilAsync(() => harness.Probe.StartedAt.Count == 2, TimeSpan.FromMinutes(3));
    }

    [Fact]
    public async Task SkipsOccurrence_WhilePreviousRunIsInProgress()
    {
        await using var harness = Harness.Start(Event(EveryMinute));
        var gate = harness.Probe.BlockRuns();

        await harness.AdvanceUntilAsync(() => harness.Probe.StartedAt.Count == 1);
        await harness.AdvanceAsync(TimeSpan.FromMinutes(2));
        Assert.Single(harness.Probe.StartedAt);

        gate.SetResult();
        await harness.AdvanceUntilAsync(() => harness.Probe.StartedAt.Count == 2);
    }

    [Fact]
    public async Task Stop_WaitsForRunningTask()
    {
        await using var harness = Harness.Start(Event(EveryMinute));
        var gate = harness.Probe.BlockRuns(ignoreCancellation: true);
        await harness.AdvanceUntilAsync(() => harness.Probe.StartedAt.Count == 1);

        harness.Stop();
        await Task.Delay(100);
        Assert.False(harness.Running.IsCompleted);

        gate.SetResult();
        await harness.Running.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EventRemovedFromConfig_RunnerStops()
    {
        await using var harness = Harness.Start(Event("0 3 * * *"));

        harness.Options.CurrentValue = Options();

        await harness.AdvanceUntilAsync(() => harness.Running.IsCompleted);
        Assert.Empty(harness.Probe.StartedAt);
    }

    private static ScheduledEventDefinition Event(string cron, bool enabled = true) =>
        new() { Name = EventName, Enabled = enabled, TaskKey = "probe", CronExpressions = [cron] };

    private static ScheduledEventsOptions Options(params ScheduledEventDefinition[] events) =>
        new() { Events = [.. events] };

    private sealed class Harness : IAsyncDisposable
    {
        private readonly CancellationTokenSource _stopping = new();
        private readonly ServiceProvider _services;

        private Harness(ScheduledEventDefinition definition)
        {
            Time = new FakeTimeProvider(StartTime);
            Options = new TestOptionsMonitor<ScheduledEventsOptions>(CronEventRunnerTests.Options(definition));
            Probe = new TaskProbe(Time);

            _services = new ServiceCollection()
                .AddSingleton(Probe)
                .AddScoped<ScheduledEventContext>()
                .AddScheduledTask<ProbeTask>("probe")
                .BuildServiceProvider();

            var runner = new CronEventRunner(
                new ScheduledTaskRegistry(_services.GetServices<TaskRegistration>()),
                _services.GetRequiredService<IServiceScopeFactory>(),
                Options,
                Time,
                NullLogger<CronEventRunner>.Instance);

            Running = Task.Run(() => runner.RunAsync(EventName, _stopping.Token));
        }

        public FakeTimeProvider Time { get; }
        public TestOptionsMonitor<ScheduledEventsOptions> Options { get; }
        public TaskProbe Probe { get; }
        public Task Running { get; }

        public static Harness Start(ScheduledEventDefinition definition) => new(definition);

        /// <summary>
        /// Двигает фейковое время секундными шагами, давая раннеру
        /// реальное время на обработку каждого шага.
        /// </summary>
        public async Task AdvanceAsync(TimeSpan duration)
        {
            for (var elapsed = TimeSpan.Zero; elapsed < duration; elapsed += TimeSpan.FromSeconds(1))
            {
                await Step();
            }

            await Task.Delay(50);
        }

        public async Task AdvanceUntilAsync(Func<bool> condition, TimeSpan? limit = null)
        {
            var deadline = Time.GetUtcNow() + (limit ?? TimeSpan.FromMinutes(2));
            while (!condition())
            {
                Assert.True(Time.GetUtcNow() < deadline, "Условие не выполнилось за отведённое фейковое время");
                await Step();
            }
        }

        public void Stop() => _stopping.Cancel();

        public async ValueTask DisposeAsync()
        {
            Probe.ReleaseAll();
            _stopping.Cancel();
            await Running.WaitAsync(TimeSpan.FromSeconds(5));
            await _services.DisposeAsync();
            _stopping.Dispose();
        }

        private async Task Step()
        {
            await Task.Delay(2);
            Time.Advance(TimeSpan.FromSeconds(1));
        }
    }

    private sealed class TaskProbe(TimeProvider time)
    {
        private TaskCompletionSource? _gate;
        private bool _ignoreCancellation;

        public ConcurrentQueue<DateTimeOffset> StartedAt { get; } = new();

        public ConcurrentQueue<string> EventNames { get; } = new();

        public TaskCompletionSource BlockRuns(bool ignoreCancellation = false)
        {
            _ignoreCancellation = ignoreCancellation;
            _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return _gate;
        }

        public void ReleaseAll() => _gate?.TrySetResult();

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            StartedAt.Enqueue(time.GetUtcNow());
            if (_gate is not null)
            {
                await (_ignoreCancellation ? _gate.Task : _gate.Task.WaitAsync(cancellationToken));
            }
        }
    }

    private sealed class ProbeTask(TaskProbe probe, ScheduledEventContext context) : IScheduledTask
    {
        public string Name => "Probe";

        public Task RunAsync(CancellationToken cancellationToken)
        {
            probe.EventNames.Enqueue(context.EventName);
            return probe.RunAsync(cancellationToken);
        }
    }
}

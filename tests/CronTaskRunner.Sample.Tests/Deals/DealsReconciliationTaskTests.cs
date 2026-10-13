using CronTaskRunner.Sample.Deals;
using CronTaskRunner.Sample.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace CronTaskRunner.Sample.Tests.Deals;

public class DealsReconciliationTaskTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 3, 0, 0, TimeSpan.Zero);

    private readonly RecordingEmailSender _email = new();

    [Fact]
    public async Task RequestsSameClosedDaysPeriodFromAllSources()
    {
        var sourceA = new RecordingSource("A");
        var sourceB = new RecordingSource("B");

        await CreateTask([sourceA, sourceB], lookbackDays: 2).RunAsync(CancellationToken.None);

        var expected = (new DateTime(2026, 10, 6), new DateTime(2026, 10, 8));
        Assert.Equal(expected, sourceA.RequestedPeriod);
        Assert.Equal(expected, sourceB.RequestedPeriod);
    }

    [Fact]
    public async Task Discrepancies_SendsHtmlReport()
    {
        var yesterday = Now.Date.AddDays(-1);
        var sources = new IDealsSource[]
        {
            new InMemoryDealsSource("A", DemoDeals.SystemA(yesterday)),
            new InMemoryDealsSource("B", DemoDeals.SystemB(yesterday)),
        };

        await CreateTask(sources).RunAsync(CancellationToken.None);

        var message = Assert.Single(_email.Sent);
        Assert.Equal(["recon@test", "risk@test"], message.To);
        Assert.True(message.IsHtml);
        Assert.Equal("Сверка сделок: найдено расхождений — 3", message.Subject);
    }

    [Fact]
    public async Task NoDiscrepancies_NoEmail()
    {
        var yesterday = Now.Date.AddDays(-1);
        var sources = new IDealsSource[]
        {
            new InMemoryDealsSource("A", DemoDeals.SystemA(yesterday)),
            new InMemoryDealsSource("B", DemoDeals.SystemA(yesterday)),
        };

        await CreateTask(sources).RunAsync(CancellationToken.None);

        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task NoRecipients_FailsBeforeLoadingData()
    {
        var source = new RecordingSource("A");
        var task = new DealsReconciliationTask(
            [source, new RecordingSource("B")],
            _email,
            new TestEventOptions<DealsReconciliationOptions>(new() { To = [] }),
            new FakeTimeProvider(Now),
            NullLogger<DealsReconciliationTask>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => task.RunAsync(CancellationToken.None));
        Assert.Null(source.RequestedPeriod);
    }

    [Fact]
    public void LessThanTwoSources_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => CreateTask([new RecordingSource("A")]));
    }

    [Fact]
    public void DuplicateSourceNames_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => CreateTask([new RecordingSource("A"), new RecordingSource("a")]));

        Assert.Contains("повторяются: A", ex.Message);
    }

    private DealsReconciliationTask CreateTask(IEnumerable<IDealsSource> sources, int lookbackDays = 1) =>
        new(
            sources,
            _email,
            new TestEventOptions<DealsReconciliationOptions>(new() { To = ["recon@test", "risk@test"], LookbackDays = lookbackDays }),
            new FakeTimeProvider(Now),
            NullLogger<DealsReconciliationTask>.Instance);

    private sealed class RecordingSource(string name) : IDealsSource
    {
        public (DateTime From, DateTime To)? RequestedPeriod { get; private set; }

        public string SourceName => name;

        public Task<IReadOnlyCollection<DealRecord>> GetDealsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            RequestedPeriod = (from, to);
            return Task.FromResult<IReadOnlyCollection<DealRecord>>([]);
        }
    }
}

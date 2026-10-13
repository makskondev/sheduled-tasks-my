using CronTaskRunner.Sample.ExchangeRates;
using CronTaskRunner.Sample.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace CronTaskRunner.Sample.Tests.ExchangeRates;

public class ExchangeRateCheckTaskTests
{
    private readonly RecordingEmailSender _email = new();

    [Fact]
    public async Task ChangeAboveThreshold_SendsAlert()
    {
        await CreateTask(previous: 100m, current: 102m).RunAsync(CancellationToken.None);

        var message = Assert.Single(_email.Sent);
        Assert.Equal("fx@test", message.To);
        Assert.Equal("Курс изменился на 2.00 (2.00%)", message.Subject);
    }

    [Fact]
    public async Task ChangeBelowThreshold_NoAlert()
    {
        await CreateTask(previous: 100m, current: 101m).RunAsync(CancellationToken.None);

        Assert.Empty(_email.Sent);
    }

    private ExchangeRateCheckTask CreateTask(decimal previous, decimal current) =>
        new(
            new StubRateProvider(previous, current),
            _email,
            new TestEventOptions<ExchangeRateCheckOptions>(new() { To = "fx@test", ThresholdPercent = 1.5m }),
            NullLogger<ExchangeRateCheckTask>.Instance);

    private sealed class StubRateProvider(decimal previous, decimal current) : IExchangeRateProvider
    {
        public Task<decimal> GetCurrentRateAsync(CancellationToken cancellationToken) => Task.FromResult(current);

        public Task<decimal> GetLastSavedRateAsync(CancellationToken cancellationToken) => Task.FromResult(previous);
    }
}

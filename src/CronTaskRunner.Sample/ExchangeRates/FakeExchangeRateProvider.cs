namespace CronTaskRunner.Sample.ExchangeRates;

/// <summary>
/// Заглушка для демонстрации — возвращает случайные значения.
/// Замените на реальный клиент внешнего API / репозиторий БД.
/// </summary>
public sealed class FakeExchangeRateProvider : IExchangeRateProvider
{
    public Task<decimal> GetCurrentRateAsync(CancellationToken cancellationToken)
    {
        var rate = 90m + (decimal)(Random.Shared.NextDouble() * 4 - 2);
        return Task.FromResult(rate);
    }

    public Task<decimal> GetLastSavedRateAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(90m);
    }
}

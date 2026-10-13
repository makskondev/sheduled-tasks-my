namespace CronTaskRunner.Sample.ExchangeRates;

public interface IExchangeRateProvider
{
    Task<decimal> GetCurrentRateAsync(CancellationToken cancellationToken);
    Task<decimal> GetLastSavedRateAsync(CancellationToken cancellationToken);
}

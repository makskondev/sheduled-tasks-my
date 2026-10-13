namespace CronTaskRunner.Sample.ExchangeRates;

public sealed record ExchangeRateData(decimal CurrentRate, decimal PreviousRate);

public sealed record ExchangeRateComparisonResult(
    decimal CurrentRate,
    decimal PreviousRate,
    decimal Diff,
    decimal DiffPercent,
    bool ThresholdExceeded);

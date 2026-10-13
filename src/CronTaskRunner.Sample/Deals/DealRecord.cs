namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Сделка в унифицированном виде — то, во что каждый IDealsSource
/// приводит свои «родные» поля перед сравнением.
/// </summary>
public sealed record DealRecord(string DealId, decimal Amount, string Currency, DateTime TradeDate);

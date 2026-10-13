namespace CronTaskRunner.Sample.Deals;

/// <summary>Сделки всех источников за один и тот же период [From, To).</summary>
public sealed record DealsSnapshot(DateTime From, DateTime To, IReadOnlyList<SourceDeals> Sources);

public sealed record SourceDeals(string SourceName, IReadOnlyCollection<DealRecord> Deals);

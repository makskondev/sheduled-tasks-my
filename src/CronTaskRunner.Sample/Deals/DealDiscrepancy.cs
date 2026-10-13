namespace CronTaskRunner.Sample.Deals;

/// <param name="BySource">
/// Что видит каждый источник по этой сделке (SourceName -> запись, null если сделки там нет).
/// </param>
public sealed record DealDiscrepancy(
    string DealId,
    DiscrepancyKind Kind,
    string Description,
    IReadOnlyDictionary<string, DealRecord?> BySource);

namespace CronTaskRunner.Sample.Deals;

/// <param name="SourceNames">Источники в порядке регистрации — порядок колонок отчёта.</param>
public sealed record DealsReconciliationResult(
    DateTime From,
    DateTime To,
    IReadOnlyList<string> SourceNames,
    int TotalDealsChecked,
    IReadOnlyList<DealDiscrepancy> Discrepancies)
{
    public bool HasDiscrepancies => Discrepancies.Count > 0;
}

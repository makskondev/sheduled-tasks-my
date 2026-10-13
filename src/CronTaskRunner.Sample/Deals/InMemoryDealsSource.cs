namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Источник сделок в памяти — для демо-режима (DealsReconciliation:UseInMemorySources)
/// и тестов.
/// </summary>
public sealed class InMemoryDealsSource(string sourceName, IReadOnlyCollection<DealRecord> deals) : IDealsSource
{
    public string SourceName => sourceName;

    public Task<IReadOnlyCollection<DealRecord>> GetDealsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DealRecord> inPeriod = deals.Where(d => d.TradeDate >= from && d.TradeDate < to).ToList();
        return Task.FromResult(inPeriod);
    }
}

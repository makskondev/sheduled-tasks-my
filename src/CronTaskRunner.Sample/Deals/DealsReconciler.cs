namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Сравнение сделок из нескольких источников по DealId (без учёта регистра):
/// отсутствие в части источников, дубликаты внутри источника, расхождение
/// суммы/валюты/даты.
/// </summary>
public static class DealsReconciler
{
    public static DealsReconciliationResult Reconcile(DealsSnapshot snapshot)
    {
        var sources = snapshot.Sources
            .Select(s => (s.SourceName, Deals: s.Deals.ToLookup(d => d.DealId, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        var dealIds = snapshot.Sources
            .SelectMany(s => s.Deals, (_, deal) => deal.DealId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var discrepancies = new List<DealDiscrepancy>();
        foreach (var dealId in dealIds)
        {
            var matches = sources.Select(s => (s.SourceName, Records: s.Deals[dealId].ToList())).ToList();
            if (Compare(dealId, matches) is { } discrepancy)
            {
                discrepancies.Add(discrepancy);
            }
        }

        return new DealsReconciliationResult(
            snapshot.From,
            snapshot.To,
            snapshot.Sources.Select(s => s.SourceName).ToList(),
            dealIds.Count,
            discrepancies);
    }

    private static DealDiscrepancy? Compare(string dealId, List<(string SourceName, List<DealRecord> Records)> matches)
    {
        var bySource = matches.ToDictionary(m => m.SourceName, m => m.Records.FirstOrDefault());

        var duplicatedIn = matches
            .Where(m => m.Records.Count > 1)
            .Select(m => $"{m.SourceName} ({m.Records.Count})")
            .ToList();
        if (duplicatedIn.Count > 0)
        {
            return new DealDiscrepancy(
                dealId, DiscrepancyKind.DuplicateInSource, $"Несколько записей в: {string.Join(", ", duplicatedIn)}", bySource);
        }

        var missingIn = bySource.Where(kv => kv.Value is null).Select(kv => kv.Key).ToList();
        if (missingIn.Count > 0)
        {
            return new DealDiscrepancy(
                dealId, DiscrepancyKind.MissingInSomeSources, $"Отсутствует в: {string.Join(", ", missingIn)}", bySource);
        }

        var records = matches.Select(m => m.Records[0]).ToList();
        var reference = records[0];
        var mismatchedFields = new List<string>();

        if (records.Any(r => r.Amount != reference.Amount))
        {
            mismatchedFields.Add("сумма");
        }

        if (records.Any(r => !string.Equals(r.Currency, reference.Currency, StringComparison.OrdinalIgnoreCase)))
        {
            mismatchedFields.Add("валюта");
        }

        if (records.Any(r => r.TradeDate.Date != reference.TradeDate.Date))
        {
            mismatchedFields.Add("дата");
        }

        return mismatchedFields.Count == 0
            ? null
            : new DealDiscrepancy(
                dealId, DiscrepancyKind.FieldMismatch, $"Расходятся: {string.Join(", ", mismatchedFields)}", bySource);
    }
}

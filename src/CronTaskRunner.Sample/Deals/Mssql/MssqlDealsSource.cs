using Microsoft.EntityFrameworkCore;

namespace CronTaskRunner.Sample.Deals.Mssql;

/// <summary>
/// Источник сделок поверх MSSQL. Знает про «родную» структуру этой
/// базы (dbo.Deals, char(3) для валюты и т.д.) и приводит её к
/// единому DealRecord — остальной код сверки про MSSQL ничего не знает.
/// </summary>
public sealed class MssqlDealsSource(MssqlDealsDbContext db) : IDealsSource
{
    public string SourceName => "MSSQL";

    public async Task<IReadOnlyCollection<DealRecord>> GetDealsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var entities = await db.Deals
            .AsNoTracking()
            .Where(d => d.TradeDate >= from && d.TradeDate < to)
            .ToListAsync(cancellationToken);

        return entities
            .Select(e => new DealRecord(
                DealId: e.DealNumber.Trim(),
                Amount: e.Amount,
                Currency: e.CurrencyCode.Trim().ToUpperInvariant(),
                TradeDate: e.TradeDate))
            .ToList();
    }
}

using Microsoft.EntityFrameworkCore;

namespace CronTaskRunner.Sample.Deals.Oracle;

/// <summary>
/// Источник сделок поверх Oracle. Структура полностью отличается от
/// MSSQL (другая таблица, другие имена и типы колонок) — вся эта
/// специфика инкапсулирована здесь и наружу не протекает.
/// </summary>
public sealed class OracleDealsSource(OracleDealsDbContext db) : IDealsSource
{
    public string SourceName => "Oracle";

    public async Task<IReadOnlyCollection<DealRecord>> GetDealsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var entities = await db.Deals
            .AsNoTracking()
            .Where(d => d.OperDate >= from && d.OperDate < to)
            .ToListAsync(cancellationToken);

        return entities
            .Select(e => new DealRecord(
                DealId: e.DealId.Trim(),
                Amount: e.DealSum,
                Currency: e.CurCode.Trim().ToUpperInvariant(),
                TradeDate: e.OperDate.Date))
            .ToList();
    }
}

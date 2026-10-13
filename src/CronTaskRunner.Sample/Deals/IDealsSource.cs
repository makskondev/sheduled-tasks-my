namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Источник сделок — одна БД/система. Каждая реализация сама
/// знает, как её «родные» поля (разные имена/типы/форматы)
/// превратить в единый DealRecord.
/// Источники одной сверки опрашиваются параллельно, поэтому не должны
/// делить между собой DbContext или соединение.
/// </summary>
public interface IDealsSource
{
    /// <summary>Имя источника — колонка в отчёте (например, "MSSQL", "Oracle").</summary>
    string SourceName { get; }

    /// <summary>Сделки с датой заключения в полуинтервале [from, to).</summary>
    Task<IReadOnlyCollection<DealRecord>> GetDealsAsync(DateTime from, DateTime to, CancellationToken cancellationToken);
}

using Cronos;

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Расписание события — набор cron-выражений. Следующее срабатывание
/// события — ближайшее из срабатываний всех его выражений.
/// </summary>
internal sealed class CronSchedule
{
    private readonly IReadOnlyList<CronExpression> _expressions;

    private CronSchedule(IReadOnlyList<CronExpression> expressions)
    {
        _expressions = expressions;
    }

    /// <summary>
    /// Разбирает выражения в 5-полевом формате
    /// (минута час день-месяца месяц день-недели).
    /// </summary>
    /// <exception cref="FormatException">Пустой список или некорректное выражение.</exception>
    public static CronSchedule Parse(IReadOnlyCollection<string> expressions)
    {
        if (expressions.Count == 0)
        {
            throw new FormatException("Не задано ни одного cron-выражения");
        }

        var parsed = new List<CronExpression>(expressions.Count);
        foreach (var expression in expressions)
        {
            try
            {
                parsed.Add(CronExpression.Parse(expression));
            }
            catch (CronFormatException ex)
            {
                throw new FormatException($"Некорректное cron-выражение '{expression}': {ex.Message}", ex);
            }
        }

        return new CronSchedule(parsed);
    }

    /// <summary>
    /// Ближайшее срабатывание строго после <paramref name="from"/>
    /// или null, если срабатываний больше не будет.
    /// </summary>
    public DateTimeOffset? GetNextOccurrence(DateTimeOffset from, TimeZoneInfo timeZone)
    {
        return _expressions.Select(e => e.GetNextOccurrence(from, timeZone)).Min();
    }
}

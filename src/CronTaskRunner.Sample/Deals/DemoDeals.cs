namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Демо-данные двух «разных систем» за указанный день с заложенными
/// расхождениями: D-002 — разная сумма, D-003 есть только в А, D-004 — только в Б.
/// </summary>
public static class DemoDeals
{
    public static IReadOnlyCollection<DealRecord> SystemA(DateTime day) =>
    [
        new("D-001", 1000.00m, "RUB", day),
        new("D-002", 2500.50m, "USD", day),
        new("D-003", 750.00m, "RUB", day),
    ];

    public static IReadOnlyCollection<DealRecord> SystemB(DateTime day) =>
    [
        new("D-001", 1000.00m, "RUB", day),
        new("D-002", 2400.00m, "USD", day),
        new("D-004", 300.00m, "RUB", day),
    ];
}

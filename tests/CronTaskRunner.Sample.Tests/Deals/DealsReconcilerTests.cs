using CronTaskRunner.Sample.Deals;

namespace CronTaskRunner.Sample.Tests.Deals;

public class DealsReconcilerTests
{
    private static readonly DateTime Day = new(2026, 10, 7);

    [Fact]
    public void SameDeals_NoDiscrepancies_IgnoringIdAndCurrencyCase()
    {
        var result = Reconcile(
            ("A", [Deal("D-1", 100m, "RUB")]),
            ("B", [Deal("d-1", 100m, "rub")]));

        Assert.False(result.HasDiscrepancies);
        Assert.Equal(1, result.TotalDealsChecked);
    }

    [Fact]
    public void DealMissingInSource_ReportedWithSourceNames()
    {
        var result = Reconcile(
            ("A", [Deal("D-1"), Deal("D-2")]),
            ("B", [Deal("D-1")]),
            ("C", [Deal("D-1")]));

        var discrepancy = Assert.Single(result.Discrepancies);
        Assert.Equal("D-2", discrepancy.DealId);
        Assert.Equal(DiscrepancyKind.MissingInSomeSources, discrepancy.Kind);
        Assert.Equal("Отсутствует в: B, C", discrepancy.Description);
        Assert.Null(discrepancy.BySource["B"]);
    }

    [Fact]
    public void DifferentFields_ListsEachMismatchedField()
    {
        var result = Reconcile(
            ("A", [Deal("D-1", 100m, "RUB", Day)]),
            ("B", [Deal("D-1", 99m, "RUB", Day.AddDays(1))]));

        var discrepancy = Assert.Single(result.Discrepancies);
        Assert.Equal(DiscrepancyKind.FieldMismatch, discrepancy.Kind);
        Assert.Equal("Расходятся: сумма, дата", discrepancy.Description);
    }

    [Fact]
    public void TradeTimeIsIgnored_OnlyDateCompared()
    {
        var result = Reconcile(
            ("A", [Deal("D-1", date: Day)]),
            ("B", [Deal("D-1", date: Day.AddHours(15))]));

        Assert.False(result.HasDiscrepancies);
    }

    [Fact]
    public void DuplicateInSource_Reported()
    {
        var result = Reconcile(
            ("A", [Deal("D-1"), Deal("D-1")]),
            ("B", [Deal("D-1")]));

        var discrepancy = Assert.Single(result.Discrepancies);
        Assert.Equal(DiscrepancyKind.DuplicateInSource, discrepancy.Kind);
        Assert.Equal("Несколько записей в: A (2)", discrepancy.Description);
    }

    [Fact]
    public void Result_KeepsPeriodAndSourceOrder()
    {
        var result = Reconcile(("B", []), ("A", []));

        Assert.Equal(["B", "A"], result.SourceNames);
        Assert.Equal(Day, result.From);
        Assert.Equal(Day.AddDays(1), result.To);
    }

    private static DealsReconciliationResult Reconcile(params (string Name, DealRecord[] Deals)[] sources) =>
        DealsReconciler.Reconcile(new DealsSnapshot(
            Day, Day.AddDays(1), sources.Select(s => new SourceDeals(s.Name, s.Deals)).ToList()));

    private static DealRecord Deal(string id, decimal amount = 100m, string currency = "RUB", DateTime? date = null) =>
        new(id, amount, currency, date ?? Day);
}

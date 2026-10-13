using CronTaskRunner.Sample.Deals;

namespace CronTaskRunner.Sample.Tests.Deals;

public class DealsReportBuilderTests
{
    [Fact]
    public void BuildHtml_ShowsPeriodSourcesAndEncodesValues()
    {
        var day = new DateTime(2026, 10, 7);
        var deal = new DealRecord("D-<1>", 2500.5m, "USD", day);
        var result = new DealsReconciliationResult(
            day,
            day.AddDays(1),
            ["MSSQL", "Oracle & Co"],
            TotalDealsChecked: 1,
            [
                new DealDiscrepancy(
                    "D-<1>",
                    DiscrepancyKind.MissingInSomeSources,
                    "Отсутствует в: Oracle & Co",
                    new Dictionary<string, DealRecord?> { ["MSSQL"] = deal, ["Oracle & Co"] = null }),
            ]);

        var html = DealsReportBuilder.BuildHtml(result);

        Assert.Contains("Период: 2026-10-07 — 2026-10-07.", html);
        Assert.Contains("<th>Oracle &amp; Co</th>", html);
        Assert.Contains("<td>D-&lt;1&gt;</td>", html);
        Assert.Contains("<td>Нет в части источников</td>", html);
        Assert.Contains("<td>2500.50 USD (2026-10-07)</td><td>—</td>", html);
    }
}

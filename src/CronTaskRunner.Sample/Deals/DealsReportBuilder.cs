using System.Globalization;
using System.Net;
using System.Text;

namespace CronTaskRunner.Sample.Deals;

/// <summary>HTML-отчёт о расхождениях для письма.</summary>
public static class DealsReportBuilder
{
    public static string BuildHtml(DealsReconciliationResult result)
    {
        var html = new StringBuilder();
        html.Append("<html><body>");
        html.Append("<h2>Сверка сделок</h2>");
        html.Append(CultureInfo.InvariantCulture,
            $"<p>Период: {result.From:yyyy-MM-dd} — {result.To.AddDays(-1):yyyy-MM-dd}. " +
            $"Проверено сделок: {result.TotalDealsChecked}. Расхождений: {result.Discrepancies.Count}.</p>");

        html.Append("<table border='1' cellpadding='4' cellspacing='0'>");
        html.Append("<tr><th>DealId</th><th>Тип</th><th>Описание</th>");
        foreach (var sourceName in result.SourceNames)
        {
            html.Append($"<th>{Encode(sourceName)}</th>");
        }
        html.Append("</tr>");

        foreach (var discrepancy in result.Discrepancies)
        {
            html.Append("<tr>");
            html.Append($"<td>{Encode(discrepancy.DealId)}</td>");
            html.Append($"<td>{Encode(KindLabel(discrepancy.Kind))}</td>");
            html.Append($"<td>{Encode(discrepancy.Description)}</td>");

            foreach (var sourceName in result.SourceNames)
            {
                var record = discrepancy.BySource.GetValueOrDefault(sourceName);
                html.Append($"<td>{Encode(FormatRecord(record))}</td>");
            }

            html.Append("</tr>");
        }

        html.Append("</table></body></html>");
        return html.ToString();
    }

    private static string KindLabel(DiscrepancyKind kind) => kind switch
    {
        DiscrepancyKind.MissingInSomeSources => "Нет в части источников",
        DiscrepancyKind.FieldMismatch => "Расходятся поля",
        DiscrepancyKind.DuplicateInSource => "Дубликаты",
        _ => kind.ToString(),
    };

    private static string FormatRecord(DealRecord? record) => record is null
        ? "—"
        : string.Create(CultureInfo.InvariantCulture, $"{record.Amount:0.00} {record.Currency} ({record.TradeDate:yyyy-MM-dd})");

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}

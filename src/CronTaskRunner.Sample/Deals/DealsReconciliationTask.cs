using CronTaskRunner.Core.Abstractions;
using CronTaskRunner.Sample.Email;
using Microsoft.Extensions.Logging;

namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Сверка сделок: тянет сделки за один и тот же период из нескольких
/// источников (БД), сравнивает их по DealId (DealsReconciler), оформляет
/// расхождения в HTML-отчёт (DealsReportBuilder) и отправляет на почту.
/// Источники и их количество задаются через DI — задача не знает, что это за БД.
/// </summary>
public sealed class DealsReconciliationTask(
    IEnumerable<IDealsSource> sources,
    IEmailSender emailSender,
    IEventOptions<DealsReconciliationOptions> options,
    TimeProvider timeProvider,
    ILogger<DealsReconciliationTask> logger)
    : ScheduledTaskBase<DealsSnapshot, DealsReconciliationResult>
{
    private readonly IReadOnlyList<IDealsSource> _sources = EnsureValid(sources.ToList());

    public override string Name => "DealsReconciliation";

    protected override async Task<DealsSnapshot> GetDataAsync(CancellationToken cancellationToken)
    {
        var lookbackDays = options.Value.LookbackDays;
        if (lookbackDays < 1)
        {
            throw new InvalidOperationException($"Options:LookbackDays должен быть >= 1, задано {lookbackDays}");
        }

        // Период считается один раз и отдаётся всем источникам: если бы каждый
        // брал «сейчас» сам, загрузка через полночь дала бы источникам разные
        // окна и ложные расхождения. Текущий день не берём — его сделки ещё дописываются.
        var to = timeProvider.GetLocalNow().Date;
        var from = to.AddDays(-lookbackDays);

        logger.LogInformation(
            "[{Task}] Загружаем сделки за [{From:yyyy-MM-dd}, {To:yyyy-MM-dd}) из: {Sources}",
            Name, from, to, string.Join(", ", _sources.Select(s => s.SourceName)));

        // Источники независимы (разные БД и DbContext) — грузим параллельно.
        var perSource = await Task.WhenAll(_sources.Select(async source =>
            new SourceDeals(source.SourceName, await source.GetDealsAsync(from, to, cancellationToken))));

        return new DealsSnapshot(from, to, perSource);
    }

    protected override Task<DealsReconciliationResult> HandleAsync(DealsSnapshot snapshot, CancellationToken cancellationToken)
    {
        var result = DealsReconciler.Reconcile(snapshot);

        logger.LogInformation(
            "[{Task}] Проверено сделок: {Total}, расхождений: {Count}",
            Name, result.TotalDealsChecked, result.Discrepancies.Count);

        return Task.FromResult(result);
    }

    protected override async Task ProcessResultAsync(DealsReconciliationResult result, CancellationToken cancellationToken)
    {
        if (!result.HasDiscrepancies)
        {
            logger.LogInformation("[{Task}] Расхождений нет, письмо не отправляется", Name);
            return;
        }

        await emailSender.SendAsync(
            new EmailMessage(
                To: options.Value.To,
                Subject: $"Сверка сделок: найдено расхождений — {result.Discrepancies.Count}",
                Body: DealsReportBuilder.BuildHtml(result),
                IsHtml: true),
            cancellationToken);
    }

    private static IReadOnlyList<IDealsSource> EnsureValid(IReadOnlyList<IDealsSource> sources)
    {
        if (sources.Count < 2)
        {
            throw new InvalidOperationException(
                $"{nameof(DealsReconciliationTask)} требует минимум 2 источника сделок (зарегистрировано: {sources.Count})");
        }

        var duplicates = sources
            .GroupBy(s => s.SourceName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                $"Имена источников сделок должны быть уникальны, повторяются: {string.Join(", ", duplicates)}");
        }

        return sources;
    }
}

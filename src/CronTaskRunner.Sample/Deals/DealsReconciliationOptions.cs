namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Настройки сверки — подсекция "Options" события расписания с TaskKey = "DealsReconciliation"
/// (ScheduledEvents:Events[]:Options в appsettings.json).
/// </summary>
public sealed class DealsReconciliationOptions
{
    /// <summary>Адреса получателей отчёта о расхождениях (массив, минимум один).</summary>
    public List<string> To { get; set; } = [];

    /// <summary>Сколько последних закрытых дней сверять (текущий день не берётся).</summary>
    public int LookbackDays { get; set; } = 1;
}

namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Настройки сверки — подсекция "Options" события расписания с TaskKey = "DealsReconciliation"
/// (ScheduledEvents:Events[]:Options в appsettings.json).
/// </summary>
public sealed class DealsReconciliationOptions
{
    /// <summary>Адрес для отчёта о расхождениях.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Сколько последних закрытых дней сверять (текущий день не берётся).</summary>
    public int LookbackDays { get; set; } = 1;
}

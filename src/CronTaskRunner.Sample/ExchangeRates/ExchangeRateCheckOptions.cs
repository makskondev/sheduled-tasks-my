namespace CronTaskRunner.Sample.ExchangeRates;

/// <summary>
/// Настройки задачи — подсекция "Options" события расписания с TaskKey = "ExchangeRateCheck"
/// (ScheduledEvents:Events[]:Options в appsettings.json).
/// </summary>
public sealed class ExchangeRateCheckOptions
{
    /// <summary>Адреса получателей уведомлений (массив, минимум один).</summary>
    public List<string> To { get; set; } = [];

    /// <summary>Порог изменения курса в процентах, начиная с которого шлём письмо.</summary>
    public decimal ThresholdPercent { get; set; } = 1.5m;
}

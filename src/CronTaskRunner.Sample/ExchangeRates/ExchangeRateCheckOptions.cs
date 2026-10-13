namespace CronTaskRunner.Sample.ExchangeRates;

/// <summary>
/// Настройки задачи — подсекция "Options" события расписания с TaskKey = "ExchangeRateCheck"
/// (ScheduledEvents:Events[]:Options в appsettings.json).
/// </summary>
public sealed class ExchangeRateCheckOptions
{
    /// <summary>Адрес для уведомлений.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Порог изменения курса в процентах, начиная с которого шлём письмо.</summary>
    public decimal ThresholdPercent { get; set; } = 1.5m;
}

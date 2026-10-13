using System.Globalization;
using CronTaskRunner.Core.Abstractions;
using CronTaskRunner.Sample.Email;
using Microsoft.Extensions.Logging;

namespace CronTaskRunner.Sample.ExchangeRates;

/// <summary>
/// Пример задачи: сравнивает текущий курс валюты с ранее сохранённым
/// и, если разница превышает порог, отправляет уведомление по почте.
/// </summary>
public sealed class ExchangeRateCheckTask(
    IExchangeRateProvider rateProvider,
    IEmailSender emailSender,
    IEventOptions<ExchangeRateCheckOptions> options,
    ILogger<ExchangeRateCheckTask> logger)
    : ScheduledTaskBase<ExchangeRateData, ExchangeRateComparisonResult>
{
    public override string Name => "ExchangeRateCheck";

    protected override async Task<ExchangeRateData> GetDataAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Task}] Получаем текущий и предыдущий курс", Name);

        var current = await rateProvider.GetCurrentRateAsync(cancellationToken);
        var previous = await rateProvider.GetLastSavedRateAsync(cancellationToken);

        return new ExchangeRateData(current, previous);
    }

    protected override Task<ExchangeRateComparisonResult> HandleAsync(
        ExchangeRateData data, CancellationToken cancellationToken)
    {
        var diff = Math.Abs(data.CurrentRate - data.PreviousRate);
        var diffPercent = data.PreviousRate == 0 ? 0 : diff / data.PreviousRate * 100;

        var result = new ExchangeRateComparisonResult(
            data.CurrentRate,
            data.PreviousRate,
            diff,
            diffPercent,
            ThresholdExceeded: diffPercent >= options.Value.ThresholdPercent);

        logger.LogInformation(
            "[{Task}] Разница {Diff} ({Percent:F2}%), порог превышен: {Exceeded}",
            Name, diff, diffPercent, result.ThresholdExceeded);

        return Task.FromResult(result);
    }

    protected override async Task ProcessResultAsync(
        ExchangeRateComparisonResult result, CancellationToken cancellationToken)
    {
        if (!result.ThresholdExceeded)
        {
            logger.LogInformation("[{Task}] Порог не превышен, письмо не отправляется", Name);
            return;
        }

        await emailSender.SendAsync(
            new EmailMessage(
                To: options.Value.To,
                Subject: string.Create(
                    CultureInfo.InvariantCulture, $"Курс изменился на {result.Diff:F2} ({result.DiffPercent:F2}%)"),
                Body: $"Было: {result.PreviousRate}, стало: {result.CurrentRate}"),
            cancellationToken);
    }
}

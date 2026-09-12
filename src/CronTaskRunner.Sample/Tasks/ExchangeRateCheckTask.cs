using CronTaskRunner.Core.Abstractions;
using CronTaskRunner.Sample.Services;
using Microsoft.Extensions.Logging;

namespace CronTaskRunner.Sample.Tasks;

/// <summary>
/// Пример задачи: сравнивает текущий курс валюты с ранее сохранённым
/// и, если разница превышает порог, отправляет уведомление по почте.
/// </summary>
public class ExchangeRateCheckTask : ScheduledTaskBase<ExchangeRateData, ExchangeRateComparisonResult>
{
    private const decimal ThresholdPercent = 1.5m;

    private readonly IExchangeRateProvider _rateProvider;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ExchangeRateCheckTask> _logger;

    public ExchangeRateCheckTask(
        IExchangeRateProvider rateProvider,
        IEmailSender emailSender,
        ILogger<ExchangeRateCheckTask> logger)
    {
        _rateProvider = rateProvider;
        _emailSender = emailSender;
        _logger = logger;
    }

    public override string Name => "ExchangeRateCheck";

    protected override async Task<ExchangeRateData> GetDataAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Task}] Получаем текущий и предыдущий курс", Name);

        var current = await _rateProvider.GetCurrentRateAsync(cancellationToken);
        var previous = await _rateProvider.GetLastSavedRateAsync(cancellationToken);

        return new ExchangeRateData
        {
            CurrentRate = current,
            PreviousRate = previous
        };
    }

    protected override Task<ExchangeRateComparisonResult> HandleAsync(
        ExchangeRateData data, CancellationToken cancellationToken)
    {
        var diff = Math.Abs(data.CurrentRate - data.PreviousRate);
        var diffPercent = data.PreviousRate == 0 ? 0 : diff / data.PreviousRate * 100;

        var result = new ExchangeRateComparisonResult
        {
            CurrentRate = data.CurrentRate,
            PreviousRate = data.PreviousRate,
            Diff = diff,
            ThresholdExceeded = diffPercent >= ThresholdPercent
        };

        _logger.LogInformation(
            "[{Task}] Разница {Diff} ({Percent}%), порог превышен: {Exceeded}",
            Name, diff, diffPercent, result.ThresholdExceeded);

        return Task.FromResult(result);
    }

    protected override async Task ProcessResultAsync(
        ExchangeRateComparisonResult result, CancellationToken cancellationToken)
    {
        if (!result.ThresholdExceeded)
        {
            _logger.LogInformation("[{Task}] Порог не превышен, письмо не отправляется", Name);
            return;
        }

        await _emailSender.SendAsync(
            to: "alerts@company.com",
            subject: $"Курс изменился на {result.Diff:F2}",
            body: $"Было: {result.PreviousRate}, стало: {result.CurrentRate}",
            cancellationToken);
    }
}

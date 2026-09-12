using CronTaskRunner.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Точка входа: читает список событий из конфигурации и для каждого
/// запускает свой независимый IEventRunner (все — конкурентно, через
/// Task.WhenAll). Ошибка в одном раннере не роняет остальные —
/// она логируется, и падает только этот раннер.
/// </summary>
public sealed class ScheduledEventsDispatcher : BackgroundService
{
    private readonly IOptionsMonitor<ScheduledEventsOptions> _optionsMonitor;
    private readonly IEventRunner _eventRunner;
    private readonly ILogger<ScheduledEventsDispatcher> _logger;

    public ScheduledEventsDispatcher(
        IOptionsMonitor<ScheduledEventsOptions> optionsMonitor,
        IEventRunner eventRunner,
        ILogger<ScheduledEventsDispatcher> logger)
    {
        _optionsMonitor = optionsMonitor;
        _eventRunner = eventRunner;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Набор событий (их имена) фиксируется при старте: добавление/удаление
        // событий требует рестарта, но Enabled и CronExpressions существующего
        // события каждый IEventRunner перечитывает из конфигурации сам.
        var eventNames = _optionsMonitor.CurrentValue.Events.Select(e => e.Name).ToList();

        if (eventNames.Count == 0)
        {
            _logger.LogWarning("В конфигурации нет ни одного события расписания (секция ScheduledEvents:Events)");
        }

        var runners = eventNames.Select(name => RunEventSafelyAsync(name, stoppingToken));

        return Task.WhenAll(runners);
    }

    private async Task RunEventSafelyAsync(string eventName, CancellationToken stoppingToken)
    {
        try
        {
            await _eventRunner.RunAsync(eventName, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // штатная остановка хоста
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[{Event}] Раннер события аварийно завершился", eventName);
        }
    }
}

using CronTaskRunner.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Точка входа: читает список событий из конфигурации и для каждого
/// запускает свой IEventRunner в отдельном потоке. Ошибка в одном
/// раннере не роняет остальные — она логируется и раннер завершается.
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
        var events = _optionsMonitor.CurrentValue.Events;

        if (events.Count == 0)
        {
            _logger.LogWarning("В конфигурации нет ни одного события расписания (секция ScheduledEvents:Events)");
        }

        var runners = events.Select(e => RunEventSafelyAsync(e, stoppingToken));

        return Task.WhenAll(runners);
    }

    private Task RunEventSafelyAsync(ScheduledEventDefinition definition, CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                await _eventRunner.RunAsync(definition, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // штатная остановка хоста
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "[{Event}] Раннер события аварийно завершился", definition.Name);
            }
        }, stoppingToken);
    }
}

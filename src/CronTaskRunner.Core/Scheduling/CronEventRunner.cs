using Cronos;
using CronTaskRunner.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Реализация IEventRunner на базе Cronos.
/// На каждой итерации заново читает определение события из
/// IOptionsMonitor — так изменения Enabled/CronExpressions в
/// appsettings.json подхватываются без рестарта хоста.
/// Для актуального набора cron-выражений вычисляет ближайшее время
/// срабатывания, ждёт, запускает задачу (не блокируя ожидание
/// следующего срабатывания) и так по кругу до остановки хоста.
/// Параллельные запуски одной и той же задачи не допускаются: если
/// предыдущий запуск ещё выполняется, следующее срабатывание пропускается.
/// </summary>
public sealed class CronEventRunner : IEventRunner
{
    private readonly IScheduledTaskFactory _taskFactory;
    private readonly IOptionsMonitor<ScheduledEventsOptions> _optionsMonitor;
    private readonly ILogger<CronEventRunner> _logger;

    public CronEventRunner(
        IScheduledTaskFactory taskFactory,
        IOptionsMonitor<ScheduledEventsOptions> optionsMonitor,
        ILogger<CronEventRunner> logger)
    {
        _taskFactory = taskFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task RunAsync(string eventName, CancellationToken cancellationToken)
    {
        var isTaskRunning = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var definition = FindDefinition(eventName);
            if (definition is null)
            {
                _logger.LogWarning("[{Event}] Событие удалено из конфигурации, раннер остановлен", eventName);
                return;
            }

            if (!definition.Enabled)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                continue;
            }

            var crons = ParseExpressions(eventName, definition.CronExpressions);
            if (crons.Count == 0)
            {
                _logger.LogWarning("[{Event}] Нет валидных cron-выражений, ждём и проверяем конфигурацию снова", eventName);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                continue;
            }

            var nowUtc = DateTime.UtcNow;

            // Берём ближайшее из срабатываний по ВСЕМ cron-выражениям события
            var next = crons
                .Select(c => c.GetNextOccurrence(nowUtc, TimeZoneInfo.Local))
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .OrderBy(d => d)
                .Cast<DateTime?>()
                .FirstOrDefault();

            if (next is null)
            {
                _logger.LogWarning("[{Event}] Не удалось вычислить следующее срабатывание", eventName);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                continue;
            }

            var delay = next.Value - nowUtc;
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            _logger.LogInformation(
                "[{Event}] Следующий запуск в {Next} (через {Delay})",
                eventName, next.Value.ToLocalTime(), delay);

            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break; // штатная остановка хоста
            }

            // Перечитываем определение — за время ожидания его могли
            // выключить или изменить конфигурацию.
            definition = FindDefinition(eventName);
            if (definition is null)
            {
                _logger.LogWarning("[{Event}] Событие удалено из конфигурации, раннер остановлен", eventName);
                return;
            }

            if (!definition.Enabled)
            {
                _logger.LogInformation("[{Event}] Пропуск запуска: выключено во время ожидания", eventName);
                continue;
            }

            if (Interlocked.CompareExchange(ref isTaskRunning, 1, 0) != 0)
            {
                _logger.LogWarning(
                    "[{Event}] Предыдущий запуск задачи {Task} ещё выполняется, срабатывание пропущено",
                    eventName, definition.TaskKey);
                continue;
            }

            // === СТАРТ ЗАДАЧИ В ОТДЕЛЬНОМ ПОТОКЕ ===
            // Не await'им напрямую — иначе следующее срабатывание cron
            // этого же события будет ждать завершения текущей задачи.
            var taskKey = definition.TaskKey;
            _ = Task.Run(() => ExecuteTaskAsync(eventName, taskKey, cancellationToken), cancellationToken);

            async Task ExecuteTaskAsync(string capturedEventName, string capturedTaskKey, CancellationToken ct)
            {
                try
                {
                    await using var handle = _taskFactory.Create(capturedTaskKey);

                    _logger.LogInformation("[{Event}] Старт задачи {Task}", capturedEventName, handle.Task.Name);

                    await handle.Task.RunAsync(ct);

                    _logger.LogInformation("[{Event}] Задача {Task} завершена", capturedEventName, handle.Task.Name);
                }
                catch (Exception ex)
                {
                    // Без try/catch внутри Task.Run исключение "потеряется"
                    // (unobserved task exception) и его никто не увидит.
                    _logger.LogError(ex, "[{Event}] Ошибка выполнения задачи", capturedEventName);
                }
                finally
                {
                    Volatile.Write(ref isTaskRunning, 0);
                }
            }
        }
    }

    private ScheduledEventDefinition? FindDefinition(string eventName)
    {
        return _optionsMonitor.CurrentValue.Events
            .FirstOrDefault(e => string.Equals(e.Name, eventName, StringComparison.OrdinalIgnoreCase));
    }

    private List<CronExpression> ParseExpressions(string eventName, List<string> expressions)
    {
        var result = new List<CronExpression>();

        foreach (var expr in expressions)
        {
            try
            {
                result.Add(CronExpression.Parse(expr));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Event}] Некорректное cron-выражение: {Expr}", eventName, expr);
            }
        }

        return result;
    }
}

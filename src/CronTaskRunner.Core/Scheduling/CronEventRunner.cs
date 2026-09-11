using Cronos;
using CronTaskRunner.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Реализация IEventRunner на базе Cronos.
/// Для каждого события: вычисляет ближайшее время срабатывания
/// среди ВСЕХ его cron-выражений, ждёт, запускает задачу в отдельном
/// потоке (чтобы не блокировать ожидание следующего срабатывания),
/// и так по кругу до остановки хоста.
/// </summary>
public sealed class CronEventRunner : IEventRunner
{
    private readonly IScheduledTaskFactory _taskFactory;
    private readonly ILogger<CronEventRunner> _logger;

    public CronEventRunner(IScheduledTaskFactory taskFactory, ILogger<CronEventRunner> logger)
    {
        _taskFactory = taskFactory;
        _logger = logger;
    }

    public async Task RunAsync(ScheduledEventDefinition definition, CancellationToken cancellationToken)
    {
        var crons = ParseExpressions(definition.Name, definition.CronExpressions);
        if (crons.Count == 0)
        {
            _logger.LogWarning("[{Event}] Нет валидных cron-выражений, раннер остановлен", definition.Name);
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!definition.Enabled)
            {
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
                _logger.LogWarning("[{Event}] Не удалось вычислить следующее срабатывание", definition.Name);
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
                definition.Name, next.Value.ToLocalTime(), delay);

            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break; // штатная остановка хоста
            }

            if (!definition.Enabled)
            {
                _logger.LogInformation("[{Event}] Пропуск запуска: выключено во время ожидания", definition.Name);
                continue;
            }

            // === СТАРТ ЗАДАЧИ В ОТДЕЛЬНОМ ПОТОКЕ ===
            // Не await'им напрямую — иначе следующее срабатывание cron
            // этого же события будет ждать завершения текущей задачи.
            _ = Task.Run(() => ExecuteTaskAsync(definition, cancellationToken), cancellationToken);
        }
    }

    private async Task ExecuteTaskAsync(ScheduledEventDefinition definition, CancellationToken cancellationToken)
    {
        try
        {
            await using var handle = _taskFactory.Create(definition.TaskKey);

            _logger.LogInformation("[{Event}] Старт задачи {Task}", definition.Name, handle.Task.Name);

            await handle.Task.RunAsync(cancellationToken);

            _logger.LogInformation("[{Event}] Задача {Task} завершена", definition.Name, handle.Task.Name);
        }
        catch (Exception ex)
        {
            // Без try/catch внутри Task.Run исключение "потеряется"
            // (unobserved task exception) и его никто не увидит.
            _logger.LogError(ex, "[{Event}] Ошибка выполнения задачи", definition.Name);
        }
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

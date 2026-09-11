using CronTaskRunner.Core.Configuration;

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Следит за расписанием одного события и запускает
/// соответствующую задачу при каждом срабатывании cron.
/// </summary>
public interface IEventRunner
{
    Task RunAsync(ScheduledEventDefinition definition, CancellationToken cancellationToken);
}

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Создаёт экземпляр задачи по её ключу вместе со scope DI,
/// в рамках которого задача должна выполняться целиком.
/// </summary>
public interface IScheduledTaskFactory
{
    ScheduledTaskHandle Create(string taskKey);
}

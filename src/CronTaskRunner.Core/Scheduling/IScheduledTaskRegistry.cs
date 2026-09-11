namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Реестр соответствия "ключ задачи" -> "CLR-тип", заполняется
/// автоматически из всех вызовов AddScheduledTask&lt;T&gt;(key).
/// </summary>
public interface IScheduledTaskRegistry
{
    Type Resolve(string taskKey);
}

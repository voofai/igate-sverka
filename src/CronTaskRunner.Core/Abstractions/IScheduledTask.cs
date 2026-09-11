namespace CronTaskRunner.Core.Abstractions;

/// <summary>
/// Базовый не-дженерик контракт задачи. Позволяет планировщику
/// хранить и запускать разнотипные задачи, не зная их TData/TResult.
/// </summary>
public interface IScheduledTask
{
    /// <summary>
    /// Человекочитаемое имя задачи (для логов).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Точка входа для планировщика.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);
}

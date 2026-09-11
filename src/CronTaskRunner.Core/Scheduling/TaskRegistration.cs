namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Связка "ключ задачи из конфига" -> "CLR-тип задачи".
/// Регистрируется в DI через AddScheduledTask&lt;T&gt;(key).
/// </summary>
public sealed record TaskRegistration(string Key, Type TaskType);

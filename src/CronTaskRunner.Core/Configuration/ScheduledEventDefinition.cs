namespace CronTaskRunner.Core.Configuration;

/// <summary>
/// Описание одного события расписания из конфигурации:
/// когда запускать (набор cron-выражений) и какую задачу.
/// </summary>
public class ScheduledEventDefinition
{
    /// <summary>Имя события (для логов).</summary>
    public string Name { get; set; } = default!;

    /// <summary>Включено ли событие. Можно выключить без пересборки.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Ключ задачи — соответствует ключу, под которым задача
    /// зарегистрирована через AddScheduledTask&lt;T&gt;(key).
    /// </summary>
    public string TaskKey { get; set; } = default!;

    /// <summary>
    /// Расписание в виде набора cron-выражений (5-полевой формат:
    /// минута час день-месяца месяц день-недели).
    /// Задача запускается по срабатыванию любого из них.
    /// </summary>
    public List<string> CronExpressions { get; set; } = new();
}

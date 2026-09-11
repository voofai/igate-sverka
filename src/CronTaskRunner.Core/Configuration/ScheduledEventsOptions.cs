namespace CronTaskRunner.Core.Configuration;

/// <summary>
/// Корень секции конфигурации "ScheduledEvents".
/// </summary>
public class ScheduledEventsOptions
{
    public List<ScheduledEventDefinition> Events { get; set; } = new();
}

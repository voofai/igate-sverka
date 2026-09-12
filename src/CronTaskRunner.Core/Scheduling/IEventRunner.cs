namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Следит за расписанием одного события (по имени) и запускает
/// соответствующую задачу при каждом срабатывании cron.
/// Актуальную конфигурацию события (Enabled, CronExpressions) раннер
/// перечитывает на каждой итерации — это позволяет включать/выключать
/// событие и менять его расписание через appsettings.json без рестарта.
/// </summary>
public interface IEventRunner
{
    Task RunAsync(string eventName, CancellationToken cancellationToken);
}

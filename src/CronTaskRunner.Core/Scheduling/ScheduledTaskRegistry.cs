namespace CronTaskRunner.Core.Scheduling;

public sealed class ScheduledTaskRegistry : IScheduledTaskRegistry
{
    private readonly Dictionary<string, Type> _map;

    public ScheduledTaskRegistry(IEnumerable<TaskRegistration> registrations)
    {
        _map = registrations.ToDictionary(
            r => r.Key,
            r => r.TaskType,
            StringComparer.OrdinalIgnoreCase);
    }

    public Type Resolve(string taskKey)
    {
        if (!_map.TryGetValue(taskKey, out var type))
        {
            throw new InvalidOperationException(
                $"Задача с ключом '{taskKey}' не зарегистрирована. " +
                $"Используйте services.AddScheduledTask<T>(\"{taskKey}\") при старте приложения.");
        }

        return type;
    }
}

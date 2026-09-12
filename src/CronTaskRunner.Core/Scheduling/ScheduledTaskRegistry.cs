namespace CronTaskRunner.Core.Scheduling;

public sealed class ScheduledTaskRegistry : IScheduledTaskRegistry
{
    private readonly Dictionary<string, Type> _map;

    public ScheduledTaskRegistry(IEnumerable<TaskRegistration> registrations)
    {
        _map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (var registration in registrations)
        {
            if (!_map.TryAdd(registration.Key, registration.TaskType))
            {
                throw new InvalidOperationException(
                    $"Задача с ключом '{registration.Key}' уже зарегистрирована " +
                    $"(тип {_map[registration.Key].Name}). Ключи задач в AddScheduledTask<T>(key) должны быть уникальны.");
            }
        }
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

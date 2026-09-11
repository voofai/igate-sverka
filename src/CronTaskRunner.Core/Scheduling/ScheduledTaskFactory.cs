using CronTaskRunner.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CronTaskRunner.Core.Scheduling;

public sealed class ScheduledTaskFactory : IScheduledTaskFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IScheduledTaskRegistry _registry;

    public ScheduledTaskFactory(IServiceScopeFactory scopeFactory, IScheduledTaskRegistry registry)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
    }

    public ScheduledTaskHandle Create(string taskKey)
    {
        var taskType = _registry.Resolve(taskKey);

        var scope = _scopeFactory.CreateScope();
        var task = (IScheduledTask)scope.ServiceProvider.GetRequiredService(taskType);

        return new ScheduledTaskHandle(task, scope);
    }
}

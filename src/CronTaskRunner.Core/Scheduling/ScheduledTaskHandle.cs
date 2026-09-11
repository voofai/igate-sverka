using CronTaskRunner.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CronTaskRunner.Core.Scheduling;

/// <summary>
/// Обёртка над задачей и её DI-scope. Scope живёт ровно столько,
/// сколько выполняется задача, и закрывается через DisposeAsync
/// ПОСЛЕ завершения RunAsync — это важно, если задача использует
/// scoped-зависимости (например, DbContext).
/// </summary>
public sealed class ScheduledTaskHandle : IAsyncDisposable
{
    private readonly IServiceScope _scope;

    public ScheduledTaskHandle(IScheduledTask task, IServiceScope scope)
    {
        Task = task;
        _scope = scope;
    }

    public IScheduledTask Task { get; }

    public ValueTask DisposeAsync()
    {
        _scope.Dispose();
        return ValueTask.CompletedTask;
    }
}

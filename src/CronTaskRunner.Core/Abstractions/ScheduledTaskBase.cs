namespace CronTaskRunner.Core.Abstractions;

/// <summary>
/// Шаблон задачи: получение данных -> обработка -> обработка результата.
/// </summary>
/// <typeparam name="TData">Тип данных, с которыми работает задача.</typeparam>
/// <typeparam name="TResult">Тип результата обработки.</typeparam>
public abstract class ScheduledTaskBase<TData, TResult> : IScheduledTask
{
    public abstract string Name { get; }

    /// <summary>
    /// Шаблонный метод — вызывается планировщиком.
    /// Наследники НЕ переопределяют этот метод, только шаги ниже.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var data = await GetDataAsync(cancellationToken);
        var result = await HandleAsync(data, cancellationToken);
        await ProcessResultAsync(result, cancellationToken);
    }

    /// <summary>
    /// Шаг 1. Получение исходных данных для работы задачи
    /// (БД, внешний API, файл и т.д.)
    /// </summary>
    protected abstract Task<TData> GetDataAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Шаг 2. Основная работа — сравнение/анализ полученных данных.
    /// </summary>
    protected abstract Task<TResult> HandleAsync(TData data, CancellationToken cancellationToken);

    /// <summary>
    /// Шаг 3. Обработка результата (в нашем случае — рассылка по почте).
    /// </summary>
    protected abstract Task ProcessResultAsync(TResult result, CancellationToken cancellationToken);
}

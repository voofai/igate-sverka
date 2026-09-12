namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Сделка в унифицированном виде — то, во что каждый ISource
/// приводит свои "родные" поля перед сравнением.
/// </summary>
public sealed class DealRecord
{
    public string DealId { get; init; } = default!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public DateTime TradeDate { get; init; }
}

/// <summary>
/// Источник сделок — одна БД/система. Каждая реализация сама
/// знает, как её "родные" поля (разные имена/типы/форматы)
/// превратить в единый DealRecord.
/// </summary>
public interface IDealsSource
{
    /// <summary>Имя источника — используется в отчёте (например, "ГИС", "Биллинг").</summary>
    string SourceName { get; }

    Task<IReadOnlyCollection<DealRecord>> GetDealsAsync(CancellationToken cancellationToken);
}

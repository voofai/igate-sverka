namespace CronTaskRunner.Sample.Deals;

public enum DiscrepancyKind
{
    /// <summary>Сделка есть не во всех источниках.</summary>
    MissingInSomeSources,

    /// <summary>Сделка есть везде, но поля (сумма/валюта/дата) расходятся.</summary>
    FieldMismatch,
}

public sealed class DealDiscrepancy
{
    public string DealId { get; init; } = default!;
    public DiscrepancyKind Kind { get; init; }

    /// <summary>Что видит каждый источник по этой сделке (SourceName -> запись, null если сделки там нет).</summary>
    public IReadOnlyDictionary<string, DealRecord?> BySource { get; init; } = new Dictionary<string, DealRecord?>();

    public string Description { get; init; } = default!;
}

public sealed class DealsReconciliationResult
{
    public int TotalDealsChecked { get; init; }
    public IReadOnlyList<DealDiscrepancy> Discrepancies { get; init; } = Array.Empty<DealDiscrepancy>();

    public bool HasDiscrepancies => Discrepancies.Count > 0;
}

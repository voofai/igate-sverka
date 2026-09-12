namespace CronTaskRunner.Sample.Deals;

/// <summary>
/// Заглушки для демонстрации — две "разные БД" с разным набором сделок.
/// Замените на реальных читателей (Dapper/EF/ADO.NET) поверх ваших БД —
/// каждый источник маппит свои "родные" поля в DealRecord самостоятельно.
/// </summary>
public sealed class SourceADealsSource : IDealsSource
{
    public string SourceName => "СистемаА";

    public Task<IReadOnlyCollection<DealRecord>> GetDealsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DealRecord> deals = new[]
        {
            new DealRecord { DealId = "D-001", Amount = 1000.00m, Currency = "RUB", TradeDate = new DateTime(2026, 9, 10) },
            new DealRecord { DealId = "D-002", Amount = 2500.50m, Currency = "USD", TradeDate = new DateTime(2026, 9, 11) },
            new DealRecord { DealId = "D-003", Amount = 750.00m, Currency = "RUB", TradeDate = new DateTime(2026, 9, 11) },
        };

        return Task.FromResult(deals);
    }
}

public sealed class SourceBDealsSource : IDealsSource
{
    public string SourceName => "СистемаБ";

    public Task<IReadOnlyCollection<DealRecord>> GetDealsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DealRecord> deals = new[]
        {
            new DealRecord { DealId = "D-001", Amount = 1000.00m, Currency = "RUB", TradeDate = new DateTime(2026, 9, 10) },
            new DealRecord { DealId = "D-002", Amount = 2400.00m, Currency = "USD", TradeDate = new DateTime(2026, 9, 11) }, // сумма расходится
            new DealRecord { DealId = "D-004", Amount = 300.00m, Currency = "RUB", TradeDate = new DateTime(2026, 9, 12) }, // нет в СистемаА
        };

        return Task.FromResult(deals);
    }
}

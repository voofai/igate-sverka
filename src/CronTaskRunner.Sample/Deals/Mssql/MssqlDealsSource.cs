using CronTaskRunner.Sample.Deals;
using Microsoft.EntityFrameworkCore;

namespace CronTaskRunner.Sample.Deals.Mssql;

/// <summary>
/// Источник сделок поверх MSSQL. Знает про "родную" структуру этой
/// базы (dbo.Deals, char(3) для валюты и т.д.) и приводит её к
/// единому DealRecord — остальной код сверки про MSSQL ничего не знает.
/// </summary>
public sealed class MssqlDealsSource : IDealsSource
{
    private readonly MssqlDealsDbContext _db;

    public MssqlDealsSource(MssqlDealsDbContext db)
    {
        _db = db;
    }

    public string SourceName => "MSSQL";

    public async Task<IReadOnlyCollection<DealRecord>> GetDealsAsync(CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Date.AddDays(-1);

        var entities = await _db.Deals
            .AsNoTracking()
            .Where(d => d.TradeDate >= from)
            .ToListAsync(cancellationToken);

        return entities
            .Select(e => new DealRecord
            {
                DealId = e.DealNumber.Trim(),
                Amount = e.Amount,
                Currency = e.CurrencyCode.Trim().ToUpperInvariant(),
                TradeDate = e.TradeDate,
            })
            .ToList();
    }
}

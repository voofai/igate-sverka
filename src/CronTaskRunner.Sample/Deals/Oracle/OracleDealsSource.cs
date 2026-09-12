using CronTaskRunner.Sample.Deals;
using Microsoft.EntityFrameworkCore;

namespace CronTaskRunner.Sample.Deals.Oracle;

/// <summary>
/// Источник сделок поверх Oracle. Структура полностью отличается от
/// MSSQL (другая таблица, другие имена и типы колонок) — вся эта
/// специфика инкапсулирована здесь и наружу не протекает.
/// </summary>
public sealed class OracleDealsSource : IDealsSource
{
    private readonly OracleDealsDbContext _db;

    public OracleDealsSource(OracleDealsDbContext db)
    {
        _db = db;
    }

    public string SourceName => "Oracle";

    public async Task<IReadOnlyCollection<DealRecord>> GetDealsAsync(CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Date.AddDays(-1);

        var entities = await _db.Deals
            .AsNoTracking()
            .Where(d => d.OperDate >= from)
            .ToListAsync(cancellationToken);

        return entities
            .Select(e => new DealRecord
            {
                DealId = e.DealId.Trim(),
                Amount = e.DealSum,
                Currency = e.CurCode.Trim().ToUpperInvariant(),
                TradeDate = e.OperDate.Date,
            })
            .ToList();
    }
}

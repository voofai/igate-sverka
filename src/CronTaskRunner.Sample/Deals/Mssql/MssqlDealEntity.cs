namespace CronTaskRunner.Sample.Deals.Mssql;

/// <summary>
/// "Родная" сущность MSSQL-базы — таблица dbo.Deals, как она реально
/// устроена в этой системе (свои имена колонок, свой суррогатный ключ).
/// </summary>
public sealed class MssqlDealEntity
{
    /// <summary>Технический PK таблицы, для сверки не используется.</summary>
    public int Id { get; set; }

    /// <summary>Бизнес-номер сделки — по нему сверяем с другими системами.</summary>
    public string DealNumber { get; set; } = default!;

    public decimal Amount { get; set; }

    /// <summary>Хранится как char(3), поэтому может приходить с паддингом пробелами.</summary>
    public string CurrencyCode { get; set; } = default!;

    public DateTime TradeDate { get; set; }
}

namespace CronTaskRunner.Sample.Deals.Oracle;

/// <summary>
/// "Родная" сущность Oracle-базы — таблица SVOD.DEAL_REG. Имена
/// таблицы/колонок, типы и даже сам бизнес-смысл полей здесь другие,
/// чем в MSSQL: это другая система с независимо спроектированной схемой.
/// </summary>
public sealed class OracleDealEntity
{
    /// <summary>Бизнес-номер сделки — аналог DealNumber в MSSQL, но своё имя колонки.</summary>
    public string DealId { get; set; } = default!;

    /// <summary>NUMBER(18,2) в Oracle маппится в decimal.</summary>
    public decimal DealSum { get; set; }

    /// <summary>VARCHAR2(3) — код валюты, но в этой системе он не паддится.</summary>
    public string CurCode { get; set; } = default!;

    /// <summary>Oracle DATE хранит и дату, и время — на выходе всегда с временем 00:00:00,
    /// но полагаться на это нельзя, поэтому при сравнении берём только Date.</summary>
    public DateTime OperDate { get; set; }
}

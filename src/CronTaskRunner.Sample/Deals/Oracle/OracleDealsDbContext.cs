using Microsoft.EntityFrameworkCore;

namespace CronTaskRunner.Sample.Deals.Oracle;

/// <summary>
/// DbContext поверх Oracle-базы источника сделок (Oracle.EntityFrameworkCore).
/// Как и MssqlDealsDbContext, регистрируется как Scoped — свой экземпляр
/// на каждый запуск задачи.
/// </summary>
public sealed class OracleDealsDbContext : DbContext
{
    public OracleDealsDbContext(DbContextOptions<OracleDealsDbContext> options) : base(options)
    {
    }

    public DbSet<OracleDealEntity> Deals => Set<OracleDealEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OracleDealEntity>(entity =>
        {
            // В Oracle неэкранированные идентификаторы хранятся в верхнем
            // регистре — указываем имена явно, а не полагаемся на конвенции EF.
            entity.ToTable("DEAL_REG", schema: "SVOD");
            entity.HasNoKey(); // читаем только на чтение, PK для сверки не нужен

            entity.Property(e => e.DealId).HasColumnName("DEAL_ID").HasMaxLength(50);
            entity.Property(e => e.DealSum).HasColumnName("DEAL_SUM").HasColumnType("NUMBER(18,2)");
            entity.Property(e => e.CurCode).HasColumnName("CUR_CODE").HasMaxLength(3);
            entity.Property(e => e.OperDate).HasColumnName("OPER_DATE").HasColumnType("DATE");
        });
    }
}

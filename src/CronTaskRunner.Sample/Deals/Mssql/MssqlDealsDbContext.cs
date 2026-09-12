using Microsoft.EntityFrameworkCore;

namespace CronTaskRunner.Sample.Deals.Mssql;

/// <summary>
/// DbContext поверх MSSQL-базы источника сделок. Регистрируется как
/// Scoped (по умолчанию у AddDbContext) — новый экземпляр на каждый
/// запуск задачи сверки, т.к. ScheduledTaskFactory создаёт свой DI-scope
/// на каждое срабатывание cron (см. ScheduledTaskHandle в Core).
/// </summary>
public sealed class MssqlDealsDbContext : DbContext
{
    public MssqlDealsDbContext(DbContextOptions<MssqlDealsDbContext> options) : base(options)
    {
    }

    public DbSet<MssqlDealEntity> Deals => Set<MssqlDealEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MssqlDealEntity>(entity =>
        {
            entity.ToTable("Deals", schema: "dbo");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.DealNumber).HasColumnName("DealNumber").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.CurrencyCode).HasColumnName("CurrencyCode").HasColumnType("char(3)").IsRequired();
            entity.Property(e => e.TradeDate).HasColumnName("TradeDate").HasColumnType("datetime2");
        });
    }
}

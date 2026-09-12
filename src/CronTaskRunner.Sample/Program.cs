using CronTaskRunner.Core.Extensions;
using CronTaskRunner.Sample.Deals;
using CronTaskRunner.Sample.Deals.Mssql;
using CronTaskRunner.Sample.Deals.Oracle;
using CronTaskRunner.Sample.Services;
using CronTaskRunner.Sample.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Регистрируем инфраструктуру планировщика (секция "ScheduledEvents" из appsettings.json)
builder.Services.AddCronTaskRunner(builder.Configuration);

// Регистрируем конкретные задачи под ключами, на которые ссылается конфиг (TaskKey)
builder.Services.AddScheduledTask<ExchangeRateCheckTask>("ExchangeRateCheck");
builder.Services.AddScheduledTask<DealsReconciliationTask>("DealsReconciliation");

// Зависимости ExchangeRateCheckTask (заглушки для демонстрации,
// замените на реальные реализации)
builder.Services.AddScoped<IExchangeRateProvider, FakeExchangeRateProvider>();
builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();

// Зависимости DealsReconciliationTask — пример сверки сделок из двух
// РЕАЛЬНЫХ БД разной природы через EF Core: MSSQL (dbo.Deals) и Oracle
// (SVOD.DEAL_REG), с полностью разными схемами. Каждый *DealsSource сам
// маппит "родную" сущность своей БД в единый DealRecord.
builder.Services.AddDbContext<MssqlDealsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MssqlDeals")));

builder.Services.AddDbContext<OracleDealsDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDeals")));

builder.Services.AddScoped<IDealsSource, MssqlDealsSource>();
builder.Services.AddScoped<IDealsSource, OracleDealsSource>();

// Для локальной проверки без реальных БД можно временно заменить два
// регистрации выше на заглушки:
// builder.Services.AddScoped<IDealsSource, SourceADealsSource>();
// builder.Services.AddScoped<IDealsSource, SourceBDealsSource>();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
// builder.Services.AddScoped<IEmailSender, SmtpEmailSender>(); // включить для реальной отправки

var host = builder.Build();
await host.RunAsync();

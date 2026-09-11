using CronTaskRunner.Core.Extensions;
using CronTaskRunner.Sample.Services;
using CronTaskRunner.Sample.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Регистрируем инфраструктуру планировщика (секция "ScheduledEvents" из appsettings.json)
builder.Services.AddCronTaskRunner(builder.Configuration);

// Регистрируем конкретные задачи под ключами, на которые ссылается конфиг (TaskKey)
builder.Services.AddScheduledTask<ExchangeRateCheckTask>("ExchangeRateCheck");

// Зависимости самой задачи (здесь — заглушки для демонстрации,
// замените на реальные реализации)
builder.Services.AddScoped<IExchangeRateProvider, FakeExchangeRateProvider>();
builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();

var host = builder.Build();
await host.RunAsync();

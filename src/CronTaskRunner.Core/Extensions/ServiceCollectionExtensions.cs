using CronTaskRunner.Core.Abstractions;
using CronTaskRunner.Core.Configuration;
using CronTaskRunner.Core.Scheduling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CronTaskRunner.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует инфраструктуру планировщика: конфиг, реестр задач,
    /// фабрику задач, раннер cron-расписания и фоновый диспетчер.
    /// Вызывается один раз при старте приложения.
    /// </summary>
    public static IServiceCollection AddCronTaskRunner(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "ScheduledEvents")
    {
        services.Configure<ScheduledEventsOptions>(configuration.GetSection(sectionName));

        services.AddSingleton<IScheduledTaskRegistry, ScheduledTaskRegistry>();
        services.AddSingleton<IScheduledTaskFactory, ScheduledTaskFactory>();
        services.AddSingleton<IEventRunner, CronEventRunner>();
        services.AddHostedService<ScheduledEventsDispatcher>();

        return services;
    }

    /// <summary>
    /// Регистрирует конкретную задачу под ключом, на который
    /// ссылается TaskKey в конфигурации события (appsettings.json).
    /// </summary>
    public static IServiceCollection AddScheduledTask<TTask>(this IServiceCollection services, string key)
        where TTask : class, IScheduledTask
    {
        services.AddTransient<TTask>();
        services.AddSingleton(new TaskRegistration(key, typeof(TTask)));

        return services;
    }
}

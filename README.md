# CronTaskRunner

Универсальный запускатор задач по расписанию для .NET (net8.0).
Расписание каждого события описывается набором cron-выражений
(формат Cronos: `минута час день-месяца месяц день-недели`).

## Структура

```
CronTaskRunner.sln
src/
  CronTaskRunner.Core/            — переиспользуемая библиотека-планировщик
    Abstractions/
      IScheduledTask.cs           — базовый контракт задачи
      ScheduledTaskBase.cs        — абстрактный класс с 3 шагами:
                                     GetDataAsync -> Handle -> ProcessResultAsync
    Configuration/
      ScheduledEventDefinition.cs — Name, Enabled, TaskKey, CronExpressions
      ScheduledEventsOptions.cs   — корень секции конфига "ScheduledEvents"
    Scheduling/
      IEventRunner.cs / CronEventRunner.cs
                                   — следит за расписанием ОДНОГО события,
                                     стартует задачу в отдельном потоке
      IScheduledTaskFactory.cs / ScheduledTaskFactory.cs
                                   — создаёт задачу + DI-scope под неё
      ScheduledTaskHandle.cs      — задача + scope, scope закрывается
                                     после завершения задачи
      IScheduledTaskRegistry.cs / ScheduledTaskRegistry.cs
                                   — ключ задачи (строка) -> CLR-тип
      TaskRegistration.cs
      ScheduledEventsDispatcher.cs
                                   — BackgroundService: по потоку на
                                     каждое событие из конфига
    Extensions/
      ServiceCollectionExtensions.cs
                                   — AddCronTaskRunner(...), AddScheduledTask<T>(key)

  CronTaskRunner.Sample/          — пример использования (консольный хост)
    Program.cs
    appsettings.json              — пример события с cron на 08:15, 09:34, 23:23
    Tasks/ExchangeRateCheckTask.cs
                                   — пример реализации ScheduledTaskBase
    Services/                     — заглушки IExchangeRateProvider / IEmailSender
```

## Как это работает

1. `ScheduledEventsDispatcher` (BackgroundService) читает `ScheduledEvents:Events`
   из конфига и на **каждое событие** стартует отдельный `CronEventRunner`
   в своём потоке (`Task.Run`).
2. `CronEventRunner` парсит `CronExpressions` события через Cronos, на каждой
   итерации цикла вычисляет **ближайшее** срабатывание среди всех выражений,
   ждёт (`Task.Delay`), а по срабатыванию **запускает задачу в отдельном
   потоке** — чтобы долгое выполнение задачи не мешало ждать следующее
   срабатывание того же события.
3. Задача резолвится через `IScheduledTaskFactory` по строковому `TaskKey`
   из конфига — фабрика создаёт DI-scope и разрешает тип задачи внутри него;
   scope закрывается только после того, как задача полностью отработала
   (важно для scoped-зависимостей вроде `DbContext`).
4. Сама задача — наследник `ScheduledTaskBase<TData, TResult>` — реализует
   три шага: `GetDataAsync` (получить данные), `Handle` (сравнение/анализ),
   `ProcessResultAsync` (в примере — отправка email при превышении порога).

## Как добавить новую задачу

```csharp
public class MyTask : ScheduledTaskBase<MyData, MyResult>
{
    public override string Name => "MyTask";

    protected override Task<MyData> GetDataAsync(CancellationToken ct) => ...;
    protected override Task<MyResult> Handle(MyData data, CancellationToken ct) => ...;
    protected override Task ProcessResultAsync(MyResult result, CancellationToken ct) => ...;
}
```

```csharp
// Program.cs
builder.Services.AddScheduledTask<MyTask>("MyTaskKey");
```

```json
// appsettings.json
{
  "ScheduledEvents": {
    "Events": [
      {
        "Name": "MyEvent",
        "Enabled": true,
        "TaskKey": "MyTaskKey",
        "CronExpressions": [ "0 12 * * *", "30 18 * * *" ]
      }
    ]
  }
}
```

## Запуск

```bash
cd src/CronTaskRunner.Sample
dotnet restore
dotnet run
```

## Что стоит доработать под продакшен

- **Misfire / персистентность**: если процесс не работал в момент
  срабатывания (упал/деплоился), запуск просто пропускается — это
  подходящее поведение для задач "по времени суток", но не для задач,
  которые обязательно должны выполниться хотя бы раз. Для этого нужна
  персистентность (например, Quartz.NET) или собственная запись
  last-run в БД с проверкой при старте.
- **Ретраи и circuit breaker** — сейчас ошибка задачи просто логируется
  (`LogError`/`LogCritical`), без повторных попыток.
- **Параллелизм одной задачи** — если задача не успела выполниться до
  следующего срабатывания того же cron, оба запуска стартуют параллельно.
  Если это нежелательно — добавьте `SemaphoreSlim(1,1)` на уровне
  `CronEventRunner` для конкретного события.
- **Часовой пояс** — сейчас используется `TimeZoneInfo.Local` (время
  сервера). Если приложение развёрнуто в нескольких регионах, лучше
  явно задавать таймзону в конфиге события.

# CronTaskRunner

Сервис, который запускает задачи по cron-расписанию из `appsettings.json`.
Основная задача — ночная сверка сделок между MSSQL и Oracle с отчётом о
расхождениях на почту.

- `src/CronTaskRunner.Core` — планировщик (библиотека, бизнес-логики нет).
- `src/CronTaskRunner.Sample` — сам сервис: задачи и их зависимости.

Стек: .NET 8, Generic Host, Cronos, EF Core (SqlServer, Oracle), xUnit.

## Быстрый старт

Локально без БД и почты, демо-данные в памяти:

```bash
cd src/CronTaskRunner.Sample
DealsReconciliation__UseInMemorySources=true dotnet run
```

Задачи сработают по расписанию из `appsettings.json`. Чтобы не ждать,
поставьте событию `"CronExpressions": [ "* * * * *" ]` — запуск каждую минуту.
Письма без `Smtp:Enabled=true` пишутся в лог.

Тесты:

```bash
dotnet test
```

## Как добавить задачу

**1. Класс задачи** — наследник `ScheduledTaskBase<TData, TResult>`, три шага:

```csharp
public sealed class MyTask(IMyRepository repository, IEmailSender email)
    : ScheduledTaskBase<MyData, MyResult>
{
    public override string Name => "MyTask";

    // 1. Получить данные (БД, API, файл)
    protected override Task<MyData> GetDataAsync(CancellationToken ct) => ...;

    // 2. Сравнить / посчитать
    protected override Task<MyResult> HandleAsync(MyData data, CancellationToken ct) => ...;

    // 3. Что сделать с результатом (письмо, запись в БД)
    protected override Task ProcessResultAsync(MyResult result, CancellationToken ct) => ...;
}
```

Каждый запуск получает свой DI-scope, поэтому scoped-зависимости
(`DbContext` и т.п.) можно внедрять напрямую.

Настройки задачи — обычный класс с публичным конструктором без параметров.
Внедряйте их как `IEventOptions<MyOptions>` — это подсекция `Options` события,
которое запустило задачу (см. шаг 3). Отдельную секцию конфига и
`services.Configure<T>(...)` заводить не нужно.

**2. Регистрация под ключом:**

```csharp
builder.Services.AddScheduledTask<MyTask>("MyTask");
```

Принято держать регистрацию фичи в её папке — см.
`Deals/DealsServiceCollectionExtensions.cs`.

**3. Событие в конфиге:**

```json
"ScheduledEvents": {
  "Events": [
    {
      "Name": "MyTask-Daily",
      "Enabled": true,
      "TaskKey": "MyTask",
      "CronExpressions": [ "0 12 * * *", "30 18 * * *" ],
      "Options": {
        "To": "alerts@company.com"
      }
    }
  ]
}
```

Если `TaskKey` не зарегистрирован или cron некорректен, сервис не стартует
и пишет, что именно не так.

Одну задачу можно запускать несколькими событиями с разными `Options`
(например, сверка за 1 день каждую ночь и за 7 дней по воскресеньям).
Нет `Options` — задача получит значения по умолчанию из своего класса настроек.

## Конфигурация

| Ключ | Что задаёт | Без рестарта |
|---|---|---|
| `ScheduledEvents:Events[]:Enabled` | включить/выключить событие | да, в течение минуты |
| `ScheduledEvents:Events[]:CronExpressions` | расписание, срабатывает по любому из выражений | да, в течение минуты |
| `ScheduledEvents:Events[]:Name`, `TaskKey` | имя события для логов, ключ задачи | нет |
| `ScheduledEvents:Events[]:Options:To` (событие с `TaskKey` = `DealsReconciliation`) | адрес отчёта о расхождениях | да, со следующего запуска |
| `ScheduledEvents:Events[]:Options:LookbackDays` (`DealsReconciliation`) | сколько закрытых дней сверять (по умолчанию 1) | да, со следующего запуска |
| `ScheduledEvents:Events[]:Options:To`, `ThresholdPercent` (`ExchangeRateCheck`) | адрес и порог изменения курса, % | да, со следующего запуска |
| `DealsReconciliation:UseInMemorySources` | демо-данные вместо MSSQL/Oracle (глобальный переключатель регистрации) | нет |
| `Smtp:Enabled` | `true` — слать через SMTP, `false` — писать в лог | нет |
| `Smtp:Host`, `Port`, `EnableSsl`, `User`, `Password`, `From` | параметры SMTP; пустой `User` — без аутентификации | да |
| `ConnectionStrings:MssqlDeals`, `OracleDeals` | строки подключения к источникам сделок | нет |

Cron в 5-полевом формате: `минута час день-месяца месяц день-недели`.
Время — в поясе контейнера (переменная `TZ`).

Пароли и строки подключения передаются через переменные окружения:
`Smtp__Password`, `ConnectionStrings__MssqlDeals` и т.д.

Переопределение `Options` переменной окружения привязано к индексу события:
`ScheduledEvents__Events__1__Options__To`. Порядок событий в `appsettings.json`
меняйте осторожно — индексы в ConfigMap и переменных сдвинутся.

## Как ведёт себя планировщик

- Пока предыдущий запуск задачи не закончился, следующее срабатывание
  пропускается с предупреждением в логе. Параллельных запусков одной задачи нет.
- Ошибка задачи пишется в лог, повторных попыток нет. Следующий запуск — по расписанию.
- Если сервис не работал в момент срабатывания (деплой, падение), запуск
  не догоняется. Срабатывания, пришедшиеся на время, пока событие было
  выключено, тоже не запускаются.
- При остановке сервис ждёт текущую задачу до 30 секунд (`HostOptions.ShutdownTimeout`).
- Новое событие в конфиге подхватывается только после рестарта.

## Сверка сделок

Каждую ночь сервис берёт сделки за последние `LookbackDays` закрытых дней
`[сегодня − N, сегодня)` из всех источников и сравнивает их по `DealId`.
Если расхождения есть, на адрес `Options:To` события уходит HTML-отчёт.

Что считается расхождением:
- сделки нет в части источников;
- в источнике несколько записей с одним `DealId`;
- отличаются сумма, валюта или дата.

Как добавить источник: реализовать `IDealsSource` (маппинг своей схемы в
`DealRecord`) и зарегистрировать в `AddDealsReconciliation`. Источники
опрашиваются параллельно, поэтому у каждого должен быть свой `DbContext`.

Ограничение: если даты сделки в разных источниках попали по разные стороны
границы периода, в отчёте она будет «отсутствует», а не «расходится дата».

## Деплой

Сборка — общим пайплайном `invest_banking/tech/cicd` (`.gitlab-ci.yml`),
образ — `src/CronTaskRunner.Sample/Dockerfile`.

В кубере:
- **Одна реплика** и стратегия `Recreate`. Каждая реплика запускает задачи
  сама, и при двух подах сверка уйдёт дважды.
- **`TZ=Europe/Moscow`** уже задан в Dockerfile, иначе cron сработает по UTC.
- **`Smtp__Enabled=true`** на проде, иначе отчёты только в логе.
- Если сверка идёт дольше 30 секунд, деплой её оборвёт. Не деплойте около
  03:00 или увеличьте `terminationGracePeriodSeconds` вместе с `ShutdownTimeout`.
- Чтобы менять расписание без рестарта, `appsettings.json` монтируется из
  ConfigMap без `subPath` и задаётся `DOTNET_USE_POLLING_FILE_WATCHER=true`.

## Структура

```
src/CronTaskRunner.Core/
  Abstractions/      IScheduledTask, ScheduledTaskBase — контракт задачи
  Configuration/     модель секции ScheduledEvents, контекст события, IEventOptions<T>
  Scheduling/        диспетчер, раннер события, проверка конфига, реестр задач
  Extensions/        AddCronTaskRunner, AddScheduledTask<T>
src/CronTaskRunner.Sample/
  Deals/             сверка сделок; Mssql/, Oracle/ — источники на EF Core
  ExchangeRates/     пример простой задачи
  Email/             IEmailSender: SMTP или лог
tests/               тесты, повторяют структуру src/
```

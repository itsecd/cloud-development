# Лабораторные работы №1-3

**Вариант:** №21 - Кредитная заявка  
**Балансировка:** Weighted Round Robin  
**Выполнил:** Гусарова Маргарита 6512

## Что реализовано

- Генерация сущности "Кредитная заявка" через Bogus.
- Кэширование результатов генерации через IDistributedCache (Redis) с TTL 10 минут.
- Структурное логирование запросов и результатов генерации.
- Оркестрация сервисов через .NET Aspire.
- API Gateway на `Ocelot`.
- Запуск трех реплик `ProjectApp.Api`.
- Балансировка нагрузки `Weighted Round Robin` с весами `3:2:1`.
- REST endpoint: `GET /api/creditapplication?id={id}`.
- Gateway endpoint: `GET /applications?id={id}`.
- Отправка событий о заявках в `SQS`.
- Файловый сервис, сохраняющий заявки в `Minio`.
- Интеграционный тест сценария `Gateway -> API -> SQS -> FileService -> Minio`.
- Клиентская карточка для запроса и просмотра кредитной заявки.


## Характеристики генерируемой заявки

1. Идентификатор в системе — `int`
2. Тип кредита — `string`
3. Запрашиваемая сумма — `decimal`
4. Срок в месяцах — `int`
5. Процентная ставка — `double`
6. Дата подачи — `DateOnly`
7. Необходимость страховки — `bool`
8. Статус заявки — `string`
9. Дата решения — `DateOnly?`
10. Одобренная сумма — `decimal?`

## Правила генерации

- Тип кредита выбирается из справочника: "Потребительский", "Ипотека", "Автокредит", "Рефинансирование", "Образовательный", "Кредитная карта", "Бизнес".
- Процентная ставка не ниже актуальной ставки из конфигурации (`FinanceSettings:MinInterestRatePercent`), округляется до двух знаков.
- Запрашиваемая и одобренная суммы округляются до двух знаков; одобренная меньше или равна запрашиваемой и заполняется только для статуса "Одобрена".
- Дата подачи не позднее текущей и не более чем 2 года назад.
- Статус выбирается из: "Новая", "В обработке", "Одобрена", "Отклонена".
- Дата решения заполняется только для терминальных статусов ("Одобрена", "Отклонена") и позже даты подачи.

## Стек

- .NET 8, ASP.NET Core Web API
- Генерация данных: Bogus
- Кэш: IDistributedCache (Redis)
- Оркестрация: .NET Aspire (AppHost поднимает Redis и API)
- Брокер сообщений: SQS через LocalStack
- Объектное хранилище: Minio
- Клиент: Blazor WebAssembly + Blazorise (Bootstrap)
- Тесты: xUnit

Проекты:
- ProjectApp.Api — API для кредитных заявок
- ProjectApp.Gateway — API Gateway на Ocelot c кастомным Weighted Round Robin
- ProjectApp.Domain — доменные сущности
- ProjectApp.FileService — файловый сервис, читающий события из SQS и сохраняющий заявки в Minio
- ProjectApp.AppHost — оркестрация (.NET Aspire) с Redis, LocalStack SQS, Minio, Gateway, FileService и тремя репликами API
- Client.Wasm - веб-клиент (карточка "Кредитная заявка")
- ProjectApp.Tests — модульные и интеграционные тесты backend

## Эндпойнты

- GET `/api/creditapplication?id={id}` — получить или сгенерировать заявку, с кэшированием
- GET `/applications?id={id}` — получить заявку через gateway с балансировкой между репликами

## Лабораторная работа №2

Во второй лабораторной работе настроены:

- несколько реплик сервиса генерации `ProjectApp.Api`;
- API Gateway на базе `Ocelot`;
- собственная реализация алгоритма `Weighted Round Robin`;
- маршрутизация через gateway к трем репликам API по схеме `R1, R1, R1, R2, R2, R3, ...`.

## Лабораторная работа №3

В третьей лабораторной работе добавлены:

- объектное хранилище `Minio` для сохранения JSON-файлов кредитных заявок;
- брокер сообщений `SQS` через `LocalStack`;
- файловый сервис `ProjectApp.FileService`, который читает события из очереди и сохраняет заявки в Minio;
- публикация события `CreditApplicationGeneratedEvent` из `ProjectApp.Api` после получения заявки;
- интеграционный тест, проверяющий получение заявки через gateway и появление соответствующего JSON-файла в объектном хранилище.

Файлы сохраняются в bucket `credit-applications` с ключом:

```text
credit-applications/{id}-{timestamp}.json
```

## Кэширование

- IDistributedCache (Redis), ключ `credit-application-{id}`
- TTL: `CacheSettings:ExpirationMinutes` (по умолчанию 10 минут)
- При промахе кэша заявка генерируется и сохраняется

## Запуск проекта

Вариант 1 — через Aspire (поднимет Redis, 3 реплики API, gateway и клиент):

```bash
dotnet run --project ProjectApp.AppHost
```

Вариант 2 — отдельно gateway, API и клиент:

```bash
dotnet run --project ProjectApp.Api          # API по launchSettings
dotnet run --project ProjectApp.Gateway      # Gateway на http://localhost:5224
dotnet run --project Client.Wasm             # Клиент на http://localhost:5127
```

Проверка тестов:

```bash
dotnet test ProjectApp.Tests/ProjectApp.Tests.csproj
```

## Скриншоты

### Карточка кредитной заявки
<p align="center">
  <img src="screenshots/credit-application-card.png" alt="Карточка кредитной заявки" width="900">
</p>

### Lab 3: Aspire dashboard
<p align="center">
  <img src="screenshots/lab3-aspire-dashboard.png" alt="Aspire dashboard с ресурсами lab3" width="900">
</p>

### Lab 3: Клиент с заявкой
<p align="center">
  <img src="screenshots/lab3-client-credit-application.png" alt="Клиент с кредитной заявкой" width="900">
</p>

### Lab 3: Minio с сохраненным JSON-файлом
<p align="center">
  <img src="screenshots/lab3-minio-credit-application.png" alt="Minio с сохраненным JSON-файлом кредитной заявки" width="900">
</p>

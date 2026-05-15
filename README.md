# Лабораторные работы — «Кэширование» и «Балансировка нагрузки»

**Вариант:** «Медицинский пациент»  
**Балансировка:** Weighted Random  
**Брокер:** SNS  
**Хостинг S3:** Minio  
**Выполнил:** Котлярский Вадим, 6512

## Что реализовано

### ЛР1 — «Кэширование»

- Генерация сущности «Медицинский пациент» через Bogus.
- Кэширование результатов генерации через `IDistributedCache` (Redis) с TTL 10 минут.
- Структурное логирование запросов и результатов генерации.
- Оркестрация сервисов через .NET Aspire.
- REST endpoint: `GET /api/patient?id={id}`.

### ЛР2 — «Балансировка нагрузки»

- Реализован API Gateway на базе Ocelot (`ProjectApp.ApiGateway`).
- Поднято 3 реплики сервиса генерации в Aspire:
  - `projectapp-api-0` -> `http://localhost:7173`
  - `projectapp-api-1` -> `http://localhost:7174`
  - `projectapp-api-2` -> `http://localhost:7175`
- Gateway настроен на маршрут `GET /api/patient`.
- Имплементирован кастомный алгоритм балансировки `Weighted Random`.
- Настроены веса реплик:
  - `projectapp-api-0` — вес 5
  - `projectapp-api-1` — вес 3
  - `projectapp-api-2` — вес 2

## Характеристики генерируемого пациента

1. Идентификатор в системе — `int`
2. ФИО пациента — `string`
3. Адрес проживания — `string`
4. Дата рождения — `DateOnly`
5. Рост — `double`
6. Вес — `double`
7. Группа крови — `int`
8. Резус-фактор — `bool`
9. Дата последнего осмотра — `DateOnly`
10. Отметка о вакцинации — `bool`

## Правила генерации

- ФИО пациента: конкатенация фамилии, имени и отчества через пробел.
- Для отчества в качестве основы всегда используется мужское имя.
- Адрес проживания генерируется через секцию `Address`.
- Дата рождения не может быть позже текущей даты.
- Дата последнего осмотра не может быть раньше даты рождения.
- Рост и вес округляются до 2 знаков после запятой.
- Для группы крови, резус-фактора и отметки о вакцинации используется взвешенная случайность.

## Архитектура

- `ProjectApp.AppHost` — оркестрация Aspire.
- `ProjectApp.Api` — сервис генерации и кэширования.
- `ProjectApp.ApiGateway` — API Gateway и балансировка нагрузки.
- `ProjectApp.ServiceDefaults` — общие настройки observability и health checks.
- `Client.Wasm` — клиентское приложение.
- Redis + Redis Commander — кэш и администрирование.

## Запуск

1. Сборка решения:

```bash
dotnet build .\CloudDevelopment.sln
```

2. Запуск оркестрации:

```bash
dotnet run --project .\ProjectApp.AppHost\ProjectApp.AppHost.csproj
```

3. Открыть Aspire Dashboard по URL из консоли.

## Проверка API

- Через gateway:
  - `https://localhost:7139/api/patient?id=1`
- Напрямую в реплики:
  - `http://localhost:7173/api/patient?id=1`
  - `http://localhost:7174/api/patient?id=1`
  - `http://localhost:7175/api/patient?id=1`

## Проверка балансировки

- Отправить серию запросов через gateway на `GET /api/patient?id={id}`.
- Проверить в логах или Aspire Dashboard, что запросы распределяются между репликами с учётом весов `5:3:2`.

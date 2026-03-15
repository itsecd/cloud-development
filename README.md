# Лабораторная работа №1 «Кэширование»

## Вариант №39 — «Программный проект»

## Описание

Реализован сервис генерации данных о программных проектах с кэшированием ответов в Redis и оркестрацией через .NET Aspire

## Что реализовано

### Генерация данных (Bogus)
- Класс `SoftwareProjectFaker` с `RuleFor` для каждого поля
- Генерация ФИО менеджера с отчеством, образованным от мужского имени с учётом пола (окончания «ович/овна», «евич/евна»)
- Локаль `ru` для русскоязычных имён

### Кэширование (Redis + IDistributedCache)
- Сервис `SoftwareProjectService` с интерфейсом `ISoftwareProjectService`

### Структурное логирование
- Логирование на английском языке через `ILogger<T>`
- Структурные параметры `{ProjectId}` для корреляции
- Отдельные уровни: `Information` для успешных операций, `Warning` для ошибок кэша, `Error` для ошибок генерации

### CORS
- Разрешён только `GET`-запрос
- Доверенные URL вынесены в `appsettings.json` (`TrustedOrigins`)

### Оркестрация (.NET Aspire)
- Redis с RedisInsight
- API сервис ждёт Redis (`WaitFor(cache)`)
- Клиент WASM ждёт API сервис (`WaitFor(softwareProjectsApi)`)

### API
- Единственный эндпоинт: `GET /api/software-projects?id={id}`
- Minimal API

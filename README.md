# Лабораторная работа №1 «Кэширование»
## Вариант №18 — «Медицинский пациент»
## Описание
Реализован сервис генерации данных о медицинских пациентах с кэшированием ответов в Redis и оркестрацией через .NET Aspire

## Студент
**Чумаков Иван Игоревич**, группа 6511

## Что реализовано
### Генерация данных (Bogus)
- Класс `MedicalPatientGenerator` с `RuleFor` для каждого поля
- Генерация ФИО с отчеством на основе фамилии с суффиксом «ович»
- Локаль `ru` для русскоязычных имён

### Кэширование (Redis + IDistributedCache)
- Сервис `PatientService` с кэшированием через `IDistributedCache`
- TTL вынесен в `appsettings.json` (`CacheSettings:AbsoluteExpirationMinutes`)

### Структурное логирование
- Логирование через `ILogger<T>` с Serilog
- Структурные параметры `{Id}`, `{FullName}`, `{BirthDate}` и др.
- `Information` для Cache HIT/MISS и успешной генерации

### CORS
- Разрешён только `localhost`-origin в Development-окружении
- Доверенный origin клиента передаётся через Aspire (`Cors:AllowedOrigin`)

### Оркестрация (.NET Aspire)
- Redis с RedisInsight
- API сервис ждёт Redis (`WaitFor(redis)`)
- Клиент WASM ждёт API сервис (`WaitFor(generatorService)`)

### API
- Единственный эндпоинт: `GET /patient?id={id}`
- Minimal API с XML-документацией для Swagger
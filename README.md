# Лабораторная работа №2 «Балансировка нагрузки»
## Вариант №18 — «Медицинский пациент»
## Описание
Реализован API Gateway с балансировкой нагрузки между тремя репликами сервиса генерации данных о медицинских пациентах с кэшированием в Redis и оркестрацией через .NET Aspire

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

### API Gateway (Ocelot)
- Маршрутизация запросов через `ApiGateway` к репликам `GeneratorService`
- Кастомный балансировщик нагрузки `WeightedRoundRobinBalancer`
- CORS настроен на уровне Gateway — разрешён только origin клиента, переданный через Aspire

### Оркестрация (.NET Aspire)
- Redis с RedisInsight
- Три реплики `GeneratorService` с фиксированными портами (15000–15002)
- `ApiGateway` ссылается на все реплики через `WithReference` и динамически переопределяет адреса Ocelot из переменных окружения Aspire
- Клиент WASM (`Client.Wasm`) взаимодействует только с Gateway

### API
- Единственный эндпоинт: `GET /patient?id={id}`
- Minimal API с XML-документацией для Swagger

# Лабораторная работа №3 «Интеграционное тестирование»
## Вариант №18 — «Медицинский пациент»
## Описание
Реализован файловый сервис с объектным хранилищем MinIO и брокером сообщений SNS/SQS (LocalStack). Генерируемые данные о пациентах публикуются в SNS и сохраняются в файлы через FileService. Добавлены интеграционные тесты на базе Aspire Testing.

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
- Маршрутизация запросов к репликам `GeneratorService`
- Кастомный балансировщик нагрузки `WeightedRoundRobinBalancer` (R1:3, R2:2, R3:1)
- CORS настроен на уровне Gateway с динамическим origin от Aspire

### Брокер сообщений (SNS + SQS через LocalStack)
- `GeneratorService` публикует сгенерированного пациента в SNS-топик при Cache MISS
- `FileService` создаёт SQS-очередь, подписывает её на SNS-топик и опрашивает очередь
- Публикация не блокирует ответ API при недоступности брокера

### Файловый сервис + объектное хранилище (MinIO)
- `FileService` принимает сообщения из SQS и сохраняет JSON-файлы в MinIO
- Имя файла: `patient-{id}-{yyyyMMddHHmmss}.json`
- Бакет создаётся автоматически при старте сервиса
- Endpoint `GET /files` возвращает список сохранённых файлов

### Интеграционные тесты (xUnit + Aspire.Hosting.Testing)
- `GetPatient_ValidId_ReturnsPatient` — проверка корректного ответа через Gateway
- `GetPatient_InvalidId_ReturnsBadRequest` — проверка валидации входных данных
- `GetPatient_SameId_ReturnsCachedPatient` — проверка работы кэша Redis
- `GetPatient_DifferentIds_ReturnDifferentPatients` — проверка независимости записей
- `GetPatient_FileAppearsInMinio` — E2E тест: запрос → SNS → SQS → MinIO

### Оркестрация (.NET Aspire)
- Redis с RedisInsight
- LocalStack (SNS + SQS)
- MinIO с консолью управления на порту 9001
- Три реплики `GeneratorService` (порты 15000–15002) с балансировкой через Gateway
- `FileService` получает адреса LocalStack и MinIO через переменные окружения Aspire

### API
- Единственный публичный эндпоинт: `GET /patient?id={id}` (через Gateway)
- `GET /files` (FileService) — список файлов в MinIO

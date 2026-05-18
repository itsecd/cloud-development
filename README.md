# Лабораторная работа №1 — «Кэширование»

**Вариант:** №1 — Транспортное средство

---

## Цель работы

Реализовать сервис генерации данных о транспортных средствах с кэшированием ответов при помощи Redis и структурным логированием.

---

## Стек технологий

| Компонент | Технология |
|---|---|
| Серверная часть | ASP.NET Core Web API (.NET 8) |
| Генерация данных | Bogus 35.6.5 |
| Кэширование | IDistributedCache + Redis (Aspire) |
| Логирование | Microsoft.Extensions.Logging (структурное) |
| Оркестрация | .NET Aspire 9.5.2 |
| Клиент | Blazor WebAssembly (.NET 8) |

---

## Модель данных — Транспортное средство

| # | Название | Тип | Источник (Bogus) | Ограничение |
|---|---|---|---|---|
| 1 | Идентификатор в системе | `int` | параметр запроса | — |
| 2 | VIN-номер | `string` | `Vehicle.Vin()` | — |
| 3 | Производитель | `string` | `Vehicle.Manufacturer()` | — |
| 4 | Модель | `string` | `Vehicle.Model()` | — |
| 5 | Год выпуска | `int` | `Date.Past(30).Year` | ≤ текущий год |
| 6 | Тип корпуса | `string` | `Vehicle.Type()` | — |
| 7 | Тип топлива | `string` | `Vehicle.Fuel()` | — |
| 8 | Цвет корпуса | `string` | `Commerce.Color()` | — |
| 9 | Пробег | `double` | `Random.Double(0, 500_000)` | ≥ 0 |
| 10 | Дата последнего техобслуживания | `DateOnly` | `Date.Between(...)` | ≥ год выпуска |

---

## API

### `GET /api/vehicle?id={id}`

Возвращает транспортное средство по идентификатору.
При первом запросе — генерирует и сохраняет в кэш.
При повторном запросе — возвращает из кэша (TTL 15 минут).

---

# Лабораторная работа №2 — «Балансировка нагрузки»

**Вариант алгоритма:** Weighted Round Robin

---

## Цель работы

Настроить оркестрацию на запуск нескольких реплик сервиса генерации, реализовать API Gateway на основе Ocelot и имплементировать алгоритм балансировки нагрузки Weighted Round Robin.

## Что было сделано

### 1. Несколько реплик сервиса генерации

В [VehicleApp.AppHost/AppHost.cs](VehicleApp/VehicleApp.AppHost/AppHost.cs) поднимается **5 реплик** сервиса `vehicleapp-api-{0..4}` на портах 5250–5254. Каждая реплика подключена к общему Redis-кэшу. Gateway получает ссылки на все реплики через `.WithReference(api)` — Aspire прокидывает их адреса в gateway через переменные окружения `services__vehicleapp-api-{i}__https__0`.

### 2. API Gateway на Ocelot

Проект [Api.Gateway](Api.Gateway/) — единая точка входа для клиента. Маршрут описан в [ocelot.json](Api.Gateway/ocelot.json):

- **Upstream:** `GET /vehicle` → **Downstream:** `/api/vehicle` на одну из реплик
- `LoadBalancerOptions.Type` = `WeightedRoundRobinLoadBalancer`
- `DangerousAcceptAnyServerCertificateValidator: true` — принимаем dev-сертификаты реплик

---

# Лабораторная работа №3 — «Интеграционное тестирование»

**Вариант:** SQS + MinIO

---

## Цель работы

Добавить в оркестрацию объектное хранилище, реализовать файловый сервис, который сериализует сгенерированные данные в файлы и сохраняет их в объектном хранилище, наладить отправку данных в файловый сервис через брокер сообщений и покрыть бекенд интеграционными тестами.

---

## Что было сделано

### 1. Объектное хранилище и брокер в оркестрации

В [VehicleApp.AppHost/AppHost.cs](VehicleApp/VehicleApp.AppHost/AppHost.cs) добавлены:

- контейнер **LocalStack** (`vehicle-localstack`, порт 4566) для эмуляции SQS;
- контейнер **MinIO** (`vehicle-minio`) — объектное хранилище;
- `AddAWSCloudFormationTemplate("resources", "CloudFormation/vehicle-template-sqs.yaml", ...)`
Каждая реплика `vehicleapp-api-{i}` и `file-service` получают ссылку на CloudFormation-стек через `.WithReference(awsResources)`, а `file-service` — дополнительно ссылку на MinIO и переменную `AWS__Resources__MinioBucketName=vehicle-bucket`.

### 2. Файловый сервис ([File.Service](File.Service/))

- [Storage/IFileStorage.cs](File.Service/Storage/IFileStorage.cs), [Storage/MinioFileStorage.cs](File.Service/Storage/MinioFileStorage.cs) — интерфейс файлового хранилища и его реализация поверх MinIO. Сериализованный JSON загружается в бакет под ключом `vehicle_{id}.json`.
- [Messaging/SqsConsumerService.cs](File.Service/Messaging/SqsConsumerService.cs) — фоновый `BackgroundService`, который батчами читает сообщения из SQS (`MaxNumberOfMessages=10`, `WaitTimeSeconds=5`), отдаёт payload в `IFileStorage` и удаляет сообщение из очереди.
- [Controllers/StorageController.cs](File.Service/Controllers/StorageController.cs) — REST-эндпойнты `GET /api/s3` (список ключей) и `GET /api/s3/{key}` (содержимое файла) — используются интеграционными тестами для проверки результата.
- [Program.cs](File.Service/Program.cs) — регистрация AWS SDK через `AddLocalStack`, MinIO-клиент через `AddMinioClient("vehicle-minio")` и автоматическое создание бакета при старте (`EnsureBucketExistsAsync`).

### 3. Отправка генерируемых данных через брокер

В [VehicleApp.Api](VehicleApp.Api/):

- [Services/IVehicleProducer.cs](VehicleApp.Api/Services/IVehicleProducer.cs), [Services/SqsVehicleProducer.cs](VehicleApp.Api/Services/SqsVehicleProducer.cs) — продюсер, отправляющий сериализованное `Vehicle` в очередь по имени из `AWS:Resources:SQSQueueName`.
- [Services/VehicleService.cs](VehicleApp.Api/Services/VehicleService.cs) — после генерации нового транспортного средства (cache miss) вызывает `producer.SendAsync(vehicle)` перед записью в кэш. На cache hit отправка не происходит.
- [Program.cs](VehicleApp.Api/Program.cs) — регистрация LocalStack/SQS-клиента и продюсера в DI.

### 4. Интеграционные тесты

[VehicleApp/VehicleApp.AppHost.Tests/IntegrationTest1.cs](VehicleApp/VehicleApp.AppHost.Tests/IntegrationTest1.cs) — два `[Fact]`-теста, поднимающие весь AppHost через `DistributedApplicationTestingBuilder`:

- **`GatewayResponse_IsPersistedToObjectStorage`** — выполняет `GET /vehicle?id=...` через шлюз, после паузы запрашивает `GET /api/s3/vehicle_{id}.json` у файлового сервиса и сравнивает оба объекта через `Assert.Equivalent`.
- **`ObjectStorageList_ContainsGeneratedVehicle`** — после запроса транспортного средства убеждается, что список `GET /api/s3` содержит ключ `vehicle_{id}.json`.




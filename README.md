# Лабораторная работа №1 «Кэширование»

## Описание

Реализация сервиса генерации данных о транспортных средствах с кэшированием ответов в Redis.
Оркестрация проектов при помощи .NET Aspire.

## Технологии

- .NET 8
- .NET Aspire 9.5
- Bogus — генерация фейковых данных
- Redis — распределённое кэширование (`IDistributedCache`)
- Blazor WebAssembly — клиентское приложение
- Blazorise — UI-компоненты
- OpenTelemetry — структурное логирование и трассировка

## Характеристики генерируемого объекта

| № | Характеристика | Тип данных | Источник Bogus |
|---|---|---|---|
| 1 | Идентификатор в системе | `int` | Параметр запроса |
| 2 | VIN-номер | `string` | `Vehicle.Vin()` |
| 3 | Производитель | `string` | `Vehicle.Manufacturer()` |
| 4 | Модель | `string` | `Vehicle.Model()` |
| 5 | Год выпуска | `int` | `Random.Int(1960, текущий год)` |
| 6 | Тип корпуса | `string` | `Vehicle.Type()` |
| 7 | Тип топлива | `string` | `Vehicle.Fuel()` |
| 8 | Цвет корпуса | `string` | `Commerce.Color()` |
| 9 | Пробег | `double` | `Random.Double(0, 1000000)` |
| 10 | Дата последнего техобслуживания | `DateOnly` | Между годом выпуска и текущей датой |

### Кэширование

- Ключ кэша: `vehicle:{id}`
- Время жизни: настраивается в `appsettings.json` (`Cache:ExpirationMinutes`, по умолчанию 5 минут)
- При ошибке чтения/записи кэша — логируется warning, запрос обрабатывается без кэша

## API

```
GET /api/vehicle?id={id}
```

Возвращает JSON с данными транспортного средства. При повторном запросе с тем же `id` — данные берутся из кэша Redis.

---

# Лабораторная работа №2 «Балансировка нагрузки»

## Описание

Реализация API-шлюза на основе Ocelot с алгоритмом балансировки нагрузки **Weighted Round Robin**.
Оркестрация запускает несколько реплик сервиса генерации через .NET Aspire.

## Алгоритм: Weighted Round Robin

Каждой реплике присваивается числовой вес из конфигурации (`LoadBalancer:Weights` в `appsettings.json` шлюза).
Реплики перебираются циклически; реплика с весом `W` обрабатывает `W` запросов подряд перед переходом к следующей.

### Конфигурация весов (`Api.Gateway/appsettings.json`)

```json
"LoadBalancer": {
  "Weights": [ 3, 2, 1, 1, 1 ]
}
```

Индекс массива соответствует порядковому номеру реплики (0-based). Если вес не задан — используется значение `1`.

## Оркестрация реплик (`VehicleVault.AppHost`)

Aspire запускает 5 независимых экземпляров `VehicleVault.Api` на портах `8000–8004`:

---

# Лабораторная работа №3 «Файловое хранилище и брокер сообщений»

## Описание

Расширение пайплайна асинхронной выгрузкой сгенерированных транспортных средств в объектное
хранилище. После генерации `VehicleVault.Api` отправляет JSON-представление ТС в очередь **AWS SQS**;
файловый сервис `File.Service` фоново читает очередь и складывает каждый объект в **S3-бакет**
(имя файла — `vehicle_{id}.json`). Очередь и бакет поднимаются эмулятором **LocalStack**, который
оркеструется через Aspire и инициализируется CloudFormation-шаблоном.

> **Вариант лабораторной:** SQS + LocalStack S3

## Технологии

- AWSSDK.SQS, AWSSDK.S3 — клиенты AWS
- LocalStack.Client + LocalStack.Client.Extensions — переадресация AWS SDK на LocalStack
- LocalStack.Aspire.Hosting — запуск контейнера LocalStack из Aspire
- CloudFormation — декларативное создание SQS-очереди и S3-бакета
- AWSSDK CloudFormation (через Aspire.Hosting.AWS, транзитивно) — применение шаблона

## Имена ресурсов и параметры

CloudFormation-шаблон [`VehicleVault.AppHost/CloudFormation/vehiclevault-template.yaml`](VehicleVault/VehicleVault.AppHost/CloudFormation/vehiclevault-template.yaml)
создаёт два ресурса:

| Ресурс            | Имя по умолчанию      | Параметры                                                |
|-------------------|-----------------------|----------------------------------------------------------|
| `AWS::SQS::Queue` | `vehiclevault-queue`  | VisibilityTimeout=30s, MessageRetentionPeriod=4 дня      |
| `AWS::S3::Bucket` | `vehiclevault-bucket` | Versioning=Suspended, public access заблокирован         |

Имена прокидываются в сервисы через `Outputs` шаблона (`SQSQueueName`, `S3BucketName`)
и читаются из конфигурации по ключам `AWS:Resources:SQSQueueName` и `AWS:Resources:S3BucketName`.

## S3-сервис

`S3VehicleStorageService` ([File.Service/Storage/S3VehicleStorageService.cs](File.Service/Storage/S3VehicleStorageService.cs)):

- `EnsureBucketExists()` — вызывается при старте `File.Service`.
- `Upload(json)` — сохраняет ТС с ключом `vehicle_{SystemId}.json`.
- `Download(key)` / `ListKeys()` — чтение для эндпоинтов проверки.

## API файлового сервиса

```
GET /api/files            — список ключей файлов в бакете
GET /api/files/{id}       — JSON транспортного средства; 404, если файла нет
```

Используется интеграционным тестом и пригоден для ручной проверки через Swagger.

## Интеграционный тест

[VehicleVault.AppHost.Tests/IntegrationTest.cs](VehicleVault/VehicleVault.AppHost.Tests/IntegrationTest.cs):

1. Поднимает весь Aspire-граф через `DistributedApplicationTestingBuilder`.
2. Дёргает `GET /vehicle?id={id}` через `api-gateway`.
3. Ждёт 5 секунд (за это время SQS-консьюмер должен забрать сообщение и положить файл в S3) и читает `GET /api/files/{id}` у `file-service`.
4. Сравнивает ответ гейтвея с содержимым файла в S3 (`Assert.Equivalent`).

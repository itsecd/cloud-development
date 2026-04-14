# Лабораторная работа №1 — «Кэширование»

## Описание

Микросервисный бэкенд для генерации кредитных заявок с кэшированием ответов в Redis. Оркестрация через .NET Aspire.

## Стек технологий

- .NET 8, ASP.NET Core Minimal API
- .NET Aspire (оркестрация, Service Defaults, Dashboard)
- Redis (кэширование через `IDistributedCache`)
- Redis Insight (веб-интерфейс для Redis)
- Bogus (генерация данных)
- Blazor WebAssembly (клиент)

## Что реализовано

### Сервис генерации данных
- Генератор `CreditApplicationGenerator` 
- `Faker<CreditApplication>` с бизнес-правилами:
  - Дата решения заполняется только для терминальных статусов
  - Одобренная сумма заполняется только при статусе «Одобрена» и не превышает запрашиваемую
  - Дата подачи — не более 2 лет назад от текущей даты
  - Процентная ставка — от 15.5%

### Кэширование
- `CreditApplicationService` с `IDistributedCache` (Redis)
- Ключ кэша: `credit-application:{id}`
- Время жизни кэша настраивается через `appsettings.json` (`CacheSettings:ExpirationMinutes`)
- Раздельная обработка ошибок: чтение из кэша, генерация, запись в кэш

### Оркестрация (.NET Aspire)
- `CreditApp.AppHost` управляет запуском всех компонентов
- Redis с веб-интерфейсом Redis Insight (`.WithRedisInsight()`)
- API ждёт готовности Redis (`.WaitFor(redis)`)
- Клиент ждёт готовности API (`.WaitFor(api)`)
- Aspire Dashboard для мониторинга логов, трейсов и метрик

## API

```
GET /credit-application?id={int}
```

Возвращает JSON с данными кредитной заявки. При повторном запросе с тем же `id` возвращает данные из кэша.

---

# Лабораторная работа №2 — «Балансировка нагрузки»

## Описание

API-шлюз на основе Ocelot с кастомным алгоритмом балансировки нагрузки Query Based. Клиентские запросы распределяются по репликам сервиса генерации на основе параметра `id` из строки запроса.

## Что реализовано

### Репликация сервиса генерации
- Aspire AppHost запускает 3 реплики `CreditApp.Api` на портах 8000, 8001, 8002
- Гейтвей ожидает готовности всех реплик перед стартом (`.WaitFor()`)

### API Gateway (Ocelot)
- Проект `Api.Gateway` — единая точка входа для клиента
- Маршрутизация настроена через `ocelot.json`
- CORS-политика вынесена в конфигурацию (`CorsSettings:AllowedOrigins`)

### Кастомный балансировщик — `QueryBasedLoadBalancer`
- Реализует интерфейс `ILoadBalancer` из Ocelot
- Алгоритм: `index = id % N`, где `N` — число реплик
- При отсутствии параметра `id` запрос направляется на первую реплику

---

# Лабораторная работа №3 — «Интеграционное тестирование»

## Описание

Файловый сервис с объектным хранилищем Minio и брокером сообщений SNS. Интеграционные тесты, проверяющие корректность работы всех сервисов бэкенда вместе.

## Стек технологий

- SNS (брокер сообщений, через LocalStack)
- Minio (объектное хранилище, S3-совместимое)
- LocalStack (эмуляция AWS-сервисов локально)
- AWS CloudFormation (провизионирование SNS-топика)
- xUnit + Aspire.Hosting.Testing (интеграционные тесты)

## Что реализовано

### Публикация сообщений в SNS (`CreditApp.Api`)
- `IProducerService` — интерфейс службы отправки сообщений в брокер
- `SnsPublisherService` — реализация, сериализует кредитную заявку в JSON и публикует в SNS-топик
- `CreditApplicationService` после генерации новой заявки отправляет её в SNS через `IProducerService`

### Файловый сервис (`Service.FileStorage`)
- Принимает SNS-нотификации через HTTP-вебхук (`POST /api/sns`)
- `SnsSubscriberController` — обрабатывает подтверждение подписки (`SubscriptionConfirmation`) и нотификации (`Notification`)
- `SnsSubscriptionService` — подписывается на SNS-топик при старте приложения
- `S3MinioService` — загрузка, скачивание и листинг файлов в Minio
- `S3StorageController` — API для получения списка файлов (`GET /api/s3`) и скачивания файла (`GET /api/s3/{key}`)
- Файлы сохраняются в формате `creditapp_{id}.json`

### Оркестрация (.NET Aspire)
- LocalStack-контейнер с SNS и CloudFormation
- CloudFormation-шаблон `creditapp-template-sns.yaml` создаёт SNS-топик
- Minio-контейнер для объектного хранилища
- `Service.FileStorage` ожидает готовности LocalStack, Minio и CloudFormation-ресурсов
- SNS URL передаётся через переменную окружения для вебхука

### Интеграционные тесты (`CreditApp.AppHost.Tests`)
- Поднимают всю инфраструктуру через `DistributedApplicationTestingBuilder`
- 6 тестов:
  1. **GatewayReturnsOkForCreditApplication** — API Gateway возвращает 200 OK
  2. **ApiReturnsCreditApplicationWithCorrectId** — API возвращает заявку с правильным Id
  3. **RepeatedRequestReturnsCachedData** — повторный запрос возвращает закэшированные данные
  4. **PipelineSavesFileToMinio** — полный пайплайн: генерация → SNS → FileStorage → Minio
  5. **StoredDataMatchesApiResponse** — данные из API и из Minio идентичны
  6. **FileStorageHealthEndpointReturnsOk** — health endpoint файлового сервиса отвечает 200 OK
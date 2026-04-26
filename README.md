# Облачная разработка — Сервис генерации данных сотрудников

## Описание

Микросервисное приложение для генерации данных сотрудников компании с кэшированием и балансировкой нагрузки. Оркестрация осуществляется при помощи .NET Aspire.

## Лабораторная работа №1 — Кэширование

### Что реализовано

- **Сервис генерации сотрудников** (`EmployeeApp.Api`) на основе библиотеки Bogus
- **Сущность `Employee`** с 10 полями: идентификатор, ФИО, должность, отдел, дата приёма, оклад, электронная почта, номер телефона, индикатор увольнения, дата увольнения
- **Генерация данных** с учётом пола (ФИО, отчество, фамилия), корреляции оклада с суффиксом должности, формата телефона `+7(***)***-**-**`
- **Кэширование** ответов через `IDistributedCache` и Redis
- **Структурное логирование** с помощью `ILogger` (логирование попаданий/промахов кэша, ошибок)
- **Оркестрация** через .NET Aspire (AppHost поднимает Redis, API и Redis Commander)
- **CORS** настроен для приёма запросов от клиента

### Технологии

- .NET 8, Minimal API
- Bogus — генерация данных
- Redis — распределённый кэш
- .NET Aspire — оркестрация

## Лабораторная работа №2 — Балансировка нагрузки

### Что реализовано

- **API Gateway** на основе Ocelot с маршрутизацией запросов к бэкенду
- **5 реплик** сервиса `EmployeeApp.Api`, создаваемых в цикле через AppHost
- **Кастомный балансировщик нагрузки Query Based** — маршрутизация на основе параметра `id` из query string: `index = id % количество_реплик`
- **CORS** на Gateway настроен на приём только GET-запросов от клиента
- Gateway скрывает пути бэкенда через `UpstreamPathTemplate`

### Алгоритм балансировки Query Based

От запрашиваемого идентификатора сотрудника находится остаток от деления по модулю числа реплик. Он определяет индекс реплики, которая обработает запрос.

## Лабораторная работа №3 — Интеграционное тестирование (вариант SNS + Localstack)

### Что реализовано

- **Объектное хранилище** — S3-бакет `employee-bucket`, эмулируемый через Localstack (endpoint `http://localhost:4566`), создаётся CloudFormation-стеком `employee`
- **Брокер сообщений** — SNS-топик `employee-topic`, эмулируемый через Localstack, создаётся тем же CloudFormation-шаблоном
- **Сервис генерации (`EmployeeApp.Api`)** после промаха кэша публикует сериализованного `Employee` в SNS-топик (`SnsPublisherService` → `PublishAsync` с `TopicArn` из аутпутов CloudFormation)
- **Файловый сервис (`File.Service`)**:
  - на старте вызывает `EnsureBucketExists` и подписывает собственный HTTP-эндпоинт `POST /api/sns` на SNS-топик (`SnsSubscriptionService`)
  - `SnsSubscriberController` — вебхук, подтверждающий подписку (`SubscriptionConfirmation`) и принимающий `Notification`, при получении уведомления сериализует сообщение в `employee_{id}.json` и загружает в S3 через `S3AwsService`
  - `S3StorageController` — REST API `GET /api/s3` (список файлов) и `GET /api/s3/{key}` (содержимое файла)

- **Интеграционные тесты (`EmployeeApp.AppHost.Tests`)**:
  - `TestPipeline` — базовый end-to-end: дёргает Gateway `GET /employees?id=N`, ждёт 5 секунд прохода сообщения через SNS, читает список файлов и `employee_{id}.json` из S3 через `File.Service`; проверяет, что список содержит ровно один элемент `employee_{id}.json`, а содержимое файла эквивалентно ответу API (`Assert.Equivalent`)
  - `TestCacheHitDoesNotDuplicateS3File` — два последовательных запроса с одинаковым `id` возвращают идентичного сотрудника (Redis-hit) и в S3 создаётся **только один** файл `employee_{id}.json` — подтверждает, что публикация в SNS происходит только при промахе кэша
  - `TestMultipleIdsProduceDistinctS3Objects` — три разных идентификатора порождают три независимых файла в S3; для каждого `id` содержимое файла сверяется с соответствующим ответом API
  - `TestMissingS3KeyReturnsError` — запрос `GET /api/s3/{key}` с несуществующим ключом возвращает отличный от `200` статус, а не падает
  - `TestGatewayRoutesRequestForEveryReplica` — в цикле `id=1..5` дёргает Gateway, проверяет `200 OK` и корректный `Employee.Id` в ответе (подтверждает, что QueryBased балансировка маршрутизирует каждый запрос на нужную реплику)
  - десериализация JSON-ответов идёт через общий `JsonSerializerOptions { PropertyNameCaseInsensitive = true }` — API/S3 отдают camelCase, а `Employee` имеет PascalCase + `required`-свойства
  - логи тестов выводятся в xUnit через `MartinCostello.Logging.XUnit`

### Технологии

- `LocalStack.Aspire.Hosting` — контейнер Localstack в Aspire
- `Aspire.Hosting.AWS` — CloudFormation-шаблон + AWS SDK конфигурация
- `AWSSDK.S3`, `AWSSDK.SimpleNotificationService` — клиенты S3 и SNS
- `LocalStack.Client.Extensions` — перенаправление AWS SDK на Localstack
- `Aspire.Hosting.Testing` + `xUnit` + `MartinCostello.Logging.XUnit` — интеграционные тесты

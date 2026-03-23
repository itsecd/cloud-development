# Лабораторная работа №3

## «Объектное хранилище и брокер сообщений»

## Что было сделано

В рамках третьей лабораторной работы добавлено объектное хранилище, реализован файловый сервис, настроена асинхронная передача данных через брокер сообщений и написаны интеграционные тесты.

### 1. Добавлено объектное хранилище (S3 через LocalStack)

-   В оркестрацию AppHost добавлен контейнер LocalStack
-   Настроен health check по эндпоинту `/_localstack/health`.
-   Регион: `us-east-1`, порт: `4566`.

### 2. Реализован файловый сервис (CreditApp.FileService)

-   Сервис принимает сообщения из SQS-очереди через MassTransit consumer (`CreditApplicationCreatedConsumer`).
-   Полученные кредитные заявки сериализуются в JSON и сохраняются в S3-бакет `credit-files`.
-   Ключ файла: `credit-applications/{id}_{timestamp}.json`.
-   Реализован интерфейс `IS3FileStorage` и его реализация `S3FileStorage` с автоматическим созданием бакета.
-   AWS-конфигурация (ServiceUrl, Region, AccessKey, SecretKey) вынесена в `appsettings.json`.

### 3. Реализована отправка данных через брокер сообщений (SNS/SQS)

-   Брокер сообщений: Amazon SNS (Simple Notification Service) через LocalStack.
-   API публикует сообщение `CreditApplicationCreated` в SNS-топик через MassTransit (`IPublishEndpoint`).
-   FileService подписан на SNS-топик через SQS-очередь и получает сообщения через MassTransit consumer.
-   Сообщение публикуется только при cache miss (генерация новой заявки), что исключает дублирование файлов.
-   Цепочка: API → SNS → SQS → FileService Consumer → S3.

### 5. Реализованы интеграционные тесты (CreditApp.IntegrationTests)

Написано 10 тестов, проверяющих корректность работы всех сервисов вместе:

-   `Api_HealthCheck_ReturnsHealthy` — API отвечает на health check.
-   `FileService_HealthCheck_ReturnsHealthy` — FileService отвечает на health check.
-   `Gateway_GetCredit_ReturnsValidCreditApplication` — Gateway возвращает корректную заявку.
-   `Gateway_RepeatedRequests_ReturnsCachedData` — повторный запрос возвращает данные из Redis-кэша.
-   `Gateway_MultipleRequests_AllSucceed` — маршрутизация к API-репликам работает.
-   `Gateway_DifferentIds_ReturnDifferentApplications` — разные id возвращают разные заявки.
-   `Gateway_GetCredit_AllFieldsPopulated` — все обязательные поля заявки заполнены.
-   `GetCredit_FileServiceSavesToS3` — сквозной сценарий: Gateway → API → SNS → FileService → S3.
-   `GetCredit_S3FileContainsAllFields` — файл в S3 содержит все поля исходной заявки.
-   `GetCredit_CacheHit_DoesNotDuplicateS3File` — cache hit не создаёт дубликат файла в S3.

## Используемые технологии

-   .NET 8
-   ASP.NET Core Web API
-   Ocelot API Gateway
-   Blazor WebAssembly
-   Bogus
-   Redis / IDistributedCache
-   MassTransit + Amazon SNS/SQS
-   AWS S3 (LocalStack)
-   .NET Aspire
-   xUnit + Aspire.Hosting.Testing

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

---

# Лабораторная работа №2 «Балансировка нагрузки»

## Описание

Реализован API Gateway на основе Ocelot с кастомным алгоритмом балансировки нагрузки Weighted Random

## Что реализовано

### Несколько реплик сервиса генерации
- 5 реплик `SoftwareProjects.Api` на портах 5200–5204
- Оркестрация через .NET Aspire (`AppHost.cs`)

### API Gateway (Ocelot)
- Маршрутизация запросов `/software-projects` → `/api/software-projects`
- Конфигурация в `ocelot.json`

### Балансировщик Weighted Random
- Каждой реплике назначается вероятность выбора
- При поступлении запроса реплика выбирается случайно с учётом заданных вероятностей

---

# Лабораторная работа №3 «Интеграционное тестирование»

## Вариант: SNS + LocalStack

## Описание

В оркестрацию добавлены брокер сообщений **SNS** и объектное хранилище **S3**, которые
поднимаются в эмуляторе **LocalStack**. Реализован файловый сервис, принимающий уведомления
из SNS-топика и сохраняющий полезную нагрузку в S3-бакет в виде JSON-файлов. Реализованы
интеграционные тесты, проверяющие корректность работы всех сервисов бекенда вместе.

## Что реализовано

### Объектное хранилище и брокер (LocalStack)
- В `SoftwareProjects.AppHost` добавлен контейнер `softwareprojects-localstack` через
  `LocalStack.Aspire.Hosting`
- Ресурсы (S3-бакет `softwareprojects-bucket` и SNS-топик `softwareprojects-topic`)
  поднимаются в LocalStack из CloudFormation-шаблона
  [`CloudFormation/softwareprojects-template.yaml`](SoftwareProjects/SoftwareProjects.AppHost/CloudFormation/softwareprojects-template.yaml)
- Имена и ARN ресурсов автоматически прокидываются в сервисы через `WithReference(awsResources)`

### Публикация в SNS из API сервиса (`SoftwareProjects.Api`)
- Интерфейс `IProjectPublisher` с реализацией `SnsProjectPublisher`
- После генерации проекта `SoftwareProjectService` сериализует его в JSON и публикует в
  SNS-топик через `IAmazonSimpleNotificationService`
- LocalStack-клиент подключается через `LocalStack.Client.Extensions`

### Файловый сервис (`File.Service`)
- HTTP-подписчик SNS: контроллер `SnsWebhookController` принимает
  `SubscriptionConfirmation` и `Notification` от SNS-топика, подтверждает подписку и
  передаёт полезную нагрузку в объектное хранилище
- Реализация объектного хранилища `S3ObjectStorage` поверх AWS SDK (LocalStack):
  загрузка, листинг и скачивание файлов
- Контроллер `StorageController` (`/api/s3`) для просмотра содержимого бакета
- Подписка на SNS и `EnsureBucketExists` выполняются на старте приложения

### Интеграционные тесты (`SoftwareProjects.AppHost.Tests`)
Поднимают всю распределённую систему через `DistributedApplicationTestingBuilder` и проверяют:
1. **`Pipeline_GatewayRequest_PersistsGeneratedProjectToS3`** — запрос через гейтвей доходит
   до API, сгенерированный проект публикуется в SNS, файловый сервис сохраняет его в S3,
   содержимое файла совпадает с ответом API
2. **`Pipeline_MultipleRequests_AreAllListedInBucket`** — после нескольких запросов с
   разными идентификаторами в бакете присутствуют все ожидаемые ключи
3. **`Pipeline_RepeatedRequests_DoNotProduceDuplicateFiles`** — повторные запросы с тем же
   идентификатором обслуживаются из Redis-кэша и не создают дубликатов файлов в S3

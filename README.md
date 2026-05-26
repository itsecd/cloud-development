# Современные технологии разработки ПО

Проект микросервисного бекенда для генерации данных об учебных курсах с кэшированием и балансировкой нагрузки.

## Лабораторная работа 1. Кэширование

- Реализован сервис генерации учебных курсов на основе Bogus
- Реализовано кэширование с помощью `IDistributedCache` и Redis
- Реализовано структурное логирование сервиса генерации
- Настроена оркестрация .NET Aspire

### Предметная область — Учебный курс

| # | Характеристика | Тип данных |
|---|----------------|------------|
| 1 | Идентификатор в системе | `int` |
| 2 | Наименование курса | `string` |
| 3 | ФИО преподавателя | `string` |
| 4 | Дата начала | `DateOnly` |
| 5 | Дата окончания | `DateOnly` |
| 6 | Максимальное число студентов | `int` |
| 7 | Текущее число студентов | `int` |
| 8 | Выдача сертификата | `bool` |
| 9 | Стоимость | `decimal` |
| 10 | Рейтинг | `int` |

## Лабораторная работа 2. Балансировка нагрузки

- Настроена оркестрация на запуск 5 реплик сервиса генерации
- Реализован API Gateway на основе Ocelot
- Имплементирован алгоритм балансировки Weighted Random

### Алгоритм Weighted Random

Каждой реплике сервиса присваивается вероятность выбора (сумма вероятностей равна 1). При поступлении запроса реплика выбирается случайно с учётом назначенных весов.

## Лабораторная работа 3. Интеграционное тестирование

- В оркестрацию добавлено объектное хранилище (S3 в эмуляции LocalStack) и брокер сообщений (SQS в эмуляции LocalStack)
- Ресурсы (S3 bucket, SQS queue) объявлены через CloudFormation-шаблон и поднимаются через `AddAWSCloudFormationTemplate`
- Реализован файловый сервис `CourseApp.FileService`, потребляющий сообщения из SQS и сохраняющий курсы в S3 в виде JSON-файлов
- В `CourseApp.Api` добавлен SQS-продюсер: после генерации нового курса сообщение публикуется в очередь
- Реализованы интеграционные тесты на основе `Aspire.Hosting.Testing`, проверяющие сквозной пайплайн всех сервисов

### Вариант реализации

| Компонент | Технология |
|-----------|------------|
| Брокер сообщений | AWS SQS (в эмуляции LocalStack) |
| Объектное хранилище | AWS S3 (в эмуляции LocalStack) |
| AWS SDK конфигурация | LocalStack.Client + AWSSDK.SQS / AWSSDK.S3 |

### Интеграционные тесты

Проект `CourseApp.AppHost.Tests` поднимает весь `DistributedApplication` через `Aspire.Hosting.Testing` и прогоняет реальные HTTP-запросы по сквозному пайплайну. Один экземпляр приложения шарится между тестами через `IAsyncLifetime`, тесты используют разные `id`, чтобы не конфликтовать по ключам в S3.

| Тест | Что проверяет |
|------|---------------|
| `Pipeline_PutsGeneratedCourseToS3` | Сквозной happy-path: запрос к API кладёт сериализованный курс в S3 идентичным тому, что вернулся клиенту (`HTTP → CourseService → SQS → SqsConsumerService → S3`) |
| `Pipeline_CacheHitDoesNotDuplicateFile` | Идемпотентность: при cache hit отправка в SQS не происходит повторно — в S3 остаётся ровно один файл `course_{id}.json` |

## Лабораторная работа 4. Развёртывание в Yandex Cloud

Проект перенесён с локальной Aspire/LocalStack-инфраструктуры на управляемые сервисы Yandex Cloud. Используются Cloud Functions, очередь сообщений Yandex Message Queue и Object Storage.

### Развёрнутые компоненты

| Компонент | Реализация | Назначение |
|-----------|------------|------------|
| Клиент | `Client.Wasm` в Object Storage static website | Веб-интерфейс для запроса учебного курса по `id` |
| API Gateway | Yandex API Gateway `course-gateway` | Serverless Integration, проксирует `GET /courses?id={id}` в функцию генерации |
| Генератор курсов | `CourseApp.Api.YandexFunction` / `course-generator` | HTTP Cloud Function, генерирует курс и публикует JSON в очередь |
| Очередь сообщений | Yandex Message Queue `courses-the80hz` | Связывает генератор и обработчик файлов |
| Файловый обработчик | `CourseApp.FileService.YandexFunction` / `course-storage` | Cloud Function, вызывается триггером очереди и сохраняет JSON в бакет |
| Объектное хранилище данных | Object Storage `courses-storage-the80hz` | Хранит файлы `course_{id}.json` |

### Схема работы

1. Пользователь открывает статический Blazor WASM клиент из Object Storage.
2. Клиент отправляет запрос `GET /courses?id={id}` в Yandex API Gateway `course-gateway`.
3. API Gateway через Serverless Integration вызывает Cloud Function `course-generator`.
4. Функция генерирует объект учебного курса по `id` и возвращает JSON клиенту через API Gateway.
5. Она же публикует JSON в Yandex Message Queue.
6. Триггер Message Queue вызывает функцию `course-storage`.
7. `course-storage` сохраняет сообщение в Object Storage как `course_{id}.json`.

# Лабораторная работа №1 — «Кэширование»

## Вариант 47 — «Транспортное средство»

## Описание

Реализован микросервисный бэкенд для генерации данных о транспортных средствах с кэшированием в Redis и оркестрацией через .NET Aspire.

## Что реализовано

- Сервис генерации данных о транспортных средствах на основе Bogus
- Кэширование ответов при помощи `IDistributedCache` и Redis
- Структурное логирование сервиса генерации
- Оркестрация всех компонентов через .NET Aspire
- Blazor WebAssembly клиент для отображения данных

## Стек технологий

- .NET 8
- .NET Aspire 9.5
- Bogus — генерация данных
- Redis — распределённое кэширование
- RedisInsight — визуализация данных в Redis
- Blazor WebAssembly — клиентское приложение
- OpenTelemetry — метрики, трейсинг, логирование

## Структура проекта

| Проект | Описание |
|---|---|
| `VehicleApp.Api` | API-сервис генерации транспортных средств |
| `VehicleApp.AppHost` | Aspire-оркестратор |
| `VehicleApp.ServiceDefaults` | Общие настройки сервисов (OpenTelemetry, health checks) |
| `Client.Wasm` | Blazor WebAssembly клиент |

## Характеристики транспортного средства

| № | Название | Тип данных | Источник Bogus |
|---|---|---|---|
| 1 | Идентификатор в системе | `int` | Параметр запроса |
| 2 | VIN-номер | `string` | `Vehicle.Vin()` |
| 3 | Производитель | `string` | `Vehicle.Manufacturer()` |
| 4 | Модель | `string` | `Vehicle.Model()` |
| 5 | Год выпуска | `int` | `Random.Int(1980, текущий год)` |
| 6 | Тип корпуса | `string` | `Vehicle.Type()` |
| 7 | Тип топлива | `string` | `Vehicle.Fuel()` |
| 8 | Цвет корпуса | `string` | `Commerce.Color()` |
| 9 | Пробег (км) | `double` | `Random.Double(0, 300000)` |
| 10 | Дата последнего ТО | `DateOnly` | `Date.Between(год выпуска, сегодня)` |

## Скриншоты

### Aspire Dashboard
![Aspire Dashboard](images/aspire.png)

### Клиент
![Client](images/client.png)

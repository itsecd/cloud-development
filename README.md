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

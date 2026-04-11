# Cloud Development (вариант 14 «Медицинский пациент»)

> Студент: Дмитрий Степанов, группа 6511.

## Описание

- **GeneratorService** — ASP.NET Core API, генерирующий карточку пациента по идентификатору, кеширует результат в Redis.
- **API Gateway (Ocelot)** — маршрутизация и балансировка запросов.
- **Client (Blazor WASM)** — пользовательский интерфейс.
- **Redis** — кеш.
- **.NET Aspire AppHost** — оркестрация сервисов.

## Архитектура


Client (Blazor)
↓
API Gateway (Ocelot)
↓
GeneratorService (3 реплики)
↓
Redis

## REST API

### `GET /patient?id={int}`

| Параметр | Тип | Обязателен | Описание |
|----------|-----|------------|----------|
| `id`     | int (>0) | да | Одновременно идентификатор пациента и seed для генератора; проверяется на положительность. |

Ответ `200 OK` содержит JSON‑объект с полями:

| Поле          | Тип      | Пример                         | Описание                               |
|---------------|----------|--------------------------------|----------------------------------------|
| `id`          | int      | `42`                           | Первичный ключ записи.                 |
| `fullName`    | string   | `«Ирина Смирнова Сергеевна»`   | ФИО пациента на русском языке.        |
| `birthday`    | DateOnly | `1993-07-20`                   | Дата рождения.                         |
| `address`     | string   | `«г. Самара, ул. Карла Маркса...»` | Полный адрес.                      |
| `height`      | double   | `167.35`                       | Рост в сантиметрах.                    |
| `weight`      | double   | `61.12`                        | Вес в килограммах.                     |
| `bloodType`   | int      | `2`                            | Группа крови (1–4).                    |
| `resus`       | bool     | `true`                         | Фактор резус.                          |
| `lastVisit`   | DateOnly | `2026-02-17`                   | Дата последнего визита.                |
| `vactination` | bool     | `false`                        | Наличие прививки.                      |

Возможные статусы:

- `200 OK` — данные найдены (из кеша или заново сгенерированы).
- `400 BadRequest` — `id <= 0`.
- `500 Internal Server Error` — непредвиденная ошибка (возвращается `ProblemDetails`).

Повторный вызов с тем же `id` в течение TTL вернет данные быстрее и будет сопровождаться логом «Patient ... was found in cache».

## Структура репозитория

```
cloud-development/
├─ Client.Wasm/
│  ├─ Components/               
│  ├─ Layout/                   
│  ├─ Pages/Home.razor          
│  └─ wwwroot/appsettings.json                    # Конфигурация клиента
├─ GeneratorService/                              
│  ├─ Models/Patient.cs                           # Объектная модель
│  ├─ Services/{Generator,PatientService}.cs      # Сервис генерации и генератор
│  └─ Program.cs
├─ Patient/
│  ├─ Patient.AppHost/
│  └─ Patient.ServiceDefaults/
├─ .github/workflows/setup_pr.yml
├─ CloudDevelopment.sln
└─ LICENSE
```

## Балансировка нагрузки

Реализован кастомный балансировщик:

instanceIndex = id % N

где N — количество реплик сервиса.

## Кеширование
Используется Redis
Повторный запрос берётся из кеша
В логах:
Patient with id: X was found in cache

## Скрины работы
<details>
  Сервисы
<img width="1753" height="907" alt="Сервисы" src="https://github.com/user-attachments/assets/391f0f98-4179-4510-acff-cc7ab21dcbc9" />
</details>
<details>
![Ответ клиента](image-1.png)
</details>
<details>
  Трассировки
  <img width="1753" height="760" alt="Трассировки" src="https://github.com/user-attachments/assets/72deb2da-0acc-4a33-b193-ae64d472d2dd" />
</details>

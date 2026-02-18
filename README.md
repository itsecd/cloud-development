# Лабораторная работа №1 - "Кэширование"

**Вариант**: №9 - "Кредитная заявка"

**Выполнил**: Куненков Иван, группа 6511

**Предметная область**: Генерация кредитных заявок


## Реализованный функционал

### Основные возможности:
- **Генерация кредитных заявок** с реалистичными данными (Bogus)
- **Интеллектное кэширование** в Redis с TTL 10 минут
- **REST API** для получения заявок
- **Blazor WebAssembly клиент** для работы с API
- **Структурное логирование** через OpenTelemetry
- **Мониторинг в реальном времени** через Aspire Dashboard

## 🏗️ Архитектура

```
┌─────────────────────────────────────┐
│   Client.Wasm (Blazor WASM)         │  ← Пользовательский интерфейс
│   - Форма ввода ID                  │
│   - Отображение данных              │
└──────────────┬──────────────────────┘
               │ HTTPS + CORS
               ↓
┌─────────────────────────────────────┐
│   CreditApp.Api (ASP.NET Core)      │  ← REST API
│   GET /api/credit?id={id}           │
│   - Проверка кэша                   │
│   - Генерация (Bogus)               │
│   - Структурное логирование         │
└──────────────┬──────────────────────┘
               │ IDistributedCache
               ↓
┌─────────────────────────────────────┐
│   Redis (Docker)                    │  ← Кэш
│   TTL: 10 минут                     │
└─────────────────────────────────────┘
               ↑
    ┌──────────┴──────────┐
    │   Aspire AppHost    │  ← Оркестрация
    └─────────────────────┘
```

## 📁 Структура проекта

```
cloud-development/
├── CreditApp.AppHost/              # Aspire orchestrator
│   └── Program.cs                  # Конфигурация оркестрации
├── CreditApp.ServiceDefaults/      # Общие настройки
│   └── Extensions.cs               # OpenTelemetry, health checks
├── CreditApp.Api/                  # REST API
│   ├── Controllers/
│   │   └── CreditController.cs     # GET /api/credit?id={id}
│   ├── Services/
│   │   └── CreditGeneratorService/
│   │       ├── ICreditApplicationGeneratorService.cs
│   │       └── CreditApplicationGeneratorService.cs
│   └── Program.cs                  # Конфигурация (Redis, CORS, логирование)
├── CreditApp.Domain/               # Модели данных
│   └── Entities/
│       └── CreditApplication.cs
├── Client.Wasm/                    # Blazor WASM клиент
│   ├── Components/
│   │   ├── DataCard.razor          # UI для запроса заявок
│   │   └── StudentCard.razor       # Информация о студенте
│   └── wwwroot/
│       └── appsettings.json        # Конфигурация API endpoint
├── screenshots/                    # Скриншоты приложения
└── README.md                       # Этот файл
```



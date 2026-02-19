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

![aspire](https://github.com/user-attachments/assets/8eae0229-1476-43ce-92e9-7d00023edfa4)
![client](https://github.com/user-attachments/assets/78d9db61-05f4-4896-8e77-1e9cb79dcf67)
![logs](https://github.com/user-attachments/assets/eb133b16-da58-47b5-8f11-e74f656977dd)


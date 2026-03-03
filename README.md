# Лабораторная работа №2 - "Балансировка нагрузки"

**Вариант**: №9 - "Кредитная заявка"  
**Алгоритм балансировки**: Weighted Round Robin

**Выполнил**: Куненков Иван, группа 6511

**Предметная область**: Генерация кредитных заявок


## Реализованный функционал

### Основные возможности:
- **API Gateway** на базе Ocelot с балансировкой нагрузки
- **Weighted Round Robin** балансировщик с весами 5:3:2
- **3 реплики API сервиса** через Aspire orchestration
- **Генерация кредитных заявок** с реалистичными данными (Bogus)
- **Общий Redis кэш** для всех реплик с TTL 10 минут
- **Blazor WebAssembly клиент** для работы через Gateway
- **Структурное логирование** через OpenTelemetry
- **Мониторинг балансировки** в реальном времени через Aspire Dashboard

## 🏗️ Архитектура

```
┌──────────────────────────────────────┐
│  Client.Wasm (Blazor WebAssembly)   │  ← Пользовательский интерфейс
│  - Форма ввода ID                    │
│  - Отображение данных заявки         │
└───────────────┬──────────────────────┘
                │ HTTPS
                ↓
┌──────────────────────────────────────┐
│   CreditApp.ApiGateway (Ocelot)      │  ← API Gateway
│   - Weighted Round Robin (5:3:2)     │
│   - Маршрутизация запросов           │
│   - Структурное логирование          │
└───────────────┬──────────────────────┘
                │
        ┌───────┼───────────┐
        ↓       ↓           ↓
┌─────────┐ ┌─────────┐ ┌─────────┐
│ API-0   │ │ API-1   │ │ API-2   │  ← 3 реплики API
│ (вес 5) │ │ (вес 3) │ │ (вес 2) │
│ :7170   │ │ :7171   │ │ :7172   │
└────┬────┘ └────┬────┘ └────┬────┘
     │           │           │
     └───────────┴───────────┘
                 ↓
        ┌─────────────────┐
        │  Redis Cache    │  ← Общий кэш
        │  TTL: 10 минут  │
        │  + Commander    │
        └─────────────────┘
                 ↑
        ┌────────┴────────┐
        │ Aspire AppHost  │  ← Оркестрация
        │ + Dashboard     │
        └─────────────────┘
```

## 📁 Структура проекта

```
cloud-development/
├── CreditApp.AppHost/                    # 🎯 Aspire orchestrator
│   └── Program.cs                        # Конфигурация: 3 реплики API + Gateway
│
├── CreditApp.ApiGateway/                 # 🌐 API Gateway (Лаб. №2)
│   ├── LoadBalancing/
│   │   └── WeightedRoundRobinLoadBalancer.cs  # Алгоритм балансировки
│   ├── Program.cs                        # Ocelot + Service Discovery
│   ├── ocelot.json                       # Конфигурация маршрутов
│   └── appsettings.json                  # Имена сервисов и веса
│
├── CreditApp.Api/                        # 🔧 REST API (3 реплики)
│   ├── Controllers/
│   │   └── CreditController.cs           # GET /api/credit?id={id}
│   ├── Services/
│   │   └── CreditGeneratorService/
│   │       └── CreditApplicationGeneratorService.cs  # Генерация + кэш
│   └── Program.cs                        # Конфигурация (Redis, CORS, Swagger)
│
├── CreditApp.ServiceDefaults/            # ⚙️ Общие настройки
│   └── Extensions.cs                     # OpenTelemetry, health checks
│
├── CreditApp.Domain/                     # 📦 Модели данных
│   └── Entities/
│       └── CreditApplication.cs          # Модель кредитной заявки
│
├── Client.Wasm/                          # 💻 Blazor WASM клиент
│   ├── Components/
│   │   ├── DataCard.razor                # UI для запроса заявок
│   │   └── StudentCard.razor             # Информация о студенте
│   └── wwwroot/
│       └── appsettings.json              # Адрес Gateway
└── 📄 README.md                          # Этот файл
```


## 📸 Скриншоты

![aspire](https://github.com/user-attachments/assets/8eae0229-1476-43ce-92e9-7d00023edfa4)
![client](https://github.com/user-attachments/assets/78d9db61-05f4-4896-8e77-1e9cb79dcf67)
![logs](https://github.com/user-attachments/assets/eb133b16-da58-47b5-8f11-e74f656977dd)

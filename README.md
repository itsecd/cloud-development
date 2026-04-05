## Описание проекта

Проект представляет собой распределённую систему для получения информации о сотрудниках с использованием кэширования Redis и балансировки нагрузки по алгоритму Query Based.

## Архитектура проекта

Решение состоит из нескольких проектов:

- **Employee.ApiService** – Web API сервис
- **Employee.ApiGateway** – API Gateway на базе Ocelot
- **Employee.AppHost** – Aspire orchestrator
- **Employee.ServiceDefaults** – общие настройки сервисов
- **Client.Wasm** – клиент

## Основная логика работы

1. Клиент отправляет запрос в API Gateway (`/api/employee?id={id}`).
2. API Gateway (Ocelot) принимает запрос и передаёт его в один из сервисов генерации.
3. Выбор сервиса осуществляется с помощью кастомного балансировщика `QueryBasedLoadBalancer`.
4. Сервис:
   - проверяет наличие данных в Redis,
   - если данные есть — возвращает их из кэша,
   - если нет — генерирует нового сотрудника и сохраняет его в кэш.
  
## Оркестрация сервисов

С помощью Aspire настроен запуск нескольких реплик сервиса генерации:

- generator-1 → http://localhost:5201  
- generator-2 → http://localhost:5202  
- generator-3 → http://localhost:5203  

## Запуск проекта

1. Запустить проект **Employee.AppHost**.
2. Aspire Dashboard откроется автоматически.
3. В Dashboard будут запущены:
   - Redis
   - Redis Commander
   - 3 реплики Employee.ApiService
   - API Gateway
   - WebFrontend
   - 
## Пример работы приложения
<img width="1904" height="913" alt="image" src="https://github.com/user-attachments/assets/3c315a88-cd29-4184-8412-289d66aa3bf7" />
<img width="1849" height="710" alt="image" src="https://github.com/user-attachments/assets/ecf07da2-9cdf-4ad3-a2b3-6802464702c9" />



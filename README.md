# Современные технологии разработки программного обеспечения
### Вариант 13 Медицинский пациент

### Лабораторная работа №1
<details>
<summary>1.	«Кэширование» - Реализация сервиса генерации контрактов, кэширование его ответов</summary>
<br> 
  
В рамках первой лабораторной работы необходимо:
* Реализовать сервис генерации контрактов на основе Bogus,
* Реализовать кеширование при помощи IDistributedCache и Redis,
* Реализовать структурное логирование сервиса генерации,
* Настроить оркестрацию Aspire. 
  
</details>

Реализовано:
1. Доменная модель: *Domain*
2. Сервис генерации данных пациента с заданным идентификатором: *GenerationService*
3. Сервис кэширования Redis: *CachingService*
4. Сервис оркестрации Aspire: *AppHost.AppHost*
5. Сервис телеметрии OpenTelemetry: *AppHost.ServiceDefaults*

Примеры интерфейса:

![1](https://github.com/user-attachments/assets/bc050486-c80b-4a8b-87e2-6fd0b510a68f)

![2](https://github.com/user-attachments/assets/c8bdd235-9656-476f-ae93-f8a9e7a75bd2)

![3](https://github.com/user-attachments/assets/bb429a0c-7553-4c5c-b863-4bacf5c5b67a)



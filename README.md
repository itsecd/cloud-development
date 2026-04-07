# Современные технологии разработки программного обеспечения

### Вариант №6:
- Доменная область: Медицинский пациент
- Балансировка: Weighted Round Robin
- Брокер: SQS
- Хостинг S3: Localstack


## Лабораторная работа №2 - Балансировка нагрузки

В рамках второй лабораторной работы необходимо:
* Настроить оркестрацию на запуск нескольких реплик сервиса генерации,
* Реализовать апи гейтвей на основе Ocelot,
* Имплементировать алгоритм балансировки нагрузки согласно варианту.
  
### Реализовано:
- Добавлен API Gateway на основе Ocelot, настроен конфиг файл
- Реализован класс балансироващика WeightedRoundRobin
- бновлены конфиг-файлы проекта для конкретного запуска, проверена работа балансировщика

### Скриншоты работащего приложения
<details>
  <summary>
    Поднятые контейнеры
  </summary>

  <img width="2560" height="1440" alt="image" src="https://github.com/user-attachments/assets/44d0ff7e-386a-4edf-b2da-35af965220f1" />

   </br>Граф связей в проекте:</br>

  <img width="719" height="650" alt="image" src="https://github.com/user-attachments/assets/064f7b83-0d2b-43d3-a356-78fb1ab5b4e6" />

</details>

<details>
  <summary>
    Логи запуска генераторов
  </summary>

    <img width="2560" height="1440" alt="image" src="https://github.com/user-attachments/assets/67ccbc95-16d1-4406-b412-ebea48661e55" />

    <img width="2560" height="1440" alt="image" src="https://github.com/user-attachments/assets/4a79997b-0629-438d-9d91-f4a31d87a9cd" />

</details>

<details>
  <summary>
    Демонстрация балансировки
  </summary>

   <img width="2560" height="1435" alt="image" src="https://github.com/user-attachments/assets/a1cd3e1c-46da-47a1-bac4-edfc4e801c69" />

   <img width="2560" height="1428" alt="image" src="https://github.com/user-attachments/assets/08d9e1ea-3ea9-47ae-97df-7645ddf4a6e2" />

   <img width="2560" height="1436" alt="image" src="https://github.com/user-attachments/assets/21847f03-e0a8-473a-abf9-3cc53527352b" />

</details>

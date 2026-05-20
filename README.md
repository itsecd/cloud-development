# Современные технологии разработки программного обеспечения

### Вариант №6:
- Доменная область: Медицинский пациент
- Балансировка: Weighted Round Robin
- Брокер: SQS
- Хостинг S3: Localstack


## Лабораторная работа №3 - Интеграционное тестирование

Реализация файлового сервиса и объектного хранилища, интеграционное тестирование бекенда

В рамках третьей лабораторной работы необходимо:
* Добавить в оркестрацию объектное хранилище,
* Реализовать файловый сервис, сериализующий сгенерированные данные в файлы и сохраняющий их в объектном хранилище,
* Реализовать отправку генерируемых данных в файловый сервис посредством брокера,
* Реализовать интеграционные тесты, проверяющие корректность работы всех сервисов бекенда вместе.
  
### Реализовано:
- Добавлена оркестрация в объектное хранилище Localstack через брокер сообщений SQS.
- Реализованы 5 интеграционных тестов, проверяющих корректную работу приложения.

### Скриншоты работащего приложения
<details>
  <summary>
    Поднятые контейнеры
  </summary>

  <img width="2560" height="1440" alt="image" src="https://github.com/user-attachments/assets/53c838b0-9c93-4520-b3fc-b4d3e1459414" />

   </br>Граф связей в проекте:</br>

  <img width="2560" height="1440" alt="image" src="https://github.com/user-attachments/assets/1a1145aa-2dee-4ebd-9127-3d9bc79ab020" />


</details>

<details>
  <summary>
    Проверка работы localstack
  </summary>
  
  <img width="1100" height="276" alt="image" src="https://github.com/user-attachments/assets/8ff24f72-ce17-4a75-8f5a-64a655eb13cd" />

  <img width="2560" height="1439" alt="image" src="https://github.com/user-attachments/assets/9c4e367f-29c8-4968-9ff0-456bcc52e4f4" />

</details>

<details>
  <summary>
    Проверка работы тестов
  </summary>

   <img width="1231" height="951" alt="image" src="https://github.com/user-attachments/assets/d8d90aba-777a-4dab-8f6b-aa7cfaec9127" />

</details>

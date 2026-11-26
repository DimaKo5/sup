# Спецификация REST API

Локальная точка входа: `http://localhost:5006`. Формат запросов и обычных ответов — JSON UTF-8. Авторизованные запросы передают `Authorization: Bearer <token>`. Примеры повторного запуска находятся в `CollegeSupport.Api/CollegeSupport.Api.http`.

| Метод | Маршрут | Доступ | Результат |
|---|---|---|---|
| GET | /health | Все | 200, проверка базы |
| POST | /api/auth/login | Все | 200 и JWT либо 401 |
| GET | /api/appeals | Все роли | 200, фильтры status, technicianId, search |
| GET | /api/appeals/{id} | Все роли | 200, карточка |
| POST | /api/appeals | Администратор, диспетчер | 201, новое обращение |
| PUT | /api/appeals/{id}/status | Все роли | 200, переход состояния |
| PUT | /api/appeals/{id}/technician | Администратор, диспетчер | 200, назначение |
| GET | /api/appeals/statistics | Все роли | 200, сводные показатели |
| GET | /api/employees | Все роли | 200, справочник |
| GET | /api/employees/{id} | Все роли | 200, сотрудник |
| POST | /api/employees | Администратор, диспетчер | 201, сотрудник |
| GET | /api/users | Администратор | 200, список пользователей |
| GET | /api/users/technicians | Администратор, диспетчер | 200, список техников |
| POST | /api/users | Администратор | 201, пользователь |
| POST | /api/exchange/employees/import | Администратор, диспетчер | 200, счётчики и ошибки по номерам строк |
| GET | /api/exchange/appeals/export | Администратор, диспетчер | 200, файл CSV, JSON или XML |

Для техника список, карточка, переходы и статистика ограничены его обращениями. Пример ошибки: `PUT /api/appeals/6/status` с телом `{"status":"InProgress"}` для обращения без техника возвращает 400 и объект ProblemDetails со значениями `status`, `title`, `detail` и `instance`.

Импорт принимает `{"content":"ФИО;Телефон;Email;Кабинет\nТестовый Сотрудник;+79001234567;;Б-13"}`. Заголовок и разделитель показаны в `sample-employees.csv`. Экспорт JSON содержит массив записей экспорта, даты передаются в ISO 8601 с указанием UTC.

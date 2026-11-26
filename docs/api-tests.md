# Работа 10. Тестирование API

Запросы — в файле `CollegeSupport.Api/CollegeSupport.Api.http`.

| № | Запрос | Ожидаемый код | Фактический код | Вывод |
|---|---|---|---|---|
| 0 | POST /api/auth/login | 200 | 200 | JWT получен |
| 1 | GET /api/appeals | 200 | 200 | 7 обращений |
| 2 | GET /api/appeals?status=InProgress | 200 | 200 | Только «В работе» |
| 3 | POST /api/employees | 201 | 201 | Телефон нормализован |
| 4 | POST /api/employees (невалидно) | 400 | 400 | Словарь errors |
| 5 | POST /api/appeals | 201 | 201 | Обращение создано |
| 6 | PUT /api/appeals/6/technician | 200 | 200 | Техник назначен |
| 7 | PUT /api/appeals/6/status | 200 | 200 | Статус изменён |
| 8 | PUT /api/appeals/6/status (Closed) | 400 | 400 | Недопустимый переход |
| 9 | GET /api/appeals/999 | 404 | 404 | ProblemDetails |
| 10 | POST /api/exchange/employees/import | 200 | 200 | 1 импортирован, 3 пропущены |
| 11 | GET /api/exchange/appeals/export?format=xml | 200 | 200 | Файл XML |
| 12 | GET /api/exchange/appeals/export?format=pdf | 400 | 400 | Неподдерживаемый формат |
| 13 | GET /api/appeals/statistics | 200 | 200 | 7 / 3 / 3 / 10000 |
| 14 | GET /health | 200 | 200 | Healthy |

Дефект из работы 10 (500 вместо 400/404) устранён глобальным обработчиком `GlobalExceptionHandler` (работа 11).

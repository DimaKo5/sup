# Работа 3. Артефакты и протоколы

## Артефакты проекта

| Артефакт | Место | Назначение |
|---|---|---|
| Исходный код решения | Корень решения | Шесть проектов |
| База SQLite | `collegesupport.db` | Данные приложения (не коммитится) |
| Миграции | `CollegeSupport.Data/Migrations` | Схема БД |
| Журнал API | `CollegeSupport.Api/logs/api-*.log` | Serilog, 14 дней |
| Журнал клиента | `%LOCALAPPDATA%\CollegeSupport\logs` | FileAppLogger, 14 дней |
| Выгрузки | CSV / JSON / XML | Обмен с внешними системами |
| Документация | `docs/` | Требования, архитектура, тесты, отчёты |

## Протокол REST API

Базовый адрес: `http://localhost:5001/`. Формат JSON: camelCase, перечисления строками (`JsonDefaults`).

| Метод | Маршрут | Назначение |
|---|---|---|
| POST | `/api/auth/login` | Вход, JWT |
| GET | `/api/appeals` | Список обращений (`status`, `technicianId`, `search`) |
| GET | `/api/appeals/{id}` | Обращение по номеру |
| POST | `/api/appeals` | Создание обращения |
| PUT | `/api/appeals/{id}/status` | Смена статуса |
| PUT | `/api/appeals/{id}/technician` | Назначение техника |
| GET | `/api/appeals/statistics` | Статистика |
| GET | `/api/employees` | Сотрудники (`search`) |
| GET | `/api/employees/{id}` | Сотрудник по номеру |
| POST | `/api/employees` | Создание сотрудника |
| GET | `/api/users` | Пользователи |
| GET | `/api/users/technicians` | Техники |
| POST | `/api/users` | Создание пользователя |
| POST | `/api/exchange/employees/import` | Импорт сотрудников из CSV |
| GET | `/api/exchange/appeals/export?format=csv\|json\|xml` | Выгрузка обращений |
| GET | `/health` | Проверка работоспособности |

## Форматы обмена

Импорт: `ФИО;Телефон;Email;Кабинет` (UTF-8 или Windows-1251).
Экспорт CSV: `Номер;Дата;Сотрудник;Телефон;Кабинет;Оборудование;Статус;Техник;Стоимость` (BOM + InvariantCulture).

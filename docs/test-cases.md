# Работа 23. Тест-кейсы и матрица трассировки

| ID | Требование | Предусловие | Шаги | Ожидаемый результат |
|---|---|---|---|---|
| TC-01 | ФТ-01 | Вход dispatcher | Создать обращение | 201, статус «Новое» |
| TC-02 | ФТ-02 | Есть 7 обращений | Фильтр «В работе» | Только нужные строки |
| TC-03 | ФТ-04 | Обращение «Новое» | Переход в «Решено» | 400, недопустимый переход |
| TC-04 | ФТ-04 | Назначен техник | «В работе» → «Решено» со стоимостью | 200, CompletedAt заполнен |
| TC-05 | ФТ-05 | Техник активен | Назначить техника | 200 |
| TC-06 | ФТ-06 | Телефон 8 900… | Создать сотрудника | +7900… |
| TC-07 | ФТ-07 | Вход technician2 | Сменить статус обращения техника1 | 403 |
| TC-08 | ФТ-08 | Неверный пароль | Вход | 401 |
| TC-09 | ФТ-09 | 7 демонстрационных записей | GET statistics | 7 / 3 / 3 / 10000 |
| TC-10 | ФТ-10 | CSV с 4 строками | Импорт | 1 импортирован, 3 ошибки |
| TC-11 | ФТ-11 | Есть обращения | Export csv | BOM и кириллица |
| TC-12 | ФТ-12 | Без токена | GET /api/appeals | 401 |
| TC-13 | ФТ-11 | Есть обращения | Экспорт JSON | Файл читается как массив обращений |
| TC-14 | ФТ-11 | Есть обращения | Экспорт XML | Элементы Appeal под корнем Appeals |
| TC-15 | ФТ-07 | Два техника с обращениями | Вход под каждым техником и открытие списка | Каждый получает только свои записи |

## Матрица трассировки

| Требование | Модульные тесты | Интеграционные тесты | UI-тесты |
|---|---|---|---|
| ФТ-01 | AppealServiceTests | CreateAppeals | — |
| ФТ-02 | — | GetAppeals_WithStatusFilter | MainWindow_SearchWithoutMatchShowsZeroRows |
| ФТ-04 | AppealStatusRulesTests, AppealServiceTests | ChangeStatus_* | — |
| ФТ-06 | PhoneNormalizerTests | CreateEmployee_* | — |
| ФТ-07 | AppealServiceTests | Technician_Cannot* | — |
| ФТ-08 | PasswordSecurityTests | Login_* | Login_WithValidCredentials |
| ФТ-09 | AppealStatisticsCalculatorTests | GetStatistics_ReturnsSeededValues | — |
| ФТ-10 | ExchangeAndLoggingTests | ExportAppeals_ReturnsCsvFile | — |
| ФТ-12 | — | GetAppeals_WithoutToken | — |

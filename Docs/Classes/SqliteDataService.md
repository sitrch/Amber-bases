# SqliteDataService

**Модуль:** Services  
**Расположение:** `AmberBases/Services/SqliteDataService.cs`

## Назначение
Класс `SqliteDataService` реализует интерфейс `ISqliteDataService` и отвечает за сохранение объекта `System.Data.DataSet` в локальную базу данных SQLite.
В целях простоты (по заданию) связи между таблицами не настраиваются, создаются простые таблицы с текстовыми полями.

## Методы
- `void SaveDataSet(DataSet dataSet, string dbFilePath)`:
  Главный публичный метод. Создает новый файл БД по пути `dbFilePath` (или перезаписывает старый). Открывает соединение с БД и в рамках одной транзакции обходит все таблицы в `DataSet`, вызывая для каждой создание таблицы и вставку данных.
- `void CreateTable(SQLiteConnection connection, DataTable table)` (private):
  Генерирует и выполняет SQL-запрос `CREATE TABLE IF NOT EXISTS`. Все столбцы создаются с типом `TEXT`. Имена таблиц и столбцов берутся из `DataTable`.
- `void InsertData(SQLiteConnection connection, DataTable table)` (private):
  Генерирует параметризованный SQL-запрос `INSERT INTO`. Использует `SQLiteParameter` для защиты от SQL-инъекций и корректной обработки спецсимволов. Выполняет вставку всех строк из `DataTable`.

## Зависимости
- `System.Data.SQLite.Core` (для работы с SQLite).
- `System.Data` (для DataSet и DataTable).
- Реализует `ISqliteDataService`.

# SqliteDictionaryDataService

**Модуль:** Services  
**Расположение:** `AmberBases/Services/SqliteDictionaryDataService.cs`

## Назначение
Реализация интерфейса `IDictionaryDataService` для работы с базой данных справочников SQLite (`AmberDictionaries.sqlite`). Сервис отвечает за создание структуры таблиц (DDL), выполнение миграций и CRUD-операций с моделями из `AmberBases.Core.Models.Dictionaries`.

## Логика работы
- **Инициализация**: Метод `InitializeDatabase` создает таблицы с правильными колонками на основе рефлексии моделей, а затем добавляет недостающие колонки через `ALTER TABLE` для существующих таблиц.
- **Миграции**: Для обратной совместимости добавлены миграции:
  - `SystemProviders`: колонка `Information`
  - `ProfileSystems`: колонка `Description`
  - `Colors`: колонки `ColorName`, `RAL`, `CoatingType`
- **CRUD-операции**: Предоставляются обобщенные методы для получения всех записей, добавления, обновления и удаления сущностей.

## Ключевые методы
- `InitializeDatabase()`: Создание файла БД (если нет), таблиц и применение миграций.
- `GetItems<T>()`: Обобщенное получение всех записей.
- `AddItem<T>()`, `UpdateItem<T>()`, `DeleteItem<T>()`: Обобщенные методы модификации данных.

## Зависимости
- `System.Data.SQLite.Core` (ADO.NET провайдер)
- `AmberBases.Core.DatabaseConfig`
- `AmberBases.Core.Models.Dictionaries.*` (Модели данных)
- `AmberBases.Core.Builders.SqlBuilder<T>` (Генератор SQL)
- `AmberBases.Services.IDictionaryDataService` (Интерфейс)
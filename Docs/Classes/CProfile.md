# Класс CProfile

## Общая информация
- **Полное имя:** `AmberBases.Core.Models.Dictionaries.CProfile`
- **Наследование:** `BaseDictionaryModel`
- **Описание:** Представляет справочник C-профилей с полной интеграцией в систему справочников

## Структура таблицы в базе данных
Таблица: `Profiles`

| Колонка | Тип | Описание | Связи |
|---------|-----|----------|-------|
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | Уникальный идентификатор | - |
| ArticleId | INTEGER | Идентификатор артикула профиля | FK → ProfileArticles.Id |
| Title | TEXT | Название профиля | - |
| Description | TEXT | Описание профиля | - |
| StandartBarLengthId | INTEGER | Идентификатор стандартной длины прутка | FK → StandartBarLengths.Id |
| CustomBarLength | DOUBLE | Пользовательская длина прутка (если не используется стандартная) | - |
| ProfileTypeId | INTEGER | Идентификатор типа профиля | FK → ProfileTypes.Id |

## Свойства класса

### Основные свойства (включая навигационные)
```csharp
public int ArticleId { get; set; }
public ProfileArticle Article { get; set; }
public string Title { get; set; }
public string Description { get; set; }
public int StandartBarLengthId { get; set; }
public StandartBarLength StandartBarLength { get; set; }
public double CustomBarLength { get; set; }
public int ProfileTypeId { get; set; }
public ProfileType ProfileType { get; set; }
```

**Примечание:** Навигационные свойства (Article, StandartBarLength, ProfileType) расположены в регионе "Основные свойства" вместе с остальными свойствами класса.

## Методы работы с данными
Интерфейс `IDictionaryDataService` включает следующие методы для работы с CProfile:

```csharp
IEnumerable<CProfile> GetCProfiles(string dbPath);
void AddCProfile(CProfile item, string dbPath);
void UpdateCProfile(CProfile item, string dbPath);
void DeleteCProfile(int id, string dbPath);
```

## Интеграция с UI
### DictionaryEditorView
- Добавлена кнопка "C-Профили" в панель выбора справочников
- Добавлен контрол `CProfilesGrid` для отображения таблицы
- Коллекция `_cProfiles` для хранения данных в памяти
- Методы синхронизации `SyncCProfiles()` для сохранения изменений в БД

### Особенности отображения
- Фильтрация по полю `Title`
- Автоматическая генерация столбцов через рефлексию (через DictionaryTableControl)
- Поддержка внешних ключей через lookup-коллекции

## Взаимодействие с другими справочниками
CProfile связан с несколькими справочниками через внешние ключи:

1. **ProfileArticles** (`ArticleId`) - артикулы профилей
2. **StandartBarLengths** (`StandartBarLengthId`) - стандартные длины прутков
3. **ProfileTypes** (`ProfileTypeId`) - типы профилей

## Контекстное меню в таблице
Контрол `CProfilesGrid` использует стандартное контекстное меню `DictionaryTableControl`:

1. **Добавить строку** - создаёт новый экземпляр CProfile через Activator.CreateInstance
2. **Редактировать** - открывает ModelEditorWindow для детального редактирования
3. **Удалить строку** - удаляет выделенные строки с подтверждением
4. **Копировать** - копирует выделенные ячейки в TSV-формате
5. **Вставить из Excel** - парсит данные из буфера и вставляет в таблицу

## Миграции
Создана миграция: `20260412_0043_CreateProfilesTable.sql`
- Создание таблицы Profiles
- Добавление внешних ключей и индексов
- Поддержка UP/DOWN операций для отката

## Использование в приложении
Справочник CProfile доступен через:
- Основное окно справочников (DictionaryEditorView)
- Отдельные окна TableViewWindow для родительских таблиц
- API сервисов данных для интеграции с другими модулями
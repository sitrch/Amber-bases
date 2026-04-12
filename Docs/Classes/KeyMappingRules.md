# Класс KeyMappingRules

## Общая информация
- **Полное имя:** `AmberBases.Dataset.KeyMappingRules`
- **Описание:** Класс для работы с правилами маппинга таблиц из Excel-файлов

## Методы

### GetMappingRules
Возвращает таблицу "Плоскости" с правилами маппинга.

```csharp
public static DataTable GetMappingRules(IEnumerable<string> loadedTableNames, DataTable floorTable = null)
```

#### Параметры
| Параметр | Тип | Описание |
|----------|-----|----------|
| `loadedTableNames` | `IEnumerable<string>` | Список имен загруженных таблиц |
| `floorTable` | `DataTable` | Таблица "Плоскости" (опционально) |

#### Возвращаемое значение
- `DataTable` — таблица "Плоскости" с правилами маппинга

### GetРядыРигелейForPlane
Получает таблицу "РядыРигелей" для указанной плоскости из DataSet.

```csharp
public static DataTable GetРядыРигелейForPlane(DataSet dataSet, string плоскость)
```

#### Параметры
| Параметр | Тип | Описание |
|----------|-----|----------|
| `dataSet` | `DataSet` | DataSet с загруженными данными из Excel |
| `плоскость` | `string` | Идентификатор плоскости (например, "(5-1)-(5-6)") |

#### Возвращаемое значение
- `DataTable` — таблица `РядыРигелей{плоскость}` или `null`, если таблица не найдена

#### Примечания
- Метод формирует имя таблицы как `РядыРигелей{плоскость}`
- Используется в связке с `DataExtractor.GetFloor` для извлечения данных о рядах ригелей

## Пример использования
```csharp
// Загружаем данные из Excel
var excelDataService = new ExcelDataService();
var dataSet = excelDataService.LoadData("Bases/_Шаблоны(5-1)-(5-6).xlsx");

// Получаем таблицу для конкретной плоскости
var table = KeyMappingRules.GetРядыРигелейForPlane(dataSet, "(5-1)-(5-6)");

if (table != null)
{
    // Используем таблицу в DataExtractor
    var floor = DataExtractor.GetFloor(dataSet, "(5-1)-(5-6)", 1);
}
```

## Связанные классы
- `DataExtractor` — класс для извлечения данных из таблиц
- `CFloor` — структура данных о рядах ригелей
# EditActionTracker

**Модуль:** UI/Tracking  
**Расположение:** `AmberBases/UI/Tracking/EditActionTracker.cs`

## Назначение
Класс для отслеживания изменений в коллекциях справочников на основе DataTable с поддержкой Undo/Redo. Создаёт копию таблицы при инициализации и автоматически фиксирует все изменения.

## Архитектура

### Основные поля
| Поле | Тип | Описание |
|------|-----|----------|
| `_sourceCollection` | `IList` | Исходная коллекция (ссылка) |
| `_entityType` | `Type` | Тип сущности коллекции |
| `_entityProperties` | `PropertyInfo[]` | Свойства типа для отслеживания |
| `_notifyCollection` | `INotifyCollectionChanged` | Подписка на изменения коллекции |
| `_trackingTable` | `DataTable` | Таблица-копия с доп. полями |
| `_currentTimestamp` | `long?` | Текущая позиция в истории (null = конец) |
| `_hasUnsavedChanges` | `bool` | Флаг несохранённых изменений |
| `_preUndoSnapshot` | `IList` | Сохранённое состояние перед undo |

### Структура TrackingTable
Все свойства модели + `Timestamp` (long, миллисекунды) + `Deleted` (bool).

## Логика работы

### Initialize()
1. Создаёт DataTable с колонками свойств + Timestamp + Deleted
2. Заполняет данными из source (timestamp = 0)
3. Подписывается на CollectionChanged (авто-отслеживание)

### DetectAndRecordChanges()
Вызывается автоматически при изменении коллекции:
1. Если был undo — вызывает PruneBranch() (обрезает ветвь)
2. Сравнивает source с _trackingTable по ID
3. Новые строки → добавляет с текущим timestamp
4. Удалённые → отмечает Deleted = true
5. Изменённые → обновляет данные и timestamp

### Undo()
1. Если `_hasUnsavedChanges` — сохраняет текущее состояние
2. Ищет предыдущий timestamp в таблице
3. Восстанавливает `_sourceCollection` из состояния на момент timestamp
4. Удаляет "будущие" строки из таблицы

### Redo()
1. Ищет следующий timestamp в таблице
2. Восстанавливает `_sourceCollection` из состояния на момент timestamp

### PruneBranch()
Вызывается при новом редактировании после undo:
- Удаляет все строки с timestamp > текущего
- Восстанавливает snapshot (если был сохранён)

## Интеграция с DictionaryTableControl

```csharp
// Инициализация
_actionTracker = new EditActionTracker(collection, entityType);
_actionTracker.Initialize();
MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;

// Автоматическое отслеживание (через CollectionChanged)
// или явно:
_actionTracker.DetectAndRecordChanges();

// Undo/Redo
_actionTracker.Undo();
_actionTracker.Redo();
```

## События
- `StateChanged(bool canUndo, bool canRedo)` — при изменении состояния Undo/Redo

## DeepClone метод
Метод `DeepClone(object source)` использует `_entityType` для создания экземпляра, а не `source.GetType()`. Это необходимо, потому что `source` может быть `DataRowView` (из `TrackingTable.DefaultView`), когда DataGrid привязан к отслеживающей таблице.

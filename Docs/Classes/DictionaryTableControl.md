# DictionaryTableControl

**Модуль:** UI  
**Расположение:** `AmberBases/UI/DictionaryTableControl.xaml` и `AmberBases/UI/DictionaryTableControl.xaml.cs`

## Назначение
Универсальный UserControl для отображения таблицы любого справочника с поддержкой:
- Динамической генерации столбцов по типу сущности
- **Полноценного редактирования через контекстное меню** (Добавить/Удалить строку, Копировать, Вставить из Excel, Редактировать, Открыть таблицу родительской)
- Поиска/фильтрации по тексту
- Навигации к родительским таблицам через callback
- Множественного выделения ячеек (SelectionMode.Extended, SelectionUnit.CellOrRowHeader)

## Логика работы
- Принимает тип сущности, коллекцию данных, словарь всех коллекций (для FK lookup) и callback для открытия родительских таблиц.
- При инициализации динамически генерирует столбцы `DataGrid` через рефлексию.
- Foreign Key поля отображаются через `DataGridComboBoxColumn` с lookup-данными.
- Все ячейки являются редактируемыми (BindingMode.TwoWay).
- Поддерживает вложенную навигацию — при клике на FK ячейку вызывает callback для открытия новой `TableViewWindow`.
- **Поддерживает копирование/вставку диапазонов ячеек в формате TSV (табуляция как разделитель столбцов)** для совместимости с Excel.

### FK-поля (внешние ключи)
- Свойства с суффиксом `Id` автоматически отображаются как `DataGridComboBoxColumn` с lookup-данными из словаря `allCollections`.
- **Важное исправление (12.04.2026)**: В методе `SetupColumns()` была убрана строка `if (prop.Name.EndsWith("Id")) continue;`, которая мешала созданию выпадающих списков для FK-полей. Теперь FK-поля правильно определяются через метод `IsForeignKey()` и создаются как `DataGridComboBoxColumn`.
- Поддерживаются следующие FK-поля:
  - `SystemProviderId`
  - `ProfileSystemId` (также `SystemId` для ProfileArticle)
  - `ProfileTypeId`
  - `ApplicabilityId`
  - `CoatingTypeId`
  - `StandartBarLengthId`
  - `ColorId` (для ProfileArticle)
  - `ArticleId` (связь с таблицей ProfileArticle)
- Все связанные сущности (Color, StandartBarLength, ProfileType, ProfileSystem) имеют свойство `Name` (или вычисляемое свойство `Name`), которое используется в `DisplayMemberPath` для отображения понятных значений в выпадающих списках.

### Порядок столбцов
- Поле `ArticleId` (если есть) всегда отображается первым в таблице.
- Остальные столбцы добавляются в порядке объявления свойств в модели.
- **Логика создания столбцов**:
  1. Сначала проверяется наличие свойства `ArticleId` и создается ComboBoxColumn для него
  2. Затем для каждого свойства модели:
     - Пропускаются технические поля (`Id`, `Position`, `Info`, `ArticleId`)
     - Пропускаются коллекции и навигационные свойства
     - Если свойство является FK (заканчивается на "Id" и есть соответствующая lookup-коллекция) - создается `DataGridComboBoxColumn`
     - Иначе, если свойство не заканчивается на "Id" - создается `DataGridTextColumn`

## Горячие клавиши (Keyboard Shortcuts)

| Комбинация | Действие |
|------------|----------|
| `Ctrl+Z` | **Отменить последнее действие (Undo)** |
| `Ctrl+Y` | **Повторить отменённое действие (Redo)** |
| `Ctrl+C` | Копировать выделенные ячейки в буфер (TSV формат) |
| `Ctrl+V` | Вставить данные из буфера (Excel/TSV) |
| `Ctrl+D` | Дублировать выделенную строку (копия без ID) |
| `Enter` | Открыть ModelEditorWindow для выбранной строки |

> Горячие клавиши НЕ работают в режиме редактирования ячейки (когда фокус на TextBox/ComboBox внутри ячейки).

## Контекстное меню (правый клик)

| Пункт | Описание |
|-------|----------|
| **Добавить строку** | Создаёт новый экземпляр сущности через `Activator.CreateInstance` и добавляет в коллекцию |
| **Редактировать** | Открывает `ModelEditorWindow` для выбранной строки |
| **Удалить строку** | Удаляет выделенные строки из коллекции (с подтверждением) |
| **Копировать** | Копирует выделенные ячейки в буфер обмена в TSV-формате |
| **Вставить из Excel** | Вставляет данные из буфера (TSV-парсинг), обновляет существующие строки или добавляет новые |
| **Открыть таблицу "[Родительская]"** | Динамически добавляется при клике на FK-ячейку |

## Ключевые методы

### Инициализация и настройка
- `Initialize(Type entityType, IList collection, Dictionary<Type, IEnumerable> allCollections, Action<object, Type> openParentCallback)`: Инициализация контрола.
- `SetupColumns()`: Генерирует столбцы DataGrid на основе свойств модели (DataGridTextColumn или DataGridComboBoxColumn для FK). Исключает навигационные свойства (Manufacturer, System, Color, StandartBarLength, ProfileType, Provider, CoatingType).
- `AddComboBoxColumn(PropertyInfo prop)`: Вспомогательный метод для добавления FK-столбца с выпадающим списком.
- `ExcludedNavigationProperties`: HashSet имён навигационных свойств, которые не отображаются как колонки.
- `IsForeignKey(PropertyInfo prop)`: Проверяет, является ли свойство FK (заканчивается на "Id").
- `GetParentEntityType(PropertyInfo prop)`: Определяет тип родительской сущности по FK свойству. Включает маппинг для `ArticleId`, `StandartBarLengthId` и других FK.
- `GetCollectionForType(Type type)`: Возвращает lookup-коллекцию из словаря.
- `OpenParentTableFromCell(DataGrid dataGrid, string propName)`: Вызывает callback для открытия родительской таблицы.
- `ApplyFilter()`: Фильтрует строки по тексту поиска через `CollectionViewSource`.

### Контекстное меню
- `DataGrid_PreviewMouseRightButtonDown`: Выделяет ячейку под курсором (если она ещё не выделена) для корректной работы контекстного меню.
- `ContextMenu_AddRow_Click`: Добавляет новую строку в коллекцию.
- `ContextMenu_Edit_Click`: Открывает `ModelEditorWindow` для выбранной строки.
- `ContextMenu_DeleteRow_Click`: Удаляет выбранные строки с подтверждением.
- `ContextMenu_Copy_Click`: Копирует выделенные ячейки или строку в TSV-формате.
- `ContextMenu_PasteExcel_Click`: Вставляет данные из буфера в TSV-формате.
- `UpdateContextMenuForDataGrid`: Динамически добавляет пункт "Открыть таблицу [Родительская]" при клике на FK-ячейку.

### Копирование/вставка
- `CopySelectedCellsToClipboard(DataGrid)`: Формирует TSV-строку из выделенных ячеек с экранированием кавычек.
- `CopySelectedItemToClipboard(object)`: Копирует всю строку с заголовками.
- `GetDisplayedCellValue(object, DataGridColumn)`: Получает отображаемое значение ячейки (поддерживает ComboBox).
- `PasteFromClipboard(DataGrid)`: Парсит TSV и вставляет данные, обновляя существующие строки или создавая новые.
- `ParseTSV(string)`: Парсер TSV с поддержкой экранированных кавычек.
- `IsHeaderRow(List<string>)`: Определяет, является ли строка заголовком (по ключевым словам).
- `GetVisibleColumns(DataGrid)`: Возвращает список видимых столбцов DataGrid.
- `GetPropertyNameFromColumn(DataGridColumn)`: Извлекает имя свойства из привязки столбца.
- `SetPropertyValue(object, PropertyInfo, string)`: Конвертирует и присваивает значение свойству (поддержка int, double, string, FK lookup по имени).

### Горячие клавиши
- `DataGrid_PreviewKeyDown`: Обрабатывает нажатия клавиш (Ctrl+C, Ctrl+V, Ctrl+D, Enter).
- `DeleteSelectedRows`: Удаляет выбранные строки с подтверждением.
- `DuplicateSelectedRow`: Создаёт копию выбранной строки через рефлексию (без копирования Id).
- `OpenEditorForSelectedItem`: Открывает `ModelEditorWindow` для выбранной строки.

## Зависимости
- WPF (Windows Presentation Foundation)
- `AmberBases.Core.Models.Dictionaries.*`
- `AmberBases.UI.ModelEditorWindow` — для редактирования выбранных записей
- `AmberBases.UI.TableViewWindow` — для навигации к родительским таблицам
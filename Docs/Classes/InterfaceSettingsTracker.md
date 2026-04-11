# InterfaceSettingsTracker

**Namespace:** `AmberBases.UI.Tracking`

## Описание
Трекер настроек интерфейса. Сохраняет и восстанавливает параметры UI элементов (ширину колонок DataGrid, положение сплиттеров, высоту Ribbon) в файл `ui_settings.json`.

## Конструктор
- `InterfaceSettingsTracker()` — инициализирует трекер, загружает существующие настройки из `ui_settings.json`

## Методы
| Метод | Описание |
|-------|----------|
| `TrackDataGridView(grid, key)` | Восстанавливает и сохраняет ширину колонок DataGridView |
| `TrackSplitContainer(splitter, key)` | Восстанавливает и сохраняет положение сплиттера |
| `TrackRibbonHeight(ribbon, rowDefinition, key)` | Восстанавливает и сохраняет высоту Ribbon-панели. Если настроек нет, использует defaultHeight (150px). Позволяет менять высоту перетаскиванием границы. Мин: 80px, макс: 400px |
| `TrackPanelHeight(panel, rowDefinition, gridSplitter, key, minHeight, maxHeight)` | Универсальный метод для отслеживания высоты любой панели через GridSplitter. defaultHeight=42px, minHeight=30px, maxHeight=200px |
| `SaveRibbonMinimizedState(tabName, isMinimized)` | Сохраняет состояние свёрнутости всей Ribbon-панели для вкладки |
| `LoadRibbonMinimizedState(tabName)` | Загружает состояние свёрнутости всей Ribbon-панели для вкладки |
| `SaveRibbonGroupStates(tabName, states)` | Сохраняет список состояний свёрнутости лент (RibbonGroup) для вкладки. `states` — `List<(string groupName, bool isCollapsed)>` |
| `LoadRibbonGroupStates(tabName, groupNames)` | Загружает состояния свёрнутости лент для вкладки. Возвращает `List<(string groupName, bool isCollapsed)>` |
| `SaveRibbonGroupState(tabName, groupName, isCollapsed)` | Сохраняет состояние одной ленты для вкладки |
| `LoadRibbonGroupState(tabName, groupName)` | Загружает состояние одной ленты для вкладки (по умолчанию `false` — лента развёрнута) |

## Формат настроек (ui_settings.json)
```json
{
  "MainWindow_RibbonHeight": 150.0,
  "DictionaryEditor_PanelHeight": 42.0,
  "RibbonMinimized_ExcelTab": false,
  "RibbonGroupCollapsed_ExcelTab_FileOperationsGroup": false,
  "RibbonGroupCollapsed_ExcelTab_DatabaseOperationsGroup": true,
  "SomeGrid_Columns": { "colName": 100 }
}
```

## Зависимости
- `Newtonsoft.Json` — сериализация
- `System.Windows.Controls.Ribbon` — WPF Ribbon
- `System.Windows.Forms` — DataGridView, SplitContainer

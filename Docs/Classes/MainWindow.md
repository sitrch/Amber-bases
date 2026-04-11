# MainWindow

**Модуль:** UI  
**Расположение:** `AmberBases/MainWindow.xaml` и `AmberBases/MainWindow.xaml.cs`

## Назначение
Главное окно приложения (WPF). Предоставляет графический интерфейс, использующий элемент управления Ribbon для навигации между представлениями:
1. **Загрузчик Excel**: 
   - Выбор файла Excel через `RibbonGallery`.
   - Отображение списка загруженных таблиц (в `RibbonGallery`).
   - Просмотр данных выбранной таблицы (в `DataGrid`).
   - Сохранение всех загруженных таблиц в базу данных SQLite.
2. **Справочники**: 
   - Интерфейс для редактирования таблиц справочников, предоставляемый контролом `DictionaryEditorView`.

## Логика работы
При инициализации форма создает экземпляры `ExcelDataService` и `SqliteDataService` (внедрение зависимостей через создание экземпляров в конструкторе, так как IoC контейнер в проекте пока не используется). Элемент управления `Ribbon` используется для переключения между двумя основными панелями (`Grid`): `ExcelView` и `DictionaryEditor`.
Для загрузчика реализована автозагрузка списка файлов: в конструкторе происходит подписка на событие `Loaded`, которое сканирует папку `Bases` и загружает первый найденный файл Excel.
Данные из Excel сохраняются во внутреннюю переменную `_currentDataSet`. При выборе другой таблицы форма переключает `ItemsSource` у `DataGrid` на соответствующий `DefaultView` из `DataTable`.
При нажатии на кнопку сохранения вызывается `SqliteDataService` и данные экспортируются в `AmberBases.sqlite` (путь извлекается из `DatabaseConfig`).
Вкладка справочников инициализируется добавлением созданного экземпляра `DictionaryEditorView`.

## Ключевые методы
- `MainWindow_Loaded`: Обработчик события отображения окна, запускающий загрузку списка файлов и инициализацию `DictionaryEditorView`.
- `LoadFileList`: Поиск Excel-файлов в папке `Bases` и добавление их в `RibbonGallery`.
- `BtnLoadExcel_Click`: Обработчик диалога выбора файла Excel вручную.
- `CbTables_SelectionChanged`: Переключение отображаемой таблицы и обновление `ItemsSource` у `DataGrid`.
- `BtnSaveSqlite_Click`: Сохранение данных в SQLite с уведомлением пользователя.

## Зависимости
- WPF (`System.Windows`, `System.Windows.Controls`)
- `System.Windows.Controls.Ribbon`
- `System.ComponentModel` (для `DependencyPropertyDescriptor`)
- `AmberBases.Services.IExcelDataService`
- `AmberBases.Services.ISqliteDataService`
- `AmberBases.Services.ISqliteDataService`
- `AmberBases.UI.Tracking.InterfaceSettingsTracker`
- `AmberBases.Core.DatabaseConfig`

## Отслеживание состояния Ribbon
Для сохранения свёрнутости Ribbon панели **отдельно для каждого таба** используется `DependencyPropertyDescriptor`, который отслеживает изменение свойства `Ribbon.IsMinimizedProperty` в реальном времени:
- При каждом сворачивании/разворачивании Ribbon состояние мгновенно сохраняется для текущего таба через `InterfaceSettingsTracker.SaveRibbonMinimizedState`.
- При переключении на другой таб ранее сохранённое состояние восстанавливается через `RestoreRibbonMinimizedState`.

## Состояние свёрнутости Ribbon (IsMinimized) для каждого таба
Используется `Ribbon.IsMinimized` для сворачивания/разворачивания всей ленты отдельно для каждого таба.

### Структура данных
- Используется `Dictionary<string, bool> _tabMinimizedStates` — ключ: имя таба, значение: true если лента свёрнута
- Сохраняется и восстанавливается через `InterfaceSettingsTracker.SaveRibbonMinimizedState` / `LoadRibbonMinimizedState`

### Способы изменения состояния
1. **Двойной клик на заголовок таба** — стандартное поведение WPF Ribbon, отслеживается через `DependencyPropertyDescriptor`
2. **Метод `ToggleRibbonState(tabName, isMinimized)`** — программное переключение состояния

### Восстановление при переключении табов
- При `MainRibbon_SelectionChanged`:
  1. Сохраняется состояние предыдущего таба через `SaveRibbonMinimizedState`
  2. Восстанавливается состояние нового таба из `_tabMinimizedStates` или из настроек
- `Ribbon_IsMinimizedChanged` — мгновенно сохраняет состояние при каждом изменении

### Ключевые методы
| Метод | Описание |
|-------|----------|
| `RestoreRibbonMinimizedState(tab)` | Восстанавливает `IsMinimized` для таба из настроек |
| `SaveRibbonMinimizedState(tab)` | Сохраняет текущее `IsMinimized` для таба |
| `ToggleRibbonState(tabName, isMinimized)` | Устанавливает состояние свёрнутости для таба |
| `Ribbon_IsMinimizedChanged` | Обработчик изменения `IsMinimized` (двойной клик на таб) |

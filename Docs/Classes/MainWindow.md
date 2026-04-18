# MainWindow

**Модуль:** UI  
**Расположение:** `AmberBases/MainWindow.xaml` и `AmberBases/MainWindow.xaml.cs`

## Назначение
Главное окно приложения на базе WPF с элементом управления Ribbon. Обеспечивает навигацию между тремя вкладками:
1. **Материалы** — отображает `DictionaryEditor` в режиме "только артикулы"
2. **Загрузчик Excel** — загрузка Excel-файлов и сохранение в SQLite
3. **Справочники** — полный интерфейс редактирования справочников

## Логика работы
При инициализации создаёт экземпляры `ExcelDataService` и `SqliteDataService` (внедрение зависимостей через создание в конструкторе). Подписывается на событие `Loaded` для загрузки списка файлов.

## Ключевые методы
- `MainWindow_Loaded`: Инициализация при загрузке окна — загрузка списка файлов, инициализация `DictionaryEditor`, настройка состояний Ribbon.
- `LoadFileList`: Сканирует папку `Bases` и загружает Excel-файлы в `RibbonGallery`.
- `LoadExcelFile(string filePath)`: Загружает выбранный файл Excel и отображает список таблиц.
- `BtnLoadExcel_Click`: Открывает диалог выбора файла Excel.
- `CbTables_SelectionChanged`: Переключает отображаемую таблицу в DataGrid.
- `BtnSaveSqlite_Click`: Сохраняет загруженные данные в SQLite.
- `BtnShowProfileArticles_Click`: Переключает на вкладку "Материалы".
- `BtnShowDictionaries_Click`: Переключает на вкладку "Справочники".
- `MainRibbon_SelectionChanged`: Обрабатывает переключение вкладок.
- `MainWindow_Closing`: Проверяет несохранённые изменения перед закрытием.

## Отслеживание состояния Ribbon
Для каждого таба сохраняется состояние свёрнутости (`IsMinimized`) через `InterfaceSettingsTracker`:
- `RestoreRibbonMinimizedState(RibbonTab tab)`: Восстанавливает состояние из настроек.
- `SaveRibbonMinimizedState(RibbonTab tab)`: Сохраняет текущее состояние.
- `ToggleRibbonState(string tabName, bool isMinimized)`: Программное переключение.
- `Ribbon_IsMinimizedChanged`: Обрабатывает изменение состояния (двойной клик на таб).

## Меню
Выпадающее меню (RibbonApplicationMenu) содержит:
- **Материалы** — переход на вкладку "Материалы"
- **Загрузчик Excel** — переход на вкладку "Загрузчик Excel"
- **Справочники** — переход на вкладку "Справочники"
- **Выход** — закрытие приложения

## Зависимости
- WPF (`System.Windows`, `System.Windows.Controls.Ribbon`)
- `AmberBases.Services.IExcelDataService` / `ExcelDataService`
- `AmberBases.Services.ISqliteDataService` / `SqliteDataService`
- `AmberBases.UI.DictionaryEditorView`
- `AmberBases.UI.Tracking.InterfaceSettingsTracker`
- `AmberBases.Core.DatabaseConfig`
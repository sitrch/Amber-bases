# DictionaryEditorView

**Модуль:** UI  
**Расположение:** `AmberBases/UI/DictionaryEditorView.xaml` и `AmberBases/UI/DictionaryEditorView.xaml.cs`

## Назначение
Главный пользовательский элемент управления (UserControl) на базе WPF для просмотра и редактирования справочников. Реализует интерфейс `IInitControl`. Содержит девять `DictionaryTableControl` для каждого справочника:
- SystemProvidersGrid
- ProfileSystemsGrid
- ColorsGrid
- StandartBarLengthsGrid
- ProfileTypesGrid
- ApplicabilitiesGrid
- ProfileArticlesGrid
- CoatingTypesGrid
- CProfilesGrid

## Логика работы
При инициализации (`InitControl`) создаёт `SqliteDictionaryDataService`, загружает данные и инициализирует каждый `DictionaryTableControl`. Поддерживает фильтрацию по тексту и синхронизацию изменений с БД.

## Ключевые методы
- `InitControl()`: Инициализация контрола, отслеживание высоты панели, асинхронная загрузка данных.
- `LoadData()`: Асинхронно загружает все справочники и инициализирует `DictionaryTableControl`.
- `GetActiveGrid()`: Возвращает видимый `DictionaryTableControl`.
- `ApplyFilter()`: Применяет фильтр поиска к активной таблице.
- `BtnSave_Click`: Сохраняет изменения во все справочники.
- `SyncAllCollections()`: Синхронизирует коллекции с БД (добавление/обновление/удаление).
- `SetMaterialsMode(bool isMaterials)`: Переключает режим "только артикулы" (скрывает панель выбора).
- `ShowProfileArticles()`: Показывает только таблицу артикулов профилей.
- `HasAnyUnsavedChanges()`: Проверяет наличие несохранённых изменений.
- `SaveAllChanges()`: Сохраняет все изменения.
- `DiscardAllChanges()`: Сбрасывает несохранённые изменения.

## Зависимости
- `AmberBases.Services.IDictionaryDataService` / `SqliteDictionaryDataService`
- `AmberBases.UI.DictionaryTableControl`
- `AmberBases.UI.Tracking.EditActionTracker` (через DictionaryTableControl)
- `AmberBases.UI.Tracking.InterfaceSettingsTracker`
- `AmberBases.Core.Models.Dictionaries.*`
- `AmberBases.Core.DatabaseConfig`

## Кнопки панели инструментов
- **Undo** / **Redo** — отмена/повтор действий (привязаны к активному `DictionaryTableControl`)
- **Сохранить** — синхронизация всех справочников с БД
- **Обновить** — перезагрузка данных из БД

## Режим "Материалы"
При переключении на вкладку "Материалы" вызывается `SetMaterialsMode(true)`, скрывающий панель выбора справочников и показывающий только `ProfileArticlesGrid`.
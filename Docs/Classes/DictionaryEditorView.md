# DictionaryEditorView

**Модуль:** UI  
**Расположение:** `AmberBases/UI/DictionaryEditorView.xaml` и `AmberBases/UI/DictionaryEditorView.xaml.cs`

## Назначение
Главное окно редактора справочников. Пользовательский элемент управления (UserControl) на базе WPF, реализующий `IInitControl`, предоставляющий интерфейс для просмотра и редактирования словарей базы данных. Отображает все справочники в виде вкладок (`TabControl`), где каждая вкладка содержит унифицированный компонент `<local:DictionaryTableControl>`. Управляет загрузкой, сохранением и координацией между словарями.

## Зависимости
- `AmberBases.Services.IDictionaryDataService` — для сохранения и загрузки данных.
- `AmberBases.UI.DictionaryTableControl` — переиспользуемый компонент для отображения и редактирования таблиц (каждая вкладка содержит свой экземпляр).
- `AmberBases.UI.Tracking.EditActionTracker` — используется глобально через опрос активной вкладки.
- `AmberBases.Core.Models.Dictionaries.*` — классы моделей для коллекций.

## Основные методы
- `InitControl()`: Инициализация контрола, скрытие спиннера и запуск асинхронной загрузки.
- `LoadAllDataAsync()` — асинхронно загружает все справочники из базы в наблюдаемые коллекции. В процессе загрузки вызывает метод `Initialize` для каждого `DictionaryTableControl` (например, `ProvidersTableControl.Initialize(...)`), передавая коллекции, lookup-справочники и callback для открытия родительских таблиц.
- `SaveAllDataAsync()` — сохраняет все измененные данные обратно в базу, делая синхронизацию списков с БД (CRUD операции через Service).

## Взаимодействие (DI / вызовы)
- Окно получает зависимости через инверсию управления (DI) или инициализацию.
- Делегирует UI-логику, поиск, undo/redo, работу с буфером обмена и контекстное меню компонентам `DictionaryTableControl`.
- Обрабатывает глобальные кнопки Undo/Redo: при смене вкладки (`TabControl_SelectionChanged`) тулбар привязывается к `ActionTracker` активного `DictionaryTableControl`.
- Координирует открытие родительских таблиц из контролов (`OpenParentTable`, `SelectTabForType`).

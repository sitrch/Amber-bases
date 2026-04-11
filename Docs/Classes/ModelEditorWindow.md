# ModelEditorWindow

**Модуль:** UI  
**Расположение:** `AmberBases/UI/ModelEditorWindow.xaml` и `AmberBases/UI/ModelEditorWindow.xaml.cs`

## Назначение
Универсальное модальное окно для редактирования любой сущности справочника, наследующей `BaseDictionaryModel`. Использует рефлексию для автоматической генерации полей формы на основе свойств объекта. **Поддерживает навигацию к родительским записям через кнопку "⚙" рядом с ComboBox.**

## Логика работы
- Принимает объект типа `BaseDictionaryModel`, Lookup-коллекцию для ComboBox-полей внешних ключей, и опционально словарь всех коллекций (`Dictionary<Type, IEnumerable>`) для поддержки навигации к родительским таблицам.
- При создании окна анализирует свойства модели через `Reflection`, игнорируя технические поля (`Id`, `Position`, `Info`) и навигационные коллекции.
- Для каждого свойства создаётся соответствующий редактор:
  - `TextBox` — для строковых и числовых типов
  - `ComboBox` с кнопкой **"⚙ Изменить"** — для foreign key полей (свойства с суффиксом `Id`, если есть коллекция родительского типа)
- При нажатии кнопки "⚙" открывается `TableViewWindow` с полноценной таблицей родительской сущности (вместо рекурсивного ModelEditorWindow).
- При нажатии "Сохранить" принудительно обновляет binding и закрывает окно с `DialogResult = true`.
- Заголовок и метки полей русифицированы через словарные маппинги.

## Ключевые методы
- `ModelEditorWindow(object model, IEnumerable lookupCollection)`: Конструктор (обратная совместимость).
- `ModelEditorWindow(object model, IEnumerable lookupCollection, Dictionary<Type, IEnumerable> allCollections)`: Конструктор с поддержкой навигации к родительским таблицам.
- `GenerateFields()`: Генерирует UI-элементы на основе свойств модели.
- `CreateForeignKeyEditor(PropertyInfo prop)`: Создает горизонтальный контейнер с ComboBox и кнопкой "⚙" для foreign key полей.
- `GetDisplayName(string typeName)`: Преобразует имя типа в русифицированное название.
- `GetPropertyDisplayName(string propertyName)`: Преобразует имя свойства в русифицированную метку.
- `IsForeignKey(PropertyInfo prop)`: Определяет, является ли свойство внешним ключом (проверяет наличие коллекции родительского типа).
- `GetParentEntityType(PropertyInfo prop)`: Возвращает тип родительской сущности по foreign key свойству.
- `GetCollectionForType(Type type)`: Возвращает коллекцию для указанного типа из словаря.
- `OpenParentEditor(PropertyInfo fkProp, IEnumerable parentCollection)`: Открывает `TableViewWindow` с полноценной таблицей родительской сущности. После закрытия обновляет ComboBox.
- `BuildMutableCollections()`: Создаёт mutable копии коллекций (ObservableCollection) для передачи в TableViewWindow.
- `RefreshComboBox(PropertyInfo fkProp, IEnumerable parentCollection)`: Обновляет ComboBox после редактирования родительской записи.
- `BtnSave_Click`: Принудительно обновляет binding и подтверждает изменения.
- `BtnCancel_Click`: Отменяет изменения и закрывает окно.

## Зависимости
- WPF (Windows Presentation Foundation)
- `AmberBases.Core.Models.Dictionaries.BaseDictionaryModel`
- `System.Reflection`
- `System.Windows.Data`
- `AmberBases.UI.TableViewWindow` — модальное окно с таблицей справочника
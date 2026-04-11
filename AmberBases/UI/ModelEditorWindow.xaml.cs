using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using AmberBases.Core.Models.Dictionaries;
using AmberBases.Services;

namespace AmberBases.UI
{
    /// <summary>
    /// Универсальное модальное окно для редактирования сущностей справочников.
    /// Поддерживает foreign key поля с ComboBox и кнопкой "Изменить" для перехода к родительской записи.
    /// </summary>
    public partial class ModelEditorWindow : Window
    {
        private readonly object _model;
        private readonly IEnumerable _lookupCollection;
        private readonly Dictionary<Type, IEnumerable> _allCollections;
        private readonly IDictionaryDataService _dataService;
        private readonly string _dbPath;
        private readonly Dictionary<FrameworkElement, PropertyInfo> _fieldBindings = new Dictionary<FrameworkElement, PropertyInfo>();
        private readonly Dictionary<string, object> _originalValues;
        private bool _isClosing = false;

        /// <summary>
        /// Конструктор для простого случая с одной lookup-коллекцией.
        /// </summary>
        public ModelEditorWindow(object model, IEnumerable lookupCollection = null)
            : this(model, lookupCollection, null, null, null)
        {
        }

        /// <summary>
        /// Конструктор с поддержкой родительских таблиц.
        /// </summary>
        /// <param name="model">Редактируемая модель</param>
        /// <param name="lookupCollection">Lookup-коллекция для foreign key полей (устаревший параметр)</param>
        /// <param name="allCollections">Словарь всех коллекций: Type -> IEnumerable для поддержки родительских таблиц</param>
        /// <param name="dataService">Сервис для сохранения в БД</param>
        /// <param name="dbPath">Путь к файлу БД</param>
        public ModelEditorWindow(object model, IEnumerable lookupCollection, Dictionary<Type, IEnumerable> allCollections,
            IDictionaryDataService dataService = null, string dbPath = null)
        {
            InitializeComponent();
            _model = model;
            _lookupCollection = lookupCollection;
            _allCollections = allCollections;
            _dataService = dataService;
            _dbPath = dbPath;

            // Сохраняем оригинальные значения свойств модели
            _originalValues = CloneModelValues(model);

            var typeName = model.GetType().Name;
            var displayName = GetDisplayName(typeName);
            TitleTextBlock.Text = $"Редактирование: {displayName}";

            GenerateFields();

            // Подписываемся на событие закрытия
            Closing += ModelEditorWindow_Closing;
        }

        /// <summary>
        /// Клонирование значений свойств модели для сравнения.
        /// </summary>
        private Dictionary<string, object> CloneModelValues(object obj)
        {
            var values = new Dictionary<string, object>();
            if (obj == null) return values;

            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(obj);
                if (!(value is IEnumerable && value is not string))
                {
                    values[prop.Name] = value;
                }
            }
            return values;
        }

        /// <summary>
        /// Проверяет, есть ли изменения в модели по сравнению с оригиналом.
        /// </summary>
        private bool CheckForChanges()
        {
            var currentValues = CloneModelValues(_model);

            if (currentValues.Count != _originalValues.Count)
                return true;

            foreach (var kvp in currentValues)
            {
                if (!_originalValues.TryGetValue(kvp.Key, out var originalValue))
                    return true;

                if (!Equals(kvp.Value, originalValue))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Восстанавливает оригинальные значения свойств модели.
        /// </summary>
        private void RestoreOriginalValues()
        {
            foreach (var kvp in _originalValues)
            {
                var prop = _model.GetType().GetProperty(kvp.Key);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        prop.SetValue(_model, kvp.Value);
                    }
                    catch
                    {
                        // Игнорируем ошибки восстановления
                    }
                }
            }
        }

        /// <summary>
        /// Сохраняет изменения в базу данных.
        /// </summary>
        private bool SaveToDatabase()
        {
            if (_dataService == null || string.IsNullOrEmpty(_dbPath))
            {
                MessageBox.Show("Сервис сохранения не настроен. Изменения сохранены только в памяти.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                var modelType = _model.GetType();

                if (modelType == typeof(SystemProvider))
                {
                    var item = (SystemProvider)_model;
                    if (item.Id == 0) _dataService.AddSystemProvider(item, _dbPath);
                    else _dataService.UpdateSystemProvider(item, _dbPath);
                }
                else if (modelType == typeof(ProfileSystem))
                {
                    var item = (ProfileSystem)_model;
                    if (item.Id == 0) _dataService.AddProfileSystem(item, _dbPath);
                    else _dataService.UpdateProfileSystem(item, _dbPath);
                }
                else if (modelType == typeof(AmberBases.Core.Models.Dictionaries.Color))
                {
                    var item = (AmberBases.Core.Models.Dictionaries.Color)_model;
                    if (item.Id == 0) _dataService.AddColor(item, _dbPath);
                    else _dataService.UpdateColor(item, _dbPath);
                }
                else if (modelType == typeof(WhipLength))
                {
                    var item = (WhipLength)_model;
                    if (item.Id == 0) _dataService.AddWhipLength(item, _dbPath);
                    else _dataService.UpdateWhipLength(item, _dbPath);
                }
                else if (modelType == typeof(ProfileType))
                {
                    var item = (ProfileType)_model;
                    if (item.Id == 0) _dataService.AddProfileType(item, _dbPath);
                    else _dataService.UpdateProfileType(item, _dbPath);
                }
                else if (modelType == typeof(Applicability))
                {
                    var item = (Applicability)_model;
                    if (item.Id == 0) _dataService.AddApplicability(item, _dbPath);
                    else _dataService.UpdateApplicability(item, _dbPath);
                }
                else if (modelType == typeof(ProfileArticle))
                {
                    var item = (ProfileArticle)_model;
                    if (item.Id == 0) _dataService.AddProfileArticle(item, _dbPath);
                    else _dataService.UpdateProfileArticle(item, _dbPath);
                }
                else if (modelType == typeof(CoatingType))
                {
                    var item = (CoatingType)_model;
                    if (item.Id == 0) _dataService.AddCoatingType(item, _dbPath);
                    else _dataService.UpdateCoatingType(item, _dbPath);
                }
                else if (modelType == typeof(Customer))
                {
                    var item = (Customer)_model;
                    if (item.Id == 0) _dataService.AddCustomer(item, _dbPath);
                    else _dataService.UpdateCustomer(item, _dbPath);
                }
                else if (modelType == typeof(CustomerContact))
                {
                    var item = (CustomerContact)_model;
                    if (item.Id == 0) _dataService.AddCustomerContact(item, _dbPath);
                    else _dataService.UpdateCustomerContact(item, _dbPath);
                }
                else
                {
                    MessageBox.Show($"Неизвестный тип модели: {modelType.Name}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                _originalValues.Clear();
                foreach (var kvp in CloneModelValues(_model))
                {
                    _originalValues[kvp.Key] = kvp.Value;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Обработчик события закрытия окна.
        /// </summary>
        private void ModelEditorWindow_Closing(object sender, CancelEventArgs e)
        {
            // Если уже в процессе закрытия (после Save/Cancel) - не показываем
            if (_isClosing) return;

            // Проверяем, есть ли изменения
            if (!CheckForChanges()) return;

            e.Cancel = true;

            var result = MessageBox.Show(
                "Есть несохранённые изменения.\nДа — Сохранить и закрыть\nНет — Отменить изменения и закрыть\nОтмена — Остаться в редакторе",
                "Несохранённые изменения",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Cancel);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    if (SaveToDatabase())
                    {
                        _isClosing = true;
                        Close();
                    }
                    break;
                case MessageBoxResult.No:
                    RestoreOriginalValues();
                    _isClosing = true;
                    Close();
                    break;
                case MessageBoxResult.Cancel:
                    // Остаёмся в окне
                    break;
            }
        }

        private void GenerateFields()
        {
            var modelType = _model.GetType();
            var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (prop.Name == "Id" || prop.Name == "Position" || prop.Name == "Info")
                    continue;
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                    continue;

                var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

                var label = new TextBlock
                {
                    Text = GetPropertyDisplayName(prop.Name),
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                panel.Children.Add(label);

                FrameworkElement editor = null;

                if (IsForeignKey(prop))
                {
                    editor = CreateForeignKeyEditor(prop);
                }
                else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                {
                    var textBox = new TextBox();
                    var binding = new Binding(prop.Name) { Mode = BindingMode.TwoWay };
                    textBox.SetBinding(TextBox.TextProperty, binding);
                    editor = textBox;
                }
                else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?) ||
                         prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                {
                    var textBox = new TextBox();
                    var binding = new Binding(prop.Name) { Mode = BindingMode.TwoWay };
                    textBox.SetBinding(TextBox.TextProperty, binding);
                    editor = textBox;
                }
                else
                {
                    var textBox = new TextBox();
                    var binding = new Binding(prop.Name) { Mode = BindingMode.TwoWay };
                    textBox.SetBinding(TextBox.TextProperty, binding);
                    editor = textBox;
                }

                if (editor != null)
                {
                    panel.Children.Add(editor);
                    _fieldBindings[editor] = prop;
                    FieldsPanel.Children.Add(panel);
                }
            }
        }

        /// <summary>
        /// Создает горизонтальный контейнер с ComboBox и кнопкой "Изменить" для foreign key поля.
        /// </summary>
        private FrameworkElement CreateForeignKeyEditor(PropertyInfo prop)
        {
            var horizontalPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var parentType = GetParentEntityType(prop);
            var parentCollection = GetCollectionForType(parentType);

            var comboBox = new ComboBox
            {
                ItemsSource = parentCollection,
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id",
                Width = 250,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var binding = new Binding(prop.Name) { Mode = BindingMode.TwoWay };
            comboBox.SetBinding(Selector.SelectedValueProperty, binding);
            _fieldBindings[comboBox] = prop;

            horizontalPanel.Children.Add(comboBox);

            if (parentCollection != null)
            {
                var editButton = new Button
                {
                    Content = "⚙",
                    Width = 30,
                    Height = 23,
                    Padding = new Thickness(2),
                    ToolTip = $"Открыть таблицу \"{GetPropertyDisplayName(prop.Name)}\""
                };
                editButton.Click += (s, e) => OpenParentEditor(prop, parentCollection);
                horizontalPanel.Children.Add(editButton);
            }

            return horizontalPanel;
        }

        private Type GetParentEntityType(PropertyInfo prop)
        {
            var modelType = _model.GetType();
            var navPropName = prop.Name.Replace("Id", "");

            var navProp = modelType.GetProperty(navPropName);
            if (navProp != null && !navProp.PropertyType.IsPrimitive && navProp.PropertyType != typeof(string))
            {
                return navProp.PropertyType;
            }

            var typeMappings = new Dictionary<string, Type>
            {
                { "SystemProviderId", typeof(SystemProvider) },
                { "ProfileSystemId", typeof(ProfileSystem) },
                { "ProfileTypeId", typeof(ProfileType) },
                { "ApplicabilityId", typeof(Applicability) },
                { "CoatingTypeId", typeof(CoatingType) }
            };

            return typeMappings.ContainsKey(prop.Name) ? typeMappings[prop.Name] : null;
        }

        private IEnumerable GetCollectionForType(Type type)
        {
            if (type == null) return null;

            if (_allCollections != null && _allCollections.ContainsKey(type))
            {
                return _allCollections[type];
            }

            return null;
        }

        private void OpenParentEditor(PropertyInfo fkProp, IEnumerable parentCollection)
        {
            var currentId = fkProp.GetValue(_model) as int?;
            if (currentId == null || currentId == 0)
            {
                MessageBox.Show("Не выбрана запись для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var parentType = GetParentEntityType(fkProp);
            if (parentType == null) return;

            object parentItem = null;

            foreach (var item in parentCollection)
            {
                var itemProp = item.GetType().GetProperty("Id");
                if (itemProp != null)
                {
                    var itemId = (int)itemProp.GetValue(item);
                    if (itemId == currentId)
                    {
                        parentItem = item;
                        break;
                    }
                }
            }

            if (parentItem == null)
            {
                MessageBox.Show("Не удалось найти родительскую запись.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var mutableCollections = BuildMutableCollections();

            var parentCollectionList = parentCollection as System.Collections.IList;
            if (parentCollectionList == null)
            {
                var listType = typeof(System.Collections.ObjectModel.ObservableCollection<>).MakeGenericType(parentCollection.GetType().GetGenericArguments()[0]);
                parentCollectionList = (System.Collections.IList)Activator.CreateInstance(listType, parentCollection);
            }

            var tableWindow = new TableViewWindow(parentType, parentCollectionList, mutableCollections)
            {
                Owner = this
            };

            tableWindow.Closed += (s, e) =>
            {
                RefreshComboBox(fkProp, parentCollection);
            };

            tableWindow.ShowDialog();
        }

        /// <summary>
        /// Строит mutable копии коллекций для TableViewWindow.
        /// </summary>
        private Dictionary<Type, IEnumerable> BuildMutableCollections()
        {
            var mutable = new Dictionary<Type, IEnumerable>();

            if (_allCollections == null) return mutable;

            foreach (var kvp in _allCollections)
            {
                var elementType = kvp.Key;
                var listType = typeof(System.Collections.ObjectModel.ObservableCollection<>).MakeGenericType(elementType);
                var observable = (IEnumerable)Activator.CreateInstance(listType, kvp.Value);
                mutable[elementType] = observable;
            }

            return mutable;
        }

        private void RefreshComboBox(PropertyInfo fkProp, IEnumerable parentCollection)
        {
            foreach (var kvp in _fieldBindings)
            {
                if (kvp.Value == fkProp && kvp.Key is ComboBox comboBox)
                {
                    comboBox.ItemsSource = null;
                    comboBox.ItemsSource = parentCollection;
                    break;
                }
            }
        }

        private string GetDisplayName(string typeName)
        {
            var names = new Dictionary<string, string>
            {
                { "SystemProvider", "Поставщик систем" },
                { "ProfileSystem", "Профильная система" },
                { "Color", "Цвет" },
                { "WhipLength", "Длина хлыста" },
                { "ProfileType", "Тип профиля" },
                { "Applicability", "Применимость" },
                { "ProfileArticle", "Артикул профиля" },
                { "CoatingType", "Тип покрытия" },
                { "Customer", "Клиент" },
                { "CustomerContact", "Контакт клиента" }
            };
            return names.ContainsKey(typeName) ? names[typeName] : typeName;
        }

        private string GetPropertyDisplayName(string propertyName)
        {
            var names = new Dictionary<string, string>
            {
                { "Name", "Название" },
                { "Information", "Информация" },
                { "Description", "Описание" },
                { "ColorName", "Название цвета" },
                { "RAL", "RAL код" },
                { "CoatingTypeId", "Тип покрытия" },
                { "Length", "Длина" },
                { "Article", "Артикул" },
                { "SystemProviderId", "Поставщик систем" },
                { "ProfileSystemId", "Профильная система" },
                { "ProfileTypeId", "Тип профиля" },
                { "ApplicabilityId", "Применимость" },
                { "FileName", "Имя файла" },
                { "Size", "Размер" },
                { "StepHeight", "Высота ступени" }
            };
            return names.ContainsKey(propertyName) ? names[propertyName] : propertyName;
        }

        private bool IsForeignKey(PropertyInfo prop)
        {
            if (!prop.Name.EndsWith("Id") || (prop.PropertyType != typeof(int) && prop.PropertyType != typeof(int?)))
                return false;

            var parentType = GetParentEntityType(prop);
            return parentType != null && GetCollectionForType(parentType) != null;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Force update bindings
            foreach (var editor in _fieldBindings.Keys)
            {
                if (editor is TextBox tb)
                {
                    var binding = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
                    binding?.UpdateSource();
                }
                else if (editor is ComboBox cb)
                {
                    var binding = BindingOperations.GetBindingExpression(cb, Selector.SelectedValueProperty);
                    binding?.UpdateSource();
                }
            }

            if (CheckForChanges())
            {
                if (SaveToDatabase())
                {
                    _isClosing = true;
                    DialogResult = true;
                    Close();
                }
            }
            else
            {
                _isClosing = true;
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            _isClosing = true;
            Close();
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Media = System.Windows.Media; 
using AmberBases.Core.Models.Dictionaries;
using AmberBases.Services;
using AmberBases.UI.Tracking;

namespace AmberBases.UI
{
    /// <summary>
    /// Универсальный UserControl для отображения таблицы справочника с поддержкой навигации к родительским записям.
    /// </summary>
    public partial class DictionaryTableControl : UserControl
    {
        private Type _entityType;
        private IEnumerable _itemsSource;
        private IList _collection;
        private Dictionary<Type, IEnumerable> _allCollections;
        private Action<object, Type> _openParentTableCallback;
        private IDictionaryDataService _dataService;
        private string _dbPath;

        // Lookup коллекции для FK полей
        private ObservableCollection<SystemProvider> _systemProviders;
        private ObservableCollection<ProfileSystem> _profileSystems;
        private ObservableCollection<ProfileType> _profileTypes;
        private ObservableCollection<Applicability> _applicabilities;
        private ObservableCollection<CoatingType> _coatingTypes;

        // Undo/Redo tracker
        private EditActionTracker _actionTracker;
        public EditActionTracker ActionTracker => _actionTracker;

        // Для отслеживания изменений при редактировании ячейки
        private object _editingItem;
        private object _editingOldItem;
        private string _editingPropertyName;

        private Window _parentWindow;
        private List<string> _currentColumnOrder = new();

        // Навигационные свойства, которые не должны отображаться как колонки
        private static readonly HashSet<string> ExcludedNavigationProperties = new HashSet<string>
        {
            "Manufacturer", "System", "Color", "StandartBarLength", "ProfileType",
            "Provider", "CoatingType", "Article"
        };

        public DictionaryTableControl()
        {
            InitializeComponent();
            Loaded += DictionaryTableControl_Loaded;
            Unloaded += DictionaryTableControl_Unloaded;
        }

        private void DictionaryTableControl_Loaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.PreviewKeyDown += ParentWindow_PreviewKeyDown;
            }
        }

        private void DictionaryTableControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
            {
                _parentWindow.PreviewKeyDown -= ParentWindow_PreviewKeyDown;
                _parentWindow = null;
            }
        }

        private void ParentWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!this.IsVisible || IsEditing()) return;

            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            if (isCtrl && e.Key == Key.Z)
            {
                PerformUndo();
                e.Handled = true;
            }
            else if (isCtrl && e.Key == Key.Y)
            {
                PerformRedo();
                e.Handled = true;
            }
            else if (isCtrl && e.Key == Key.C)
            {
                CopySelectedCells(MainDataGrid);
                e.Handled = true;
            }
            else if (isCtrl && e.Key == Key.V)
            {
                PasteFromClipboard(MainDataGrid);
                e.Handled = true;
            }
            else if (isCtrl && e.Key == Key.D)
            {
                DuplicateSelectedRow();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                CommitEdit();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Проверяет, находится ли фокус в элементе ввода текста.
        /// </summary>
        private bool IsEditing()
        {
            var focused = FocusManager.GetFocusedElement(this);
            return focused is TextBox || focused is ComboBox || focused is CheckBox;
        }

        public Type EntityType => _entityType;

        /// <summary>
        /// Инициализирует контрол с данными для отображения.
        /// </summary>
        public void Initialize(Type entityType, IList collection, Dictionary<Type, IEnumerable> allCollections,
            IDictionaryDataService dataService = null, string dbPath = null, Action<object, Type> openParentCallback = null)
        {
            _entityType = entityType;
            _collection = collection;
            _itemsSource = collection;
            _allCollections = allCollections;
            _dataService = dataService;
            _dbPath = dbPath;
            _openParentTableCallback = openParentCallback;

            // Инициализируем трекер undo/redo
            _actionTracker = new EditActionTracker(collection, entityType);
            _actionTracker.StateChanged += OnTrackerStateChanged;
            _actionTracker.Initialize();

            // Подписываемся на события редактирования ячеек
            MainDataGrid.PreparingCellForEdit += MainDataGrid_PreparingCellForEdit;
            MainDataGrid.CellEditEnding += MainDataGrid_CellEditEnding;

            // Подписываемся на событие перемещения столбцов
            MainDataGrid.ColumnReordered += MainDataGrid_ColumnReordered;

            // Извлекаем lookup коллекции из allCollections
            ExtractLookupCollections();

            // Устанавливаем заголовок
            TitleTextBlock.Text = DisplayNameProvider.GetTypeName(entityType.Name);

            // Настраиваем столбцы
            SetupColumns();

            // Заполняем комбобокс выбора столбца для поиска
            PopulateSearchColumnCombo();

            // Привязываем данные к TrackingTable
            MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;
        }

        private void PopulateSearchColumnCombo()
        {
            SearchColumnCombo.Items.Clear();
            SearchColumnCombo.Items.Add(new ComboBoxItem { Content = "Все столбцы", Tag = "" });

            var columns = GetFilterableColumnNames();
            foreach (var col in columns)
            {
                SearchColumnCombo.Items.Add(new ComboBoxItem { Content = col.DisplayName, Tag = col.Name });
            }

            SearchColumnCombo.SelectedIndex = 0;
        }

        private void SearchColumnCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchColumnCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                string columnName = selectedItem.Tag as string ?? "";
                string columnDisplayName = selectedItem.Content as string ?? "";

                if (string.IsNullOrEmpty(columnName) || columnDisplayName == "Все столбцы")
                {
                    ApplyFilter(SearchTextBox.Text);
                }
                else
                {
                    ApplyFilter(SearchTextBox.Text, columnDisplayName);
                }
            }
        }

        private void ExtractLookupCollections()
        {
            if (_allCollections == null) return;

            if (_allCollections.TryGetValue(typeof(SystemProvider), out var sp))
                _systemProviders = sp as ObservableCollection<SystemProvider>;
            if (_allCollections.TryGetValue(typeof(ProfileSystem), out var ps))
                _profileSystems = ps as ObservableCollection<ProfileSystem>;
            if (_allCollections.TryGetValue(typeof(ProfileType), out var pt))
                _profileTypes = pt as ObservableCollection<ProfileType>;
            if (_allCollections.TryGetValue(typeof(Applicability), out var app))
                _applicabilities = app as ObservableCollection<Applicability>;
            if (_allCollections.TryGetValue(typeof(CoatingType), out var ct))
                _coatingTypes = ct as ObservableCollection<CoatingType>;
        }

        #region Undo/Redo Handlers

        private void OnTrackerStateChanged(bool canUndo, bool canRedo)
        {
            if (UndoButton != null)
                UndoButton.IsEnabled = canUndo;
            if (RedoButton != null)
                RedoButton.IsEnabled = canRedo;
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            PerformUndo();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            PerformRedo();
        }

        public void Undo()
        {
            Console.WriteLine($"[UI Undo] Start. CanUndo={_actionTracker?.CanUndo}");
            
            // Сохраняем позицию и выделение
            var selectedIndex = MainDataGrid.SelectedIndex;
            var selectedItem = MainDataGrid.SelectedItem;
            
            if (_actionTracker?.Undo() == true)
            {
                Console.WriteLine($"[UI Undo] Done. Tracker.CanUndo={_actionTracker.CanUndo}, TableRows={_actionTracker.TrackingTable?.Rows.Count}");
                
                // Очищаем и перепривязываем ItemsSource для корректного обновления UI
                MainDataGrid.ItemsSource = null;
                MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;
                
                // Восстанавливаем выделение после применения ItemsSource
                if (selectedIndex >= 0 && selectedIndex < MainDataGrid.Items.Count)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (selectedIndex < MainDataGrid.Items.Count)
                        {
                            MainDataGrid.SelectedIndex = selectedIndex;
                            MainDataGrid.ScrollIntoView(MainDataGrid.SelectedItem);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            else
            {
                Console.WriteLine("[UI Undo] Undo returned false");
            }
        }

        public void Redo()
        {
            Console.WriteLine($"[UI Redo] Start. CanRedo={_actionTracker?.CanRedo}");
            
            // Сохраняем позицию и выделение
            var selectedIndex = MainDataGrid.SelectedIndex;
            
            if (_actionTracker?.Redo() == true)
            {
                Console.WriteLine($"[UI Redo] Done. Tracker.CanRedo={_actionTracker.CanRedo}, TableRows={_actionTracker.TrackingTable?.Rows.Count}");
                
                // Очищаем и перепривязываем ItemsSource для корректного обновления UI
                MainDataGrid.ItemsSource = null;
                MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;
                
                // Восстанавливаем выделение после применения ItemsSource
                if (selectedIndex >= 0 && selectedIndex < MainDataGrid.Items.Count)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (selectedIndex < MainDataGrid.Items.Count)
                        {
                            MainDataGrid.SelectedIndex = selectedIndex;
                            MainDataGrid.ScrollIntoView(MainDataGrid.SelectedItem);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            else
            {
                Console.WriteLine("[UI Redo] Redo returned false");
            }
        }

        private void PerformUndo()
        {
            Undo();
        }

        private void PerformRedo()
        {
            Redo();
        }

        private void MainDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            _editingItem = e.Row.Item;
            _editingPropertyName = e.Column.SortMemberPath ?? GetPropertyNameFromColumn(e.Column);
            _editingOldItem = _editingItem != null ? _actionTracker.DeepClone(_editingItem) : null;
        }

        private void MainDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && _editingItem != null && _editingOldItem != null)
            {
                var editingItem = _editingItem;
                var oldItem = _editingOldItem;
                var propertyName = _editingPropertyName;
                var rowItem = e.Row.Item;

                // Используем Dispatcher.BeginInvoke, чтобы binding успел обновить источник
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var newValue = GetPropertyValue(editingItem, propertyName);
                    var oldValue = GetPropertyValue(oldItem, propertyName);

                    Console.WriteLine($"[CellEditEnding] Property={propertyName}, oldValue={oldValue}, newValue={newValue}, equal={Equals(oldValue, newValue)}");

                    if (!Equals(oldValue, newValue))
                    {
                        // Находим sourceItem через GetSourceItemFromDataGrid для корректного индекса
                        var sourceItem = GetSourceItemFromDataGrid(rowItem, MainDataGrid);
                        if (sourceItem != null)
                        {
                            var prop = sourceItem.GetType().GetProperty(propertyName);
                            if (prop != null && prop.CanWrite)
                            {
                                try
                                {
                                    object convertedValue = null;
                                    if (newValue != DBNull.Value && newValue != null)
                                    {
                                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                        
                                        // Для строковых типов просто присваиваем значение
                                        if (targetType == typeof(string))
                                        {
                                            convertedValue = newValue.ToString();
                                        }
                                        else
                                        {
                                            convertedValue = Convert.ChangeType(newValue, targetType);
                                        }
                                    }
                                    prop.SetValue(sourceItem, convertedValue);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Ошибка при сохранении значения в поле '{GetPropertyNameFromColumn(e.Column)}':\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                            }
                        }

                        Console.WriteLine("[CellEditEnding] Changes detected, calling DetectAndRecordChanges");
                        // Перестраиваем таблицу для корректного Undo/Redo
                        _actionTracker.DetectAndRecordChanges();
                        
                        // Принудительно обновляем UI
                        MainDataGrid.ItemsSource = null;
                        MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;
                    }
                    else
                    {
                        Console.WriteLine("[CellEditEnding] Values are equal, NOT tracking");
                    }
                }), System.Windows.Threading.DispatcherPriority.Input);
            }

            _editingItem = null;
            _editingOldItem = null;
            _editingPropertyName = null;
        }

        private object GetPropertyValue(object item, string propertyName)
        {
            if (item == null || string.IsNullOrEmpty(propertyName)) return null;
            
            try
            {
                if (item is System.Data.DataRowView drv)
                {
                    if (drv.Row.Table.Columns.Contains(propertyName))
                    {
                        return drv[propertyName] == DBNull.Value ? null : drv[propertyName];
                    }
                    return null;
                }

                var prop = item.GetType().GetProperty(propertyName);
                return prop?.GetValue(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetPropertyValue] Error getting property '{propertyName}': {ex.Message}");
                return null;
            }
        }

        #endregion

        private void SetupColumns()
        {
            MainDataGrid.Columns.Clear();

            var properties = _entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Сначала добавляем FK-поле ArticleId, если оно есть
            var articleIdProp = properties.FirstOrDefault(p => p.Name == "ArticleId");
            if (articleIdProp != null && IsForeignKey(articleIdProp))
            {
                AddComboBoxColumn(articleIdProp);
            }

            // Затем добавляем остальные свойства
            foreach (var prop in properties)
            {
                // Пропускаем коллекции
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                    continue;

                // Проверяем, является ли поле FK
                if (IsForeignKey(prop))
                {
                    AddComboBoxColumn(prop);
                }
                else if (!prop.Name.EndsWith("Id")) // Пропускаем только не-FK поля, заканчивающиеся на Id
                {
                    // Проверяем видимость через атрибуты для текстовых полей
                    if (!ColumnSettings.IsColumnVisible(_entityType.Name, prop.Name))
                        continue;

                    var textCol = new DataGridTextColumn
                    {
                        Header = ColumnSettings.GetColumnName(_entityType.Name, prop.Name),
                        Binding = new System.Windows.Data.Binding(prop.Name) { Mode = BindingMode.TwoWay },
                        Width = DataGridLength.Auto
                    };
                    MainDataGrid.Columns.Add(textCol);
                }
            }

            // Применяем сохранённый порядок столбцов
            var savedOrder = ColumnOrderStore.GetOrder(_entityType.Name);
            if (savedOrder != null && savedOrder.Count > 0)
            {
                _currentColumnOrder = savedOrder;
                ApplyColumnOrder(savedOrder);
            }
            else
            {
                _currentColumnOrder = MainDataGrid.Columns.Select(c => GetColumnPropertyName(c)).ToList();
            }
        }

        private string GetColumnPropertyName(DataGridColumn column)
        {
            if (column is DataGridBoundColumn boundColumn && boundColumn.Binding is System.Windows.Data.Binding binding)
            {
                return binding.Path.Path;
            }
            if (column is DataGridComboBoxColumn comboBoxColumn && comboBoxColumn.SelectedValueBinding is System.Windows.Data.Binding cbBinding)
            {
                return cbBinding.Path.Path;
            }
            return null;
        }

        private void ApplyColumnOrder(List<string> order)
        {
            var columns = new List<DataGridColumn>(MainDataGrid.Columns);

            for (int i = 0; i < order.Count; i++)
            {
                var propName = order[i];
                var column = columns.FirstOrDefault(c => GetColumnPropertyName(c) == propName);
                if (column != null)
                {
                    var currentIndex = MainDataGrid.Columns.IndexOf(column);
                    if (currentIndex != i && currentIndex >= 0)
                    {
                        MainDataGrid.Columns.Move(currentIndex, i);
                    }
                }
            }
        }

        private void AddComboBoxColumn(PropertyInfo prop)
        {
            var parentType = GetParentEntityType(prop);
            if (parentType != null)
            {
                var comboBoxCol = new DataGridComboBoxColumn
                {
                    Header = ColumnSettings.GetColumnName(_entityType.Name, prop.Name),
                    SelectedValuePath = "Id",
                    DisplayMemberPath = "Name",
                    Width = DataGridLength.Auto
                };

                // Определяем источник данных для ComboBox
                var lookupCollection = GetCollectionForType(parentType);
                if (lookupCollection != null)
                {
                    comboBoxCol.ItemsSource = lookupCollection;
                }

                var binding = new System.Windows.Data.Binding(prop.Name) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
                comboBoxCol.SelectedValueBinding = binding;

                MainDataGrid.Columns.Add(comboBoxCol);
            }
        }

        private bool IsForeignKey(PropertyInfo prop)
        {
            if (!prop.Name.EndsWith("Id"))
                return false;

            var parentType = GetParentEntityType(prop);
            return parentType != null && GetCollectionForType(parentType) != null;
        }

    private Type GetParentEntityType(PropertyInfo prop)
    {
        var navPropName = prop.Name.Replace("Id", "");
        var navProp = _entityType.GetProperty(navPropName);
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
            { "CoatingTypeId", typeof(CoatingType) },
            { "ManufacturerId", typeof(SystemProvider) },
            { "SystemId", typeof(ProfileSystem) },
            { "ColorId", typeof(Color) },
            { "StandartBarLengthId", typeof(StandartBarLength) },
            { "ArticleId", typeof(ProfileArticle) }
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

        #region Context Menu Handlers

        private void DataGrid_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CommitEdit();
            if (sender is DataGrid dataGrid)
            {
                var dependencyObject = e.OriginalSource as DependencyObject;
                
                var cell = FindVisualParent<DataGridCell>(dependencyObject);
                if (cell != null && !cell.IsSelected)
                {
                    dataGrid.UnselectAll();
                    dataGrid.SelectedCells.Clear();
                    dataGrid.CurrentCell = new DataGridCellInfo(cell);
                    dataGrid.SelectedCells.Add(dataGrid.CurrentCell);
                    var row = FindVisualParent<DataGridRow>(cell);
                    if (row != null)
                    {
                        row.IsSelected = true;
                    }
                }

                var currentRow = FindVisualParent<DataGridRow>(dependencyObject);
                if (currentRow != null)
                {
                    UpdateContextMenuForDataGrid(dataGrid, dependencyObject);
                }
            }
        }

        private void UpdateContextMenuForDataGrid(DataGrid dataGrid, DependencyObject sourceElement)
        {
            var existingMenuItem = dataGrid.ContextMenu?.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "MenuItem_OpenParentTable");
            if (existingMenuItem != null)
            {
                dataGrid.ContextMenu.Items.Remove(existingMenuItem);
            }

            var cell = FindVisualParent<DataGridCell>(sourceElement);
            if (cell != null)
            {
                var column = cell.Column;
                string bindingPath = null;

                if (column is DataGridComboBoxColumn comboBoxCol)
                {
                    bindingPath = comboBoxCol.SelectedValueBinding is System.Windows.Data.Binding b ? b.Path.Path : null;
                }
                else if (column is DataGridBoundColumn boundColumn && boundColumn.Binding is System.Windows.Data.Binding binding)
                {
                    bindingPath = binding.Path.Path;
                }

                bool isFkColumn = !string.IsNullOrEmpty(bindingPath) && bindingPath.EndsWith("Id");

                if (isFkColumn)
                {
                    var prop = _entityType.GetProperty(bindingPath);
                    if (prop != null && IsForeignKey(prop))
                    {
                        var parentType = GetParentEntityType(prop);
                        if (parentType != null)
                        {
                            var parentDisplayName = DisplayNameProvider.GetTypeName(parentType.Name);
                            var capturedPropName = bindingPath;
                            var capturedContextItem = cell.DataContext;
                            
                            var openTableMenuItem = new MenuItem
                            {
                                Header = $"Открыть таблицу \"{parentDisplayName}\"",
                                Name = "MenuItem_OpenParentTable",
                                Icon = new TextBlock { Text = "📂" }
                            };
                            openTableMenuItem.Click += (s, ev) => OpenParentTableFromCell(dataGrid, capturedPropName, capturedContextItem);

                            var editMenuItem = dataGrid.ContextMenu?.Items.OfType<MenuItem>().FirstOrDefault(m => m.Header.ToString() == "Редактировать");
                            if (editMenuItem != null)
                            {
                                var editIndex = dataGrid.ContextMenu.Items.IndexOf(editMenuItem);
                                dataGrid.ContextMenu.Items.Insert(editIndex + 1, openTableMenuItem);
                            }
                            else
                            {
                                dataGrid.ContextMenu?.Items.Add(openTableMenuItem);
                            }
                        }
                    }
                }
            }
        }

        private void ContextMenu_Edit_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is DataGrid dataGrid)
            {
                var itemToEdit = dataGrid.SelectedItem;
                if (itemToEdit == null && dataGrid.SelectedCells.Count > 0)
                {
                    itemToEdit = dataGrid.SelectedCells[0].Item;
                }

                // Получаем реальную модель из коллекции, а не DataRowView
                var modelItem = GetSourceItemFromDataGrid(itemToEdit, dataGrid);
                if (modelItem == null)
                {
                    MessageBox.Show("Выберите строку для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var lookupCollection = GetLookupCollectionForEntityType(modelItem.GetType());
                var editorWindow = new ModelEditorWindow(modelItem, lookupCollection, _allCollections, _dataService, _dbPath)
                {
                    Owner = Window.GetWindow(this)
                };

                if (editorWindow.ShowDialog() == true)
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                    _actionTracker.DetectAndRecordChanges();
                    MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;
                }
            }
        }

        /// <summary>
        /// Получает реальный объект модели из DataGrid, корректно обрабатывая DataRowView.
        /// </summary>
        private object GetSourceItemFromDataGrid(object dataGridItem, DataGrid dataGrid)
        {
            if (dataGridItem == null) return null;
            
            // Если это DataRowView (из TrackingTable), получаем индекс и берём из исходной коллекции
            if (dataGridItem is System.Data.DataRowView rowView)
            {
                var table = rowView.Row.Table;
                if (table != null)
                {
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        if (table.Rows[i] == rowView.Row)
                        {
                            return (i >= 0 && i < _collection.Count) ? _collection[i] : null;
                        }
                    }
                }
                return null;
            }

            // Обычный объект модели — проверяем, что он есть в коллекции
            foreach (var item in _collection)
            {
                if (item == dataGridItem) return item;
            }
            return null;
        }

        /// <summary>
        /// Возвращает lookup-коллекцию для типа объекта (универсальный метод).
        /// </summary>
        private IEnumerable GetLookupCollectionForEntityType(Type type)
        {
            if (_allCollections == null) return null;
            _allCollections.TryGetValue(type, out var collection);
            return collection;
        }

        private void OpenParentTableFromCell(DataGrid dataGrid, string propName, object contextItem = null)
        {
            if (string.IsNullOrEmpty(propName))
            {
                MessageBox.Show("Не удалось определить свойство связи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var selectedItem = contextItem ?? dataGrid.SelectedItem;
            
            if (selectedItem == null && dataGrid.SelectedCells.Count > 0)
            {
                selectedItem = dataGrid.SelectedCells[0].Item;
            }

            if (selectedItem == null)
            {
                MessageBox.Show("Выберите строку для открытия таблицы.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Используем GetParentEntityType вместо дублирующегося словаря
            var prop = _entityType.GetProperty(propName);
            if (prop == null) return;

            var parentType = GetParentEntityType(prop);
            if (parentType == null) return;

            var parentCollection = GetCollectionForType(parentType);
            if (parentCollection == null)
            {
                MessageBox.Show($"Коллекция для типа {parentType.Name} не найдена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

    // Получаем значение FK из selectedItem, учитывая что он может быть DataRowView
    object propValue = GetFKValueFromItem(selectedItem, propName);
    
    int currentId = 0;
    if (propValue != null && propValue != DBNull.Value)
    {
        if (propValue is int intVal) currentId = intVal;
        else if (propValue is short shortVal) currentId = shortVal;
        else if (propValue is long longVal) currentId = (int)longVal;
        else if (int.TryParse(propValue.ToString(), out var parsedId)) currentId = parsedId;
    }

    object parentItem = null;
    if (currentId > 0)
    {
        foreach (var item in parentCollection)
        {
            var itemProp = item.GetType().GetProperty("Id");
            if (itemProp != null && itemProp.GetValue(item) is int id && id == currentId)
            {
                parentItem = item;
                break;
            }
        }
    }

    // Если parentItem == null (ячейка пустая или запись не найдена), 
    // всё равно открываем родительскую таблицу, но без выбранной записи
    _openParentTableCallback?.Invoke(parentItem, parentType);
        }

        /// <summary>
        /// Получает значение FK-ячейки из selectedItem, корректно обрабатывая DataRowView.
        /// </summary>
        private object GetFKValueFromItem(object item, string columnName)
        {
            // Если это DataRowView (из TrackingTable), используем индексатор
            if (item is System.Data.DataRowView rowView)
            {
                if (rowView.Row.Table.Columns.Contains(columnName))
                {
                    return rowView[columnName];
                }
                return null;
            }

            // Если это обычный объект модели, используем рефлексию
            var prop = item.GetType().GetProperty(columnName);
            return prop?.GetValue(item);
        }


        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = Media.VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            return parentObject is T parent ? parent : FindVisualParent<T>(parentObject);
        }

        #endregion

        private void MainDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            CommitEdit();
        }

        private void MainDataGrid_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            var order = MainDataGrid.Columns.Select(c => GetColumnPropertyName(c)).ToList();
            _currentColumnOrder = order;
            ColumnOrderStore.SaveOrder(_entityType.Name, order);
            Console.WriteLine($"[ColumnOrder] Saved order for {_entityType.Name}: {string.Join(", ", order)}");
        }

        #region Add / Delete Row

        private void ContextMenu_AddRow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CommitEdit();
                var newItem = Activator.CreateInstance(_entityType);
                _collection.Add(newItem);
                _actionTracker.DetectAndRecordChanges();
                MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;

                MainDataGrid.SelectedIndex = _collection.Count - 1;
                MainDataGrid.ScrollIntoView(newItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении строки:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ContextMenu_DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is DataGrid dataGrid)
            {
                DeleteSelectedRowsWithDataGrid(dataGrid);
            }
        }

        private void DeleteSelectedRowsWithDataGrid(DataGrid dataGrid)
        {
            CommitEdit();
            var indicesToDelete = new HashSet<int>();

            // Собираем уникальные индексы строк для удаления
            if (dataGrid.SelectedItems.Count > 0)
            {
                foreach (var item in dataGrid.SelectedItems)
                {
                    int index = GetSourceIndexFromDataGridItem(item, dataGrid);
                    if (index >= 0)
                    {
                        indicesToDelete.Add(index);
                    }
                }
            }
            else if (dataGrid.SelectedItem != null)
            {
                int index = GetSourceIndexFromDataGridItem(dataGrid.SelectedItem, dataGrid);
                if (index >= 0)
                {
                    indicesToDelete.Add(index);
                }
            }
            else if (dataGrid.SelectedCells.Count > 0)
            {
                foreach (var cell in dataGrid.SelectedCells)
                {
                    int index = GetSourceIndexFromDataGridItem(cell.Item, dataGrid);
                    if (index >= 0)
                    {
                        indicesToDelete.Add(index);
                    }
                }
            }

            if (indicesToDelete.Count == 0) return;

            // Удаляем элементы по индексам (в обратном порядке, чтобы индексы не сдвигались)
            var sortedIndices = indicesToDelete.OrderByDescending(i => i).ToList();
            foreach (var index in sortedIndices)
            {
                if (index >= 0 && index < _collection.Count)
                {
                    _collection.RemoveAt(index);
                }
            }
            _actionTracker.DetectAndRecordChanges();
            MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;
        }

        /// <summary>
        /// Получает индекс элемента в исходной коллекции из элемента DataGrid.
        /// Корректно обрабатывает DataRowView (из TrackingTable) и обычные объекты модели.
        /// </summary>
        private int GetSourceIndexFromDataGridItem(object item, DataGrid dataGrid)
        {
            // Если это DataRowView (из TrackingTable), получаем индекс строки
            if (item is System.Data.DataRowView rowView)
            {
                // Находим индекс строки в DataTable, сравнивая по ссылке
                var table = rowView.Row.Table;
                if (table != null)
                {
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        if (table.Rows[i] == rowView.Row)
                        {
                            return i;
                        }
                    }
                }
                return -1;
            }

            // Если это обычный объект модели, ищем его в коллекции
            return _collection.IndexOf(item);
        }

        #endregion

        #region Copy / Paste Excel

        private void ContextMenu_Copy_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is DataGrid dataGrid)
            {
                if (dataGrid.SelectedCells.Count > 0)
                {
                    CopySelectedCells(dataGrid);
                }
                else if (dataGrid.SelectedItem != null)
                {
                    CopySelectedItemToClipboard(dataGrid.SelectedItem);
                }
                else
                {
                    MessageBox.Show("Выберите ячейку или строку для копирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        public void CopySelectedCells(DataGrid dataGrid)
        {
            var selectedCells = dataGrid.SelectedCells;
            if (selectedCells.Count == 0) return;

            var validCells = selectedCells.Where(c => c.IsValid).ToList();
            if (validCells.Count == 0) return;

            int minRow = int.MaxValue, maxRow = int.MinValue;
            int minCol = int.MaxValue, maxCol = int.MinValue;

            foreach (var cell in validCells)
            {
                int row = dataGrid.Items.IndexOf(cell.Item);
                int col = dataGrid.Columns.IndexOf(cell.Column);
                if (row >= 0 && col >= 0)
                {
                    minRow = Math.Min(minRow, row);
                    maxRow = Math.Max(maxRow, row);
                    minCol = Math.Min(minCol, col);
                    maxCol = Math.Max(maxCol, col);
                }
            }

            var sb = new System.Text.StringBuilder();
            for (int row = minRow; row <= maxRow; row++)
            {
                if (row > minRow) sb.AppendLine();
                for (int col = minCol; col <= maxCol; col++)
                {
                    if (col > minCol) sb.Append('\t');

                    var item = dataGrid.Items[row];
                    var column = dataGrid.Columns[col];

                    string cellValue = GetDisplayedCellValueForCopy(item, column, dataGrid);
                    cellValue = cellValue.Replace("\"", "\"\"");
                    if (cellValue.Contains('\t') || cellValue.Contains('\n') || cellValue.Contains('"'))
                    {
                        cellValue = "\"" + cellValue + "\"";
                    }
                    sb.Append(cellValue);
                }
            }

            Clipboard.SetText(sb.ToString());
        }

        private void CopySelectedItemToClipboard(object item)
        {
            var type = item.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var sb = new System.Text.StringBuilder();
            bool first = true;

            foreach (var prop in properties)
            {
                if (prop.Name == "Id" || prop.Name == "Position" || prop.Name == "Info") continue;
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string)) continue;

                if (!first) sb.Append('\t');
                first = false;

                var value = prop.GetValue(item)?.ToString() ?? "";
                value = value.Replace("\"", "\"\"");
                if (value.Contains('\t') || value.Contains('\n') || value.Contains('"'))
                {
                    value = "\"" + value + "\"";
                }
                sb.Append(value);
            }

            var headerSb = new System.Text.StringBuilder();
            first = true;
            foreach (var prop in properties)
            {
                if (prop.Name == "Id" || prop.Name == "Position" || prop.Name == "Info") continue;
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string)) continue;

                if (!first) headerSb.Append('\t');
                first = false;
                headerSb.Append(ColumnSettings.GetColumnName(_entityType.Name, prop.Name));
            }

            Clipboard.SetText(headerSb.AppendLine().Append(sb).ToString());
        }

        private string GetDisplayedCellValue(object item, DataGridColumn column)
        {
            if (column is DataGridComboBoxColumn comboCol)
            {
                var binding = comboCol.SelectedValueBinding as System.Windows.Data.Binding;
                if (binding?.Path?.Path != null)
                {
                    return item.GetType().GetProperty(binding.Path.Path)?.GetValue(item)?.ToString() ?? "";
                }
            }
            else if (column is DataGridBoundColumn boundCol)
            {
                var binding = boundCol.Binding as System.Windows.Data.Binding;
                if (binding?.Path?.Path != null)
                {
                    return item.GetType().GetProperty(binding.Path.Path)?.GetValue(item)?.ToString() ?? "";
                }
            }
            return "";
        }

        /// <summary>
        /// Получает значение ячейки для копирования. Для FK-колонок возвращает Name вместо Id.
        /// </summary>
        private string GetDisplayedCellValueForCopy(object item, DataGridColumn column, DataGrid dataGrid)
        {
            if (column is DataGridComboBoxColumn comboCol)
            {
                var binding = comboCol.SelectedValueBinding as System.Windows.Data.Binding;
                if (binding?.Path?.Path != null)
                {
                    string propName = binding.Path.Path;
                    object fkIdValue = null;
                    if (propName.EndsWith("Id"))
                    {
                        var propInfo = _entityType.GetProperty(propName);
                        fkIdValue = item.GetType().GetProperty(propName)?.GetValue(item);

                        if (propInfo != null)
                        {
                            var parentType = GetParentEntityType(propInfo);
                            var parentCollection = GetCollectionForType(parentType);

                            if (parentCollection != null)
                            {
                                int currentId = 0;
                                if (fkIdValue is int intVal) currentId = intVal;
                                else if (fkIdValue is int nullableVal && nullableVal != 0) currentId = nullableVal;

                                if (currentId == 0) return "";

                                foreach (var parentItem in parentCollection)
                                {
                                    var idProp = parentItem.GetType().GetProperty("Id");
                                    if (idProp != null && idProp.GetValue(parentItem) is int parentId && parentId == currentId)
                                    {
                                        var nameProp = parentItem.GetType().GetProperty("Name");
                                        return nameProp?.GetValue(parentItem)?.ToString() ?? "";
                                    }
                                }
                            }
                        }
                        return fkIdValue?.ToString() ?? "";
                    }
                    return fkIdValue?.ToString() ?? "";
                }
            }
            else if (column is DataGridBoundColumn boundCol)
            {
                var binding = boundCol.Binding as System.Windows.Data.Binding;
                if (binding?.Path?.Path != null)
                {
                    return item.GetType().GetProperty(binding.Path.Path)?.GetValue(item)?.ToString() ?? "";
                }
            }
            return "";
        }

        private void ContextMenu_PasteExcel_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is DataGrid dataGrid)
            {
                PasteFromClipboard(dataGrid);
            }
        }

        public void PasteFromClipboard(DataGrid dataGrid)
        {
            CommitEdit();
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show("Буфер обмена не содержит текстовых данных.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string clipboardText = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                MessageBox.Show("Буфер обмена пуст.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rows = ParseTSV(clipboardText);
            if (rows.Count == 0)
            {
                MessageBox.Show("Не удалось распарсить данные из буфера обмена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int startRow = -1, startCol = -1;

            if (dataGrid.SelectedCells.Count > 0)
            {
                var firstCell = dataGrid.SelectedCells[0];
                startRow = dataGrid.Items.IndexOf(firstCell.Item);
                startCol = dataGrid.Columns.IndexOf(firstCell.Column);
            }
            else if (dataGrid.SelectedItem != null)
            {
                startRow = dataGrid.Items.IndexOf(dataGrid.SelectedItem);
                startCol = 0;
            }

            bool isAddingNewRows = startRow < 0;
            if (isAddingNewRows) startRow = _collection.Count;

            var visibleColumns = GetVisibleColumns(dataGrid);

            try
            {
                for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
                {
                    int targetRow = startRow + rowIdx;
                    var rowCells = rows[rowIdx];

                    object item;

                    if (targetRow < _collection.Count)
                    {
                        item = _collection[targetRow];
                    }
                    else
                    {
                        item = Activator.CreateInstance(_entityType);
                        _collection.Add(item);
                    }

                    for (int colIdx = 0; colIdx < rowCells.Count && (startCol + colIdx) < visibleColumns.Count; colIdx++)
                    {
                        int targetColAbs = startCol + colIdx;
                        string columnName = visibleColumns[targetColAbs];

                        if (columnName == "Id" || columnName == "Position" || columnName == "Info") continue;

                        var prop = _entityType.GetProperty(columnName);
                        if (prop == null) continue;

                        string cellValue = rowCells[colIdx].Trim();
                        SetPropertyValue(item, prop, cellValue);
                    }
                }

                // Фиксируем изменения
                _actionTracker.DetectAndRecordChanges();
                MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;

                if (startRow + rows.Count > _collection.Count)
                {
                    dataGrid.SelectedIndex = Math.Max(0, _collection.Count - 1);
                }

                RefreshDataGrid(dataGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при вставке данных:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<List<string>> ParseTSV(string text)
        {
            var rows = new List<List<string>>();

            string[] lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cells = new List<string>();
                bool inQuotes = false;
                var cell = new System.Text.StringBuilder();

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];

                    if (inQuotes)
                    {
                        if (c == '"')
                        {
                            if (i + 1 < line.Length && line[i + 1] == '"')
                            {
                                cell.Append('"');
                                i++;
                            }
                            else
                            {
                                inQuotes = false;
                            }
                        }
                        else
                        {
                            cell.Append(c);
                        }
                    }
                    else
                    {
                        if (c == '"')
                        {
                            inQuotes = true;
                        }
                        else if (c == '\t')
                        {
                            cells.Add(cell.ToString());
                            cell.Clear();
                        }
                        else
                        {
                            cell.Append(c);
                        }
                    }
                }

                cells.Add(cell.ToString());
                rows.Add(cells);
            }

            if (rows.Count > 0 && IsHeaderRow(rows[0]))
            {
                rows.RemoveAt(0);
            }

            return rows;
        }

        private bool IsHeaderRow(List<string> row)
        {
            var headerKeywords = new[] { "название", "имя", "name", "код", "code", "описание", "тип" };
            var lowerRow = row.ConvertAll(s => s.ToLower());

            foreach (var keyword in headerKeywords)
            {
                if (lowerRow.Any(s => s.Contains(keyword)))
                    return true;
            }

            return false;
        }

        private List<string> GetVisibleColumns(DataGrid dataGrid)
        {
            var columns = new List<string>();
            var properties = _entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (prop.Name == "Id" || prop.Name == "Position" || prop.Name == "Info") continue;
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string)) continue;

                var columnInGrid = dataGrid.Columns.Cast<DataGridColumn>()
                    .FirstOrDefault(c => GetPropertyNameFromColumn(c) == prop.Name);

                if (columnInGrid?.Visibility == Visibility.Visible)
                {
                    columns.Add(prop.Name);
                }
            }

            return columns;
        }

        private string GetPropertyNameFromColumn(DataGridColumn column)
        {
            if (column is DataGridComboBoxColumn comboCol)
            {
                return (comboCol.SelectedValueBinding as System.Windows.Data.Binding)?.Path?.Path ?? "";
            }
            else if (column is DataGridBoundColumn boundCol)
            {
                return (boundCol.Binding as System.Windows.Data.Binding)?.Path?.Path ?? "";
            }
            return "";
        }

        private void SetPropertyValue(object item, System.Reflection.PropertyInfo prop, string value)
        {
            var propType = prop.PropertyType;

            if (propType == typeof(int?) || propType == typeof(int))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (propType == typeof(int?)) prop.SetValue(item, null);
                    else prop.SetValue(item, 0);
                    return;
                }

                if (value.All(char.IsDigit) && int.TryParse(value, out int id))
                {
                    prop.SetValue(item, id);
                    return;
                }

                if (IsForeignKey(prop))
                {
                    var parentType = GetParentEntityType(prop);
                    var parentCollection = parentType != null ? GetCollectionForType(parentType) : null;

                    if (parentCollection != null)
                    {
                        foreach (var parentItem in parentCollection)
                        {
                            var nameProp = parentItem.GetType().GetProperty("Name");
                            if (nameProp != null && nameProp.GetValue(parentItem)?.ToString() == value)
                            {
                                var idProp = parentItem.GetType().GetProperty("Id");
                                if (idProp != null)
                                {
                                    prop.SetValue(item, idProp.GetValue(parentItem));
                                    return;
                                }
                            }
                        }
                    }
                }

                if (int.TryParse(value, out int result))
                {
                    prop.SetValue(item, result);
                }
            }
            else if (propType == typeof(double?) || propType == typeof(double))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (propType == typeof(double?)) prop.SetValue(item, null);
                    else prop.SetValue(item, 0.0);
                    return;
                }

                if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture, out double result))
                {
                    prop.SetValue(item, result);
                }
                else if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out result))
                {
                    prop.SetValue(item, result);
                }
            }
            else
            {
                prop.SetValue(item, value);
            }
        }

        #endregion

        #region Control API Methods

        public void CommitEdit()
        {
            if (MainDataGrid == null) return;
            
            bool cellCommit = MainDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            bool rowCommit = MainDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            if (!cellCommit) MainDataGrid.CancelEdit(DataGridEditingUnit.Cell);
            if (!rowCommit) MainDataGrid.CancelEdit(DataGridEditingUnit.Row);
        }

        public void RefreshFromCollection(IList collection, Dictionary<Type, IEnumerable> allCollections, IDictionaryDataService dataService)
        {
            _collection = collection;
            _allCollections = allCollections;
            _dataService = dataService;

            ExtractLookupCollections();

            _actionTracker = new EditActionTracker(collection, _entityType);
            _actionTracker.StateChanged += OnTrackerStateChanged;
            _actionTracker.Initialize();

            MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(SearchTextBox.Text);
        }

        public void ApplyFilter(string filterText, string columnDisplayName = null)
        {
            if (_actionTracker?.TrackingTable == null) return;
            var dataView = _actionTracker.TrackingTable.DefaultView;
            if (dataView == null) return;

            filterText = filterText ?? "";

            if (string.IsNullOrWhiteSpace(filterText))
            {
                dataView.RowFilter = "";
            }
            else
            {
                var excludedColumns = new[] { "Id", "Position", "Info", "Timestamp", "Deleted" };
                var allColumns = _actionTracker.TrackingTable.Columns.Cast<DataColumn>()
                    .Where(col => !excludedColumns.Contains(col.ColumnName))
                    .ToList();

                if (!string.IsNullOrEmpty(columnDisplayName))
                {
                    var col = allColumns.FirstOrDefault(c => DisplayNameProvider.GetPropertyName(c.ColumnName) == columnDisplayName);
                    if (col != null)
                    {
                        var filterExpr = BuildFilterExpression(col, filterText);
                        if (!string.IsNullOrEmpty(filterExpr))
                            dataView.RowFilter = filterExpr;
                    }
                }
                else
                {
                    var stringColumns = allColumns
                        .Where(col => col.DataType == typeof(string) || col.DataType == typeof(object))
                        .ToList();

                    if (stringColumns.Count > 0)
                    {
                        var filterExpression = string.Join(" OR ",
                            stringColumns.Select(col => BuildFilterExpression(col, filterText)));
                        dataView.RowFilter = filterExpression;
                    }
                }
            }

            MainDataGrid.ItemsSource = dataView;
        }

        private string BuildFilterExpression(DataColumn col, string filterText)
        {
            if (col.DataType == typeof(string) || col.DataType == typeof(object))
                return $"[{col.ColumnName}] LIKE '%{filterText}%'";
            
            if (col.DataType == typeof(int) || col.DataType == typeof(double) || col.DataType == typeof(decimal) || col.DataType == typeof(float))
            {
                if (double.TryParse(filterText.Replace(",", "."), out double numValue))
                    return $"[{col.ColumnName}] = {numValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
            
            return null;
        }

        public List<(string Name, string DisplayName)> GetFilterableColumnNames()
        {
            if (_actionTracker?.TrackingTable == null) return new List<(string, string)>();

            var excludedColumns = new[] { "Id", "Position", "Info", "Timestamp", "Deleted" };
            return _actionTracker.TrackingTable.Columns.Cast<DataColumn>()
                .Where(col => !excludedColumns.Contains(col.ColumnName))
                .Select(col => (col.ColumnName, DisplayNameProvider.GetPropertyName(col.ColumnName)))
                .ToList();
        }

        #endregion

        #region Keyboard Shortcuts

        private void DataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid == null) return;

            bool isEditing = e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase 
                          || e.OriginalSource is ComboBox 
                          || e.OriginalSource is CheckBox;

            // Обработка Enter — жесткое завершение редактирования и переход вниз
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (isEditing)
                {
                    // Явно коммитим ячейку и строку (закрываем TextBox, курсор перестает мигать)
                    dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                    // Отбираем фокус у TextBox и возвращаем его таблице
                    Keyboard.Focus(dataGrid);

                    // Переходим на строку вниз
                    MoveCellFocus(dataGrid, Key.Down);
                }
                else
                {
                    // Если просто нажали Enter на выделенной ячейке (не в режиме редактирования) - переходим вниз
                    MoveCellFocus(dataGrid, Key.Down);
                }
                e.Handled = true;
                return;
            }

            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            // Горячие клавиши теперь обрабатываются на уровне Grid
            // Но оставляем навигацию стрелками, когда фокус на DataGrid
            if (!isEditing && (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down))
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                {
                    MoveCellFocus(dataGrid, e.Key);
                    e.Handled = true;
                }
            }
        }

        private void MoveCellFocus(DataGrid dataGrid, Key key)
        {
            if (dataGrid.Items.Count == 0 || dataGrid.Columns.Count == 0) return;

            int currentRow = -1;
            int currentCol = -1;

            if (dataGrid.CurrentCell.IsValid)
            {
                currentRow = dataGrid.Items.IndexOf(dataGrid.CurrentCell.Item);
                currentCol = dataGrid.Columns.IndexOf(dataGrid.CurrentCell.Column);
            }
            else if (dataGrid.SelectedCells.Count > 0)
            {
                currentRow = dataGrid.Items.IndexOf(dataGrid.SelectedCells[0].Item);
                currentCol = dataGrid.Columns.IndexOf(dataGrid.SelectedCells[0].Column);
            }

            if (currentRow < 0) currentRow = 0;
            if (currentCol < 0) currentCol = 0;

            switch (key)
            {
                case Key.Left:
                    currentCol--;
                    break;
                case Key.Right:
                    currentCol++;
                    break;
                case Key.Up:
                    currentRow--;
                    break;
                case Key.Down:
                    currentRow++;
                    break;
            }

            if (currentRow < 0) currentRow = 0;
            if (currentRow >= dataGrid.Items.Count) currentRow = dataGrid.Items.Count - 1;
            if (currentCol < 0) currentCol = 0;
            if (currentCol >= dataGrid.Columns.Count) currentCol = dataGrid.Columns.Count - 1;

            var newCellInfo = new DataGridCellInfo(dataGrid.Items[currentRow], dataGrid.Columns[currentCol]);
            
            // Очищаем старое выделение ячеек и переносим фокус на новую
            dataGrid.SelectedCells.Clear();
            dataGrid.CurrentCell = newCellInfo;
            dataGrid.SelectedCells.Add(newCellInfo);
            
            dataGrid.ScrollIntoView(dataGrid.Items[currentRow], dataGrid.Columns[currentCol]);
        }

        private void DeleteSelectedRows(DataGrid dataGrid)
        {
            CommitEdit();
            var indicesToDelete = new HashSet<int>();

            // Сначала проверяем обычные выделенные строки
            if (dataGrid.SelectedItems.Count > 0)
            {
                foreach (var item in dataGrid.SelectedItems)
                {
                    int index = GetSourceIndexFromDataGridItem(item, dataGrid);
                    if (index >= 0)
                    {
                        indicesToDelete.Add(index);
                    }
                }
            }
            // Если нет выделенных строк, используем SelectedCells
            else if (dataGrid.SelectedCells.Count > 0)
            {
                foreach (var cell in dataGrid.SelectedCells)
                {
                    int index = GetSourceIndexFromDataGridItem(cell.Item, dataGrid);
                    if (index >= 0)
                    {
                        indicesToDelete.Add(index);
                    }
                }
            }
            // Если и этого нет, используем CurrentCell
            else if (dataGrid.CurrentCell.IsValid)
            {
                int index = GetSourceIndexFromDataGridItem(dataGrid.CurrentCell.Item, dataGrid);
                if (index >= 0)
                {
                    indicesToDelete.Add(index);
                }
            }

            if (indicesToDelete.Count == 0) return;

            var sortedIndices = indicesToDelete.OrderByDescending(i => i).ToList();
            foreach (var index in sortedIndices)
            {
                if (index >= 0 && index < _collection.Count)
                {
                    _collection.RemoveAt(index);
                }
            }
            _actionTracker.DetectAndRecordChanges();
            MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;
        }

        private void DuplicateSelectedRow()
        {
            CommitEdit();
            
            // Получаем элемент для дублирования — сначала из SelectedItem, потом из CurrentCell
            object sourceItem = MainDataGrid.SelectedItem;
            if (sourceItem == null && MainDataGrid.CurrentCell.IsValid)
            {
                sourceItem = MainDataGrid.CurrentCell.Item;
            }
            if (sourceItem == null && MainDataGrid.SelectedCells.Count > 0)
            {
                sourceItem = MainDataGrid.SelectedCells[0].Item;
            }
            if (sourceItem == null) return;

            try
            {
                var newItem = Activator.CreateInstance(_entityType);
                var properties = _entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    if (prop.Name == "Id") continue;

                    if (prop.CanWrite && prop.CanRead)
                    {
                        var value = prop.GetValue(sourceItem);
                        prop.SetValue(newItem, value);
                    }
                }

                _collection.Add(newItem);
                _actionTracker.DetectAndRecordChanges();
                MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;
                MainDataGrid.SelectedItem = newItem;
                MainDataGrid.ScrollIntoView(newItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при дублировании строки:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private object CloneItem(object source)
        {
            if (source == null) return null;
            var clone = Activator.CreateInstance(source.GetType());
            var properties = _entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    try
                    {
                        prop.SetValue(clone, prop.GetValue(source));
                    }
                    catch { }
                }
            }
            return clone;
        }

        private void RefreshDataGrid(DataGrid dataGrid)
        {
            if (dataGrid?.Items != null)
            {
                dataGrid.Items.Refresh();
            }
        }

        public void RefreshDataGrid()
        {
            if (MainDataGrid?.Items != null)
            {
                MainDataGrid.Items.Refresh();
            }
        }

        private void OpenEditorForSelectedItem()
        {
            var itemToEdit = MainDataGrid.SelectedItem;
            if (itemToEdit == null && MainDataGrid.CurrentCell.IsValid)
            {
                itemToEdit = MainDataGrid.CurrentCell.Item;
            }

            // Получаем реальную модель из коллекции, а не DataRowView
            var modelItem = GetSourceItemFromDataGrid(itemToEdit, MainDataGrid);
            if (modelItem == null) return;

            var lookupCollection = GetLookupCollectionForEntityType(modelItem.GetType());
            var editorWindow = new ModelEditorWindow(modelItem, lookupCollection, _allCollections, _dataService, _dbPath)
            {
                Owner = Window.GetWindow(this)
            };

            if (editorWindow.ShowDialog() == true)
            {
                MainDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                _actionTracker.DetectAndRecordChanges();
                MainDataGrid.ItemsSource = _actionTracker.TrackingTable.DefaultView;
            }
        }

        #endregion
    }
}
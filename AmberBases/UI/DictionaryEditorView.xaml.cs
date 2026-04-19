using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Media = System.Windows.Media;
using AmberBases.Core;
using AmberBases.Core.Interfaces;
using AmberBases.Core.Models.Dictionaries;
using AmberBases.Services;
using AmberBases.UI.Tracking;

namespace AmberBases.UI
{
    public partial class DictionaryEditorView : UserControl, IInitControl
    {
        private readonly IDictionaryDataService _dataService;
        private readonly string _dbPath;
        private readonly InterfaceSettingsTracker _settingsTracker;
        private bool _isMaterialsMode;

        // Data collections
        private ObservableCollection<SystemProvider> _systemProviders;
        private ObservableCollection<ProfileSystem> _profileSystems;
        private ObservableCollection<AmberBases.Core.Models.Dictionaries.Color> _colors;
        private ObservableCollection<StandartBarLength> _whipLengths;
        private ObservableCollection<ProfileType> _profileTypes;
        private ObservableCollection<Applicability> _applicabilities;
        private ObservableCollection<ProfileArticle> _profileArticles;
        private ObservableCollection<CoatingType> _coatingTypes;
        private ObservableCollection<CProfile> _cProfiles;


        public DictionaryEditorView()
        {
            InitializeComponent();
            _dataService = new SqliteDictionaryDataService();
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseConfig.SqliteDictionariesDatabaseFile);
            _settingsTracker = App.GetSettingsTracker();
            
            // Ensure DB is initialized
            _dataService.InitializeDatabase(_dbPath);
        }

        public InterfaceSettingsTracker SettingsTracker => _settingsTracker;

        public void InitControl()
        {
            // Инициализируем отслеживание высоты панели справочников
            var splitter = FindName("DictionarySelectorSplitter") as GridSplitter;
            var rowDef = FindName("DictionarySelectorRow") as RowDefinition;
            var panel = FindName("DictionarySelectorBorder") as Border;
            if (splitter != null && rowDef != null && panel != null)
            {
                _settingsTracker.TrackPanelHeight(panel, rowDef, splitter, "DictionaryEditor");
            }
            
            // LoadData должен выполниться асинхронно, чтобы избежать конфликта с Dispatcher
            Dispatcher.BeginInvoke(new Action(LoadData), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void LoadData()
        {
            try
            {
                _systemProviders = new ObservableCollection<SystemProvider>(_dataService.GetSystemProviders(_dbPath));
                _profileSystems = new ObservableCollection<ProfileSystem>(_dataService.GetProfileSystems(_dbPath));
                _colors = new ObservableCollection<AmberBases.Core.Models.Dictionaries.Color>(_dataService.GetColors(_dbPath));
                _whipLengths = new ObservableCollection<StandartBarLength>(_dataService.GetStandartBarLengths(_dbPath));
                _profileTypes = new ObservableCollection<ProfileType>(_dataService.GetProfileTypes(_dbPath));
                _applicabilities = new ObservableCollection<Applicability>(_dataService.GetApplicabilities(_dbPath));
                _profileArticles = new ObservableCollection<ProfileArticle>(_dataService.GetProfileArticles(_dbPath));
                _coatingTypes = new ObservableCollection<CoatingType>(_dataService.GetCoatingTypes(_dbPath));
                _cProfiles = new ObservableCollection<CProfile>(_dataService.GetCProfiles(_dbPath));

                // Map dictionaries for looking up FK references
                var editorsCollections = GetAllCollectionsForEditor();
                var dbContext = _dataService;

                // Configure and initialize each control
                SystemProvidersGrid.Initialize(typeof(SystemProvider), _systemProviders, editorsCollections, dbContext, _dbPath, OpenParentTable);
                ProfileSystemsGrid.Initialize(typeof(ProfileSystem), _profileSystems, editorsCollections, dbContext, _dbPath, OpenParentTable);
                ColorsGrid.Initialize(typeof(Color), _colors, editorsCollections, dbContext, _dbPath, OpenParentTable);
                StandartBarLengthsGrid.Initialize(typeof(StandartBarLength), _whipLengths, editorsCollections, dbContext, _dbPath, OpenParentTable);
                ProfileTypesGrid.Initialize(typeof(ProfileType), _profileTypes, editorsCollections, dbContext, _dbPath, OpenParentTable);
                ApplicabilitiesGrid.Initialize(typeof(Applicability), _applicabilities, editorsCollections, dbContext, _dbPath, OpenParentTable);
                ProfileArticlesGrid.Initialize(typeof(ProfileArticle), _profileArticles, editorsCollections, dbContext, _dbPath, OpenParentTable);
                CoatingTypesGrid.Initialize(typeof(CoatingType), _coatingTypes, editorsCollections, dbContext, _dbPath, OpenParentTable);
                CProfilesGrid.Initialize(typeof(CProfile), _cProfiles, editorsCollections, dbContext, _dbPath, OpenParentTable);

                // Setup Undo/Redo tracking state
                SystemProvidersGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                ProfileSystemsGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                ColorsGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                StandartBarLengthsGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                ProfileTypesGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                ApplicabilitiesGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                ProfileArticlesGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                CoatingTypesGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                CProfilesGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                
                // Сбрасываем состояние кнопок Undo/Redo после refresh
                OnAnyTrackerStateChanged(false, false);
                
                ApplyFilter(); // Apply current filter if any
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Error loading dictionaries: " + ex.Message));
            }
        }

        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            GetActiveGrid()?.Undo();
        }

        private void RedoBtn_Click(object sender, RoutedEventArgs e)
        {
            GetActiveGrid()?.Redo();
        }

        private DictionaryTableControl GetActiveGrid()
        {
            if (TablesTabControl?.SelectedItem is TabItem tabItem && tabItem.Content is DictionaryTableControl grid)
                return grid;
            return null;
        }

        private void OnAnyTrackerStateChanged(bool canUndo, bool canRedo)
        {
            // Update button states based on the currently visible tracker (call on UI thread)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var activeGrid = GetActiveGrid();
                var undoBtn = FindName("UndoBtn") as Button;
                var redoBtn = FindName("RedoBtn") as Button;
                if (undoBtn != null)
                    undoBtn.IsEnabled = activeGrid?.ActionTracker?.CanUndo ?? false;
                if (redoBtn != null)
                    redoBtn.IsEnabled = activeGrid?.ActionTracker?.CanRedo ?? false;
            }));
        }

        private void TablesTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesTabControl?.SelectedItem is TabItem tabItem)
            {
                // Check for unsaved changes in current grid
                var activeGrid = GetActiveGrid();
                if (activeGrid?.ActionTracker?.HasUnsavedChanges == true)
                {
                    MessageBox.Show(
                        "Есть несохранённые изменения. Сохраните или отмените изменения перед переключением.",
                        "Несохранённые изменения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    // Don't cancel - let the tab switch complete
                    return;
                }

                // Update undo/redo buttons when switching tabs
                OnAnyTrackerStateChanged(false, false);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var activeGrid = GetActiveGrid();
            if (activeGrid == null) return;

            var searchTextBox = activeGrid.FindName("SearchTextBox") as System.Windows.Controls.TextBox;
            var searchColumnCombo = activeGrid.FindName("SearchColumnCombo") as System.Windows.Controls.ComboBox;

            string filterText = searchTextBox?.Text?.ToLower() ?? "";
            string selectedColumnName = null;

            if (searchColumnCombo?.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                var tag = selectedItem.Tag as string;
                if (!string.IsNullOrEmpty(tag))
                {
                    selectedColumnName = tag;
                }
            }

            activeGrid.ApplyFilter(filterText, selectedColumnName);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Keyboard.ClearFocus();

                GetActiveGrid()?.CommitEdit();

                SyncAllCollections();
                
                SystemProvidersGrid.ActionTracker.MarkChangesAsSaved();
                ProfileSystemsGrid.ActionTracker.MarkChangesAsSaved();
                ColorsGrid.ActionTracker.MarkChangesAsSaved();
                StandartBarLengthsGrid.ActionTracker.MarkChangesAsSaved();
                ProfileTypesGrid.ActionTracker.MarkChangesAsSaved();
                ApplicabilitiesGrid.ActionTracker.MarkChangesAsSaved();
                ProfileArticlesGrid.ActionTracker.MarkChangesAsSaved();
                CoatingTypesGrid.ActionTracker.MarkChangesAsSaved();
                CProfilesGrid.ActionTracker.MarkChangesAsSaved();

                RefreshGridsWithCurrentCollections();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving dictionaries: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshGridsWithCurrentCollections()
        {
            var editorsCollections = GetAllCollectionsForEditor();
            var dbContext = _dataService;

            SystemProvidersGrid.RefreshFromCollection(_systemProviders, editorsCollections, dbContext);
            ProfileSystemsGrid.RefreshFromCollection(_profileSystems, editorsCollections, dbContext);
            ColorsGrid.RefreshFromCollection(_colors, editorsCollections, dbContext);
            StandartBarLengthsGrid.RefreshFromCollection(_whipLengths, editorsCollections, dbContext);
            ProfileTypesGrid.RefreshFromCollection(_profileTypes, editorsCollections, dbContext);
            ApplicabilitiesGrid.RefreshFromCollection(_applicabilities, editorsCollections, dbContext);
            ProfileArticlesGrid.RefreshFromCollection(_profileArticles, editorsCollections, dbContext);
            CoatingTypesGrid.RefreshFromCollection(_coatingTypes, editorsCollections, dbContext);
            CProfilesGrid.RefreshFromCollection(_cProfiles, editorsCollections, dbContext);
        }

        private void SyncCollection<T>(ICollection<T> collection) where T : BaseDictionaryModel, new()
        {
            var dbItems = _dataService.GetItems<T>(_dbPath);
            foreach (var item in collection)
            {
                if (item.Id == 0) _dataService.AddItem(item, _dbPath);
                else _dataService.UpdateItem(item, _dbPath);
            }
            var currentIds = collection.Where(x => x.Id > 0).Select(x => x.Id).ToList();
            foreach (var dbItem in dbItems.Where(x => !currentIds.Contains(x.Id)))
            {
                _dataService.DeleteItem<T>(dbItem.Id, _dbPath);
            }
        }

        private void SyncAllCollections()
        {
            SyncCollection(_systemProviders);
            SyncCollection(_profileSystems);
            SyncCollection(_colors);
            SyncCollection(_whipLengths);
            SyncCollection(_profileArticles);
            SyncCollection(_profileTypes);
            SyncCollection(_applicabilities);
            SyncCollection(_coatingTypes);
            SyncCollection(_cProfiles);
        }

        /// <summary>
        /// Возвращает словарь всех коллекций (для передачи в дочерние окна)
        /// </summary>
        private Dictionary<Type, IEnumerable> GetAllCollectionsForEditor()
        {
            return new Dictionary<Type, IEnumerable>
            {
                { typeof(SystemProvider), _systemProviders },
                { typeof(ProfileSystem), _profileSystems },
                { typeof(ProfileType), _profileTypes },
                { typeof(Applicability), _applicabilities },
                { typeof(CoatingType), _coatingTypes },
                { typeof(Color), _colors },
                { typeof(StandartBarLength), _whipLengths },
                { typeof(ProfileArticle), _profileArticles },
                { typeof(CProfile), _cProfiles }
            };
        }

        /// <summary>
        /// Открывает родительскую таблицу в новом окне TableViewWindow.
        /// </summary>
        private void OpenParentTable(object parentItem, Type parentType)
        {
            var editorsCollections = GetAllCollectionsForEditor();
            if (!editorsCollections.ContainsKey(parentType))
            {
                MessageBox.Show($"Коллекция для типа {parentType.Name} не найдена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parentCollection = (IList)editorsCollections[parentType];

            var parentWindow = new TableViewWindow(parentType, parentCollection, editorsCollections)
            {
                Owner = Window.GetWindow(this)
            };

            parentWindow.ShowDialog();
        }

        /// <summary>
        public bool HasAnyUnsavedChanges()
        {
            return HasUnsavedChanges(SystemProvidersGrid) ||
                   HasUnsavedChanges(ProfileSystemsGrid) ||
                   HasUnsavedChanges(ColorsGrid) ||
                   HasUnsavedChanges(StandartBarLengthsGrid) ||
                   HasUnsavedChanges(ProfileTypesGrid) ||
                   HasUnsavedChanges(ApplicabilitiesGrid) ||
                   HasUnsavedChanges(ProfileArticlesGrid) ||
                   HasUnsavedChanges(CoatingTypesGrid) ||
                   HasUnsavedChanges(CProfilesGrid);
        }

        private static bool HasUnsavedChanges(DictionaryTableControl grid)
        {
            return grid?.ActionTracker?.HasUnsavedChanges == true;
        }

        public void SaveAllColumnSettings()
        {
            SystemProvidersGrid?.SaveColumnSettings();
            ProfileSystemsGrid?.SaveColumnSettings();
            ColorsGrid?.SaveColumnSettings();
            StandartBarLengthsGrid?.SaveColumnSettings();
            ProfileTypesGrid?.SaveColumnSettings();
            ApplicabilitiesGrid?.SaveColumnSettings();
            ProfileArticlesGrid?.SaveColumnSettings();
            CoatingTypesGrid?.SaveColumnSettings();
            CProfilesGrid?.SaveColumnSettings();
        }

        /// <summary>
        /// Сохраняет все изменения справочников в базу данных.
        /// </summary>
        public void SaveAllChanges()
        {
            BtnSave_Click(null, null);
        }

        /// <summary>
        /// Сбрасывает все несохранённые изменения, возвращая данные из базы.
        /// </summary>
        public void DiscardAllChanges()
        {
            LoadData();
        }

        /// <summary>
        /// Устанавливает режим отображения только таблицы артикулов профилей (для вкладки "Материалы").
        /// В этом режиме панель переключения таблиц скрыта.
        /// </summary>
        public void SetMaterialsMode(bool isMaterials)
        {
            _isMaterialsMode = isMaterials;
            if (_isMaterialsMode)
            {
                // Скрываем панель переключения таблиц и показываем только артикулы
                if (TablesTabControl != null)
                    TablesTabControl.Visibility = Visibility.Collapsed;
                ShowProfileArticles();
            }
            else
            {
                // Восстанавливаем панель переключения таблиц
                if (TablesTabControl != null)
                    TablesTabControl.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Переключает отображение на таблицу материалов (Profile Articles).
        /// </summary>
        public void ShowProfileArticles()
        {
            // Select the Profile Articles tab
            if (TablesTabControl.Items.Count > 6)
            {
                TablesTabControl.SelectedIndex = 6; // Артикулы Профилей - 7th tab (0-indexed)
            }
            
            // Force update undo/redo buttons
            OnAnyTrackerStateChanged(false, false);
        }
    }
}
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

        // Data collections
        private ObservableCollection<SystemProvider> _systemProviders;
        private ObservableCollection<ProfileSystem> _profileSystems;
        private ObservableCollection<AmberBases.Core.Models.Dictionaries.Color> _colors;
        private ObservableCollection<WhipLength> _whipLengths;
        private ObservableCollection<ProfileType> _profileTypes;
        private ObservableCollection<Applicability> _applicabilities;
        private ObservableCollection<ProfileArticle> _profileArticles;
        private ObservableCollection<CoatingType> _coatingTypes;


        public DictionaryEditorView()
        {
            InitializeComponent();
            _dataService = new SqliteDictionaryDataService();
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseConfig.SqliteDictionariesDatabaseFile);
            _settingsTracker = new InterfaceSettingsTracker();
            
            // Ensure DB is initialized
            _dataService.InitializeDatabase(_dbPath);
        }

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
            
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _systemProviders = new ObservableCollection<SystemProvider>(_dataService.GetSystemProviders(_dbPath));
                _profileSystems = new ObservableCollection<ProfileSystem>(_dataService.GetProfileSystems(_dbPath));
                _colors = new ObservableCollection<AmberBases.Core.Models.Dictionaries.Color>(_dataService.GetColors(_dbPath));
                _whipLengths = new ObservableCollection<WhipLength>(_dataService.GetWhipLengths(_dbPath));
                _profileTypes = new ObservableCollection<ProfileType>(_dataService.GetProfileTypes(_dbPath));
                _applicabilities = new ObservableCollection<Applicability>(_dataService.GetApplicabilities(_dbPath));
                _profileArticles = new ObservableCollection<ProfileArticle>(_dataService.GetProfileArticles(_dbPath));
                _coatingTypes = new ObservableCollection<CoatingType>(_dataService.GetCoatingTypes(_dbPath));

                // Map dictionaries for looking up FK references
                var editorsCollections = GetAllCollectionsForEditor();
                var dbContext = _dataService;

                // Configure and initialize each control
                SystemProvidersGrid.Initialize(typeof(SystemProvider), _systemProviders, editorsCollections, dbContext, _dbPath, OpenParentTable);
                ProfileSystemsGrid.Initialize(typeof(ProfileSystem), _profileSystems, editorsCollections, dbContext, _dbPath, OpenParentTable);
                ColorsGrid.Initialize(typeof(Color), _colors, editorsCollections, dbContext, _dbPath, OpenParentTable);
                WhipLengthsGrid.Initialize(typeof(WhipLength), _whipLengths, editorsCollections, dbContext, _dbPath, OpenParentTable);
                ProfileTypesGrid.Initialize(typeof(ProfileType), _profileTypes, editorsCollections, dbContext, _dbPath, OpenParentTable);
                ApplicabilitiesGrid.Initialize(typeof(Applicability), _applicabilities, editorsCollections, dbContext, _dbPath, OpenParentTable);
                ProfileArticlesGrid.Initialize(typeof(ProfileArticle), _profileArticles, editorsCollections, dbContext, _dbPath, OpenParentTable);
                CoatingTypesGrid.Initialize(typeof(CoatingType), _coatingTypes, editorsCollections, dbContext, _dbPath, OpenParentTable);

                // Setup Undo/Redo tracking state
                SystemProvidersGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                ProfileSystemsGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                ColorsGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                WhipLengthsGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                ProfileTypesGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                ApplicabilitiesGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                ProfileArticlesGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                CoatingTypesGrid.ActionTracker.StateChanged += OnAnyTrackerStateChanged;
                
                // Сбрасываем состояние кнопок Undo/Redo после refresh
                OnAnyTrackerStateChanged(false, false);
                
                ApplyFilter(); // Apply current filter if any
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dictionaries: " + ex.Message);
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
            if (SystemProvidersGrid.Visibility == Visibility.Visible) return SystemProvidersGrid;
            if (ProfileSystemsGrid.Visibility == Visibility.Visible) return ProfileSystemsGrid;
            if (ColorsGrid.Visibility == Visibility.Visible) return ColorsGrid;
            if (WhipLengthsGrid.Visibility == Visibility.Visible) return WhipLengthsGrid;
            if (ProfileTypesGrid.Visibility == Visibility.Visible) return ProfileTypesGrid;
            if (ApplicabilitiesGrid.Visibility == Visibility.Visible) return ApplicabilitiesGrid;
            if (ProfileArticlesGrid.Visibility == Visibility.Visible) return ProfileArticlesGrid;
            if (CoatingTypesGrid.Visibility == Visibility.Visible) return CoatingTypesGrid;
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

        private void BtnShowGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string gridName)
            {
                // Проверяем несохранённые изменения в текущей активной таблице
                var activeGrid = GetActiveGrid();
                if (activeGrid != null && activeGrid.ActionTracker.HasUnsavedChanges)
                {
                    MessageBox.Show(
                        "Есть несохранённые изменения. Сохраните или отмените изменения перед переключением.",
                        "Несохранённые изменения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                SystemProvidersGrid.Visibility = Visibility.Collapsed;
                ProfileSystemsGrid.Visibility = Visibility.Collapsed;
                ColorsGrid.Visibility = Visibility.Collapsed;
                WhipLengthsGrid.Visibility = Visibility.Collapsed;
                ProfileTypesGrid.Visibility = Visibility.Collapsed;
                ApplicabilitiesGrid.Visibility = Visibility.Collapsed;
                ProfileArticlesGrid.Visibility = Visibility.Collapsed;
                CoatingTypesGrid.Visibility = Visibility.Collapsed;

                var grid = this.FindName(gridName) as DictionaryTableControl;
                if (grid != null)
                {
                    grid.Visibility = Visibility.Visible;
                    // Force update undo/redo buttons right away when switching tabs
                    OnAnyTrackerStateChanged(false, false);
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string filterText = SearchTextBox.Text.ToLower();
            
            SystemProvidersGrid.ApplyFilter(filterText, "Name");
            ProfileSystemsGrid.ApplyFilter(filterText, "Name");
            ColorsGrid.ApplyFilter(filterText, "ColorName");
            WhipLengthsGrid.ApplyFilter(filterText, "Length");
            ProfileTypesGrid.ApplyFilter(filterText, "Name");
            ApplicabilitiesGrid.ApplyFilter(filterText, "Name");
            ProfileArticlesGrid.ApplyFilter(filterText, "Article");
            CoatingTypesGrid.ApplyFilter(filterText, "Name");
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Принудительно убираем фокус с текущего элемента, чтобы сработал UpdateSourceTrigger=LostFocus
                Keyboard.ClearFocus();

                // Force commit edit on the active grid
                GetActiveGrid()?.CommitEdit();

                SyncSystemProviders();
                SyncProfileSystems();
                SyncColors();
                SyncWhipLengths();
                SyncProfileTypes();
                SyncApplicabilities();
                SyncProfileArticles();
                SyncCoatingTypes();
                
                // Помечаем все изменения как сохранённые
                SystemProvidersGrid.ActionTracker.MarkChangesAsSaved();
                ProfileSystemsGrid.ActionTracker.MarkChangesAsSaved();
                ColorsGrid.ActionTracker.MarkChangesAsSaved();
                WhipLengthsGrid.ActionTracker.MarkChangesAsSaved();
                ProfileTypesGrid.ActionTracker.MarkChangesAsSaved();
                ApplicabilitiesGrid.ActionTracker.MarkChangesAsSaved();
                ProfileArticlesGrid.ActionTracker.MarkChangesAsSaved();
                CoatingTypesGrid.ActionTracker.MarkChangesAsSaved();
                
                LoadData(); // reload to get updated IDs from DB
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving dictionaries: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SyncSystemProviders()
        {
            var dbItems = _dataService.GetSystemProviders(_dbPath);
            foreach (var item in _systemProviders)
            {
                if (item.Id == 0) _dataService.AddSystemProvider(item, _dbPath);
                else _dataService.UpdateSystemProvider(item, _dbPath);
            }
            var currentIds = _systemProviders.Where(x => x.Id > 0).Select(x => x.Id).ToList();
            foreach (var dbItem in dbItems.Where(x => !currentIds.Contains(x.Id)))
            {
                _dataService.DeleteSystemProvider(dbItem.Id, _dbPath);
            }
        }

        private void SyncProfileSystems()
        {
            var dbItems = _dataService.GetProfileSystems(_dbPath);
            foreach (var item in _profileSystems)
            {
                if (item.Id == 0) _dataService.AddProfileSystem(item, _dbPath);
                else _dataService.UpdateProfileSystem(item, _dbPath);
            }
            var currentIds = _profileSystems.Where(x => x.Id > 0).Select(x => x.Id).ToList();
            foreach (var dbItem in dbItems.Where(x => !currentIds.Contains(x.Id)))
            {
                _dataService.DeleteProfileSystem(dbItem.Id, _dbPath);
            }
        }

        private void SyncColors()
        {
            var dbItems = _dataService.GetColors(_dbPath);
            foreach (var item in _colors)
            {
                if (item.Id == 0) _dataService.AddColor(item, _dbPath);
                else _dataService.UpdateColor(item, _dbPath);
            }
            var currentIds = _colors.Where(x => x.Id > 0).Select(x => x.Id).ToList();
            foreach (var dbItem in dbItems.Where(x => !currentIds.Contains(x.Id)))
            {
                _dataService.DeleteColor(dbItem.Id, _dbPath);
            }
        }

        private void SyncWhipLengths()
        {
            var dbItems = _dataService.GetWhipLengths(_dbPath);
            foreach (var item in _whipLengths)
            {
                if (item.Id == 0) _dataService.AddWhipLength(item, _dbPath);
                else _dataService.UpdateWhipLength(item, _dbPath);
            }
            var currentIds = _whipLengths.Where(x => x.Id > 0).Select(x => x.Id).ToList();
            foreach (var dbItem in dbItems.Where(x => !currentIds.Contains(x.Id)))
            {
                _dataService.DeleteWhipLength(dbItem.Id, _dbPath);
            }
        }

        private void SyncProfileArticles()
        {
            var dbItems = _dataService.GetProfileArticles(_dbPath);
            foreach (var item in _profileArticles)
            {
                if (item.Id == 0) _dataService.AddProfileArticle(item, _dbPath);
                else _dataService.UpdateProfileArticle(item, _dbPath);
            }
            var currentIds = _profileArticles.Where(x => x.Id > 0).Select(x => x.Id).ToList();
            foreach (var dbItem in dbItems.Where(x => !currentIds.Contains(x.Id)))
            {
                _dataService.DeleteProfileArticle(dbItem.Id, _dbPath);
            }
        }

        private void SyncProfileTypes()
        {
            var dbItems = _dataService.GetProfileTypes(_dbPath);
            foreach (var item in _profileTypes)
            {
                if (item.Id == 0) _dataService.AddProfileType(item, _dbPath);
                else _dataService.UpdateProfileType(item, _dbPath);
            }
            var currentIds = _profileTypes.Where(x => x.Id > 0).Select(x => x.Id).ToList();
            foreach (var dbItem in dbItems.Where(x => !currentIds.Contains(x.Id)))
            {
                _dataService.DeleteProfileType(dbItem.Id, _dbPath);
            }
        }

        private void SyncApplicabilities()
        {
            var dbItems = _dataService.GetApplicabilities(_dbPath);
            foreach (var item in _applicabilities)
            {
                if (item.Id == 0) _dataService.AddApplicability(item, _dbPath);
                else _dataService.UpdateApplicability(item, _dbPath);
            }
            var currentIds = _applicabilities.Where(x => x.Id > 0).Select(x => x.Id).ToList();
            foreach (var dbItem in dbItems.Where(x => !currentIds.Contains(x.Id)))
            {
                _dataService.DeleteApplicability(dbItem.Id, _dbPath);
            }
        }

        private void SyncCoatingTypes()
        {
            var dbItems = _dataService.GetCoatingTypes(_dbPath);
            foreach (var item in _coatingTypes)
            {
                if (item.Id == 0) _dataService.AddCoatingType(item, _dbPath);
                else _dataService.UpdateCoatingType(item, _dbPath);
            }
            var currentIds = _coatingTypes.Where(x => x.Id > 0).Select(x => x.Id).ToList();
            foreach (var dbItem in dbItems.Where(x => !currentIds.Contains(x.Id)))
            {
                _dataService.DeleteCoatingType(dbItem.Id, _dbPath);
            }
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
                { typeof(CoatingType), _coatingTypes }
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
        /// Проверяет, есть ли несохранённые изменения в любой из таблиц справочников.
        /// </summary>
        public bool HasAnyUnsavedChanges()
        {
            return SystemProvidersGrid.ActionTracker.HasUnsavedChanges ||
                   ProfileSystemsGrid.ActionTracker.HasUnsavedChanges ||
                   ColorsGrid.ActionTracker.HasUnsavedChanges ||
                   WhipLengthsGrid.ActionTracker.HasUnsavedChanges ||
                   ProfileTypesGrid.ActionTracker.HasUnsavedChanges ||
                   ApplicabilitiesGrid.ActionTracker.HasUnsavedChanges ||
                   ProfileArticlesGrid.ActionTracker.HasUnsavedChanges ||
                   CoatingTypesGrid.ActionTracker.HasUnsavedChanges;
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
        /// Переключает отображение на таблицу материалов (Profile Articles).
        /// </summary>
        public void ShowProfileArticles()
        {
            // Симулируем нажатие кнопки Profile Articles
            ProfileArticlesGrid.Visibility = Visibility.Visible;
            SystemProvidersGrid.Visibility = Visibility.Collapsed;
            ProfileSystemsGrid.Visibility = Visibility.Collapsed;
            ColorsGrid.Visibility = Visibility.Collapsed;
            WhipLengthsGrid.Visibility = Visibility.Collapsed;
            ProfileTypesGrid.Visibility = Visibility.Collapsed;
            ApplicabilitiesGrid.Visibility = Visibility.Collapsed;
            CoatingTypesGrid.Visibility = Visibility.Collapsed;
            
            // Force update undo/redo buttons
            OnAnyTrackerStateChanged(false, false);
        }
    }
}
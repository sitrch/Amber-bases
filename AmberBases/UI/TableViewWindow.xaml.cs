using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using AmberBases.Core;
using AmberBases.Core.Models.Dictionaries;
using AmberBases.Services;

namespace AmberBases.UI
{
    /// <summary>
    /// Модальное окно для отображения таблицы справочника с поддержкой вложенной навигации.
    /// При клике на FK ячейку открывается новая TableViewWindow с родительской таблицей.
    /// Поддерживает сохранение, отмену и закрытие с проверкой несохранённых изменений.
    /// </summary>
    public partial class TableViewWindow : Window
    {
        private readonly Dictionary<Type, IEnumerable> _allCollections;
        private Stack<TableViewWindow> _windowStack;
        private readonly IDictionaryDataService _dataService;
        private readonly string _dbPath;

        // Локальные коллекции для отображения текущей таблицы
        private ObservableCollection<SystemProvider> _systemProviders;
        private ObservableCollection<ProfileSystem> _profileSystems;
        private ObservableCollection<AmberBases.Core.Models.Dictionaries.Color> _colors;
        private ObservableCollection<WhipLength> _whipLengths;
        private ObservableCollection<ProfileType> _profileTypes;
        private ObservableCollection<Applicability> _applicabilities;
        private ObservableCollection<ProfileArticle> _profileArticles;
        private ObservableCollection<CoatingType> _coatingTypes;

        // Оригинальные снимки данных для отслеживания изменений
        private Dictionary<string, object> _originalSnapshots;
        private bool _isClosing = false;

        // Текущий тип и коллекция
        private Type _currentEntityType;
        private IList _currentCollection;

        public TableViewWindow(Type entityType, IList collection, Dictionary<Type, IEnumerable> allCollections)
        {
            InitializeComponent();

            _currentEntityType = entityType;
            _currentCollection = collection;
            _allCollections = allCollections;
            _windowStack = new Stack<TableViewWindow>();
            _windowStack.Push(this);

            // Инициализация сервиса данных
            _dataService = new SqliteDictionaryDataService();
            _dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseConfig.SqliteDictionariesDatabaseFile);

            // Инициализируем локальные копии коллекций
            InitializeLocalCollections();

            // Переназначаем локальную коллекцию для редактируемой сущности, чтобы SyncXXX методы работали с _currentCollection
            if (_currentEntityType == typeof(SystemProvider)) _systemProviders = _currentCollection as ObservableCollection<SystemProvider>;
            else if (_currentEntityType == typeof(ProfileSystem)) _profileSystems = _currentCollection as ObservableCollection<ProfileSystem>;
            else if (_currentEntityType == typeof(AmberBases.Core.Models.Dictionaries.Color)) _colors = _currentCollection as ObservableCollection<AmberBases.Core.Models.Dictionaries.Color>;
            else if (_currentEntityType == typeof(WhipLength)) _whipLengths = _currentCollection as ObservableCollection<WhipLength>;
            else if (_currentEntityType == typeof(ProfileType)) _profileTypes = _currentCollection as ObservableCollection<ProfileType>;
            else if (_currentEntityType == typeof(Applicability)) _applicabilities = _currentCollection as ObservableCollection<Applicability>;
            else if (_currentEntityType == typeof(ProfileArticle)) _profileArticles = _currentCollection as ObservableCollection<ProfileArticle>;
            else if (_currentEntityType == typeof(CoatingType)) _coatingTypes = _currentCollection as ObservableCollection<CoatingType>;

            // Строим словарь для DictionaryTableControl, заменяя на локальные копии
            var localCollections = BuildLocalCollectionsDict();

            // Инициализируем контрол
            TableControl.Initialize(
                entityType, 
                collection, 
                localCollections, 
                _dataService,
                _dbPath,
                OpenParentTable
            );

            // Устанавливаем заголовок окна
            var displayName = GetDisplayName(entityType.Name);
            Title = $"Таблица: {displayName}";

            // Сохраняем оригинальные значения для отслеживания изменений
            _originalSnapshots = CloneAllCollections();

            // Подписываемся на событие закрытия
            Closing += TableViewWindow_Closing;
        }

        /// <summary>
        /// Клонирование всех коллекций для сравнения изменений.
        /// </summary>
        private Dictionary<string, object> CloneAllCollections()
        {
            var snapshots = new Dictionary<string, object>();

            if (_systemProviders != null)
                snapshots["SystemProviders"] = CloneCollection(_systemProviders);
            if (_profileSystems != null)
                snapshots["ProfileSystems"] = CloneCollection(_profileSystems);
            if (_colors != null)
                snapshots["Colors"] = CloneCollection(_colors);
            if (_whipLengths != null)
                snapshots["WhipLengths"] = CloneCollection(_whipLengths);
            if (_profileTypes != null)
                snapshots["ProfileTypes"] = CloneCollection(_profileTypes);
            if (_applicabilities != null)
                snapshots["Applicabilities"] = CloneCollection(_applicabilities);
            if (_profileArticles != null)
                snapshots["ProfileArticles"] = CloneCollection(_profileArticles);
            if (_coatingTypes != null)
                snapshots["CoatingTypes"] = CloneCollection(_coatingTypes);

            return snapshots;
        }

        /// <summary>
        /// Клонирование коллекции объектов.
        /// </summary>
        private object CloneCollection(IList collection)
        {
            var clonedList = new List<Dictionary<string, object>>();

            foreach (var item in collection)
            {
                var itemDict = new Dictionary<string, object>();
                foreach (var prop in item.GetType().GetProperties())
                {
                    if (!(prop.PropertyType.IsClass && prop.PropertyType != typeof(string)))
                    {
                        itemDict[prop.Name] = prop.GetValue(item);
                    }
                }
                clonedList.Add(itemDict);
            }

            return clonedList;
        }

        /// <summary>
        /// Проверяет, есть ли изменения в коллекциях по сравнению с оригиналом.
        /// </summary>
        private bool CheckForChanges()
        {
            if (_originalSnapshots == null) return false;

            return HasCollectionChanged("SystemProviders", _systemProviders) ||
                   HasCollectionChanged("ProfileSystems", _profileSystems) ||
                   HasCollectionChanged("Colors", _colors) ||
                   HasCollectionChanged("WhipLengths", _whipLengths) ||
                   HasCollectionChanged("ProfileTypes", _profileTypes) ||
                   HasCollectionChanged("Applicabilities", _applicabilities) ||
                   HasCollectionChanged("ProfileArticles", _profileArticles) ||
                   HasCollectionChanged("CoatingTypes", _coatingTypes);
        }

        /// <summary>
        /// Проверяет изменения в конкретной коллекции.
        /// </summary>
        private bool HasCollectionChanged(string key, IList currentCollection)
        {
            if (currentCollection == null)
            {
                return _originalSnapshots.ContainsKey(key) && _originalSnapshots[key] != null;
            }

            if (!_originalSnapshots.TryGetValue(key, out var originalList))
                return true;

            var originalItems = originalList as List<Dictionary<string, object>>;
            if (originalItems == null) return true;

            // Проверяем количество элементов
            if (currentCollection.Count != originalItems.Count)
                return true;

            // Проверяем каждый элемент
            for (int i = 0; i < currentCollection.Count; i++)
            {
                var currentItem = currentCollection[i];
                var originalItem = originalItems[i];

                foreach (var kvp in originalItem)
                {
                    var prop = currentItem.GetType().GetProperty(kvp.Key);
                    if (prop != null)
                    {
                        var currentValue = prop.GetValue(currentItem);
                        if (!Equals(currentValue, kvp.Value))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Восстанавливает оригинальные значения коллекций.
        /// </summary>
        private void RestoreOriginalValues()
        {
            // Загружаем актуальные данные из БД для редактируемой коллекции
            IEnumerable freshData = null;
            if (_currentEntityType == typeof(SystemProvider)) freshData = _dataService.GetSystemProviders(_dbPath);
            else if (_currentEntityType == typeof(ProfileSystem)) freshData = _dataService.GetProfileSystems(_dbPath);
            else if (_currentEntityType == typeof(AmberBases.Core.Models.Dictionaries.Color)) freshData = _dataService.GetColors(_dbPath);
            else if (_currentEntityType == typeof(WhipLength)) freshData = _dataService.GetWhipLengths(_dbPath);
            else if (_currentEntityType == typeof(ProfileType)) freshData = _dataService.GetProfileTypes(_dbPath);
            else if (_currentEntityType == typeof(Applicability)) freshData = _dataService.GetApplicabilities(_dbPath);
            else if (_currentEntityType == typeof(ProfileArticle)) freshData = _dataService.GetProfileArticles(_dbPath);
            else if (_currentEntityType == typeof(CoatingType)) freshData = _dataService.GetCoatingTypes(_dbPath);

            if (freshData != null)
            {
                _currentCollection.Clear();
                foreach (var item in freshData)
                {
                    _currentCollection.Add(item);
                }
            }

            // Сбрасываем снимки
            _originalSnapshots = CloneAllCollections();
            
            // Обновляем DataGrid и очищаем историю изменений, если трекер поддерживает очистку
            TableControl.MainDataGrid.Items.Refresh();
        }

        /// <summary>
        /// Сохраняет изменения в базу данных.
        /// </summary>
        private bool SaveChanges()
        {
            try
            {
                if (_currentEntityType == typeof(SystemProvider)) SyncSystemProviders();
                else if (_currentEntityType == typeof(ProfileSystem)) SyncProfileSystems();
                else if (_currentEntityType == typeof(AmberBases.Core.Models.Dictionaries.Color)) SyncColors();
                else if (_currentEntityType == typeof(WhipLength)) SyncWhipLengths();
                else if (_currentEntityType == typeof(ProfileType)) SyncProfileTypes();
                else if (_currentEntityType == typeof(Applicability)) SyncApplicabilities();
                else if (_currentEntityType == typeof(ProfileArticle)) SyncProfileArticles();
                else if (_currentEntityType == typeof(CoatingType)) SyncCoatingTypes();

                // Обновляем снимок оригинальных данных
                _originalSnapshots = CloneAllCollections();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Отменяет изменения и восстанавливает оригинальные данные.
        /// </summary>
        private void DiscardChanges()
        {
            RestoreOriginalValues();
        }

        private void InitializeLocalCollections()
        {
            if (_allCollections == null) return;

            if (_allCollections.TryGetValue(typeof(SystemProvider), out var sp))
                _systemProviders = new ObservableCollection<SystemProvider>((IEnumerable<SystemProvider>)sp);
            if (_allCollections.TryGetValue(typeof(ProfileSystem), out var ps))
                _profileSystems = new ObservableCollection<ProfileSystem>((IEnumerable<ProfileSystem>)ps);
            if (_allCollections.TryGetValue(typeof(Color), out var c))
                _colors = new ObservableCollection<AmberBases.Core.Models.Dictionaries.Color>((IEnumerable<Color>)c);
            if (_allCollections.TryGetValue(typeof(WhipLength), out var wl))
                _whipLengths = new ObservableCollection<WhipLength>((IEnumerable<WhipLength>)wl);
            if (_allCollections.TryGetValue(typeof(ProfileType), out var pt))
                _profileTypes = new ObservableCollection<ProfileType>((IEnumerable<ProfileType>)pt);
            if (_allCollections.TryGetValue(typeof(Applicability), out var app))
                _applicabilities = new ObservableCollection<Applicability>((IEnumerable<Applicability>)app);
            if (_allCollections.TryGetValue(typeof(ProfileArticle), out var pa))
                _profileArticles = new ObservableCollection<ProfileArticle>((IEnumerable<ProfileArticle>)pa);
            if (_allCollections.TryGetValue(typeof(CoatingType), out var ct))
                _coatingTypes = new ObservableCollection<CoatingType>((IEnumerable<CoatingType>)ct);
        }

        private Dictionary<Type, IEnumerable> BuildLocalCollectionsDict()
        {
            var dict = new Dictionary<Type, IEnumerable>();

            if (_systemProviders != null) dict[typeof(SystemProvider)] = _systemProviders;
            if (_profileSystems != null) dict[typeof(ProfileSystem)] = _profileSystems;
            if (_colors != null) dict[typeof(Color)] = _colors;
            if (_whipLengths != null) dict[typeof(WhipLength)] = _whipLengths;
            if (_profileTypes != null) dict[typeof(ProfileType)] = _profileTypes;
            if (_applicabilities != null) dict[typeof(Applicability)] = _applicabilities;
            if (_profileArticles != null) dict[typeof(ProfileArticle)] = _profileArticles;
            if (_coatingTypes != null) dict[typeof(CoatingType)] = _coatingTypes;

            return dict;
        }

        /// <summary>
        /// Открывает родительскую таблицу в новом окне TableViewWindow.
        /// </summary>
        private void OpenParentTable(object parentItem, Type parentType)
        {
            var parentCollection = GetCollectionForEntityType(parentType);
            if (parentCollection == null)
            {
                MessageBox.Show($"Коллекция для типа {parentType.Name} не найдена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parentWindow = new TableViewWindow(parentType, parentCollection, BuildLocalCollectionsDict())
            {
                Owner = this
            };

            _windowStack.Push(parentWindow);
            parentWindow.Closed += (s, e) => _windowStack.Pop();
            parentWindow.ShowDialog();
        }

        private IList GetCollectionForEntityType(Type type)
        {
            if (type == typeof(SystemProvider)) return _systemProviders;
            if (type == typeof(ProfileSystem)) return _profileSystems;
            if (type == typeof(ProfileType)) return _profileTypes;
            if (type == typeof(Applicability)) return _applicabilities;
            if (type == typeof(CoatingType)) return _coatingTypes;
            return null;
        }

        private string GetDisplayName(string typeName)
        {
            var names = new Dictionary<string, string>
            {
                { "SystemProvider", "Поставщики систем" },
                { "ProfileSystem", "Профильные системы" },
                { "Color", "Цвета" },
                { "WhipLength", "Длины хлыста" },
                { "ProfileType", "Типы профилей" },
                { "Applicability", "Применимость" },
                { "ProfileArticle", "Артикулы" },
                { "CoatingType", "Типы покрытий" }
            };
            return names.ContainsKey(typeName) ? names[typeName] : typeName;
        }

        #region Sync Methods

        private void SyncSystemProviders()
        {
            if (_systemProviders == null) return;
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
            if (_profileSystems == null) return;
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
            if (_colors == null) return;
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
            if (_whipLengths == null) return;
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
            if (_profileArticles == null) return;
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
            if (_profileTypes == null) return;
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
            if (_applicabilities == null) return;
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
            if (_coatingTypes == null) return;
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

        #endregion

        #region Button Click Handlers

        /// <summary>
        /// Обработчик кнопки "Сохранить" — сохраняет изменения в БД.
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CheckForChanges())
            {
                if (SaveChanges())
                {
                    // Обновляем интерфейс
                    TableControl.MainDataGrid.Items.Refresh();
                }
            }
            else
            {
                MessageBox.Show("Нет изменений для сохранения.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Обработчик кнопки "Отменить" — сбрасывает изменения и восстанавливает оригинальные данные.
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (CheckForChanges())
            {
                var result = MessageBox.Show(
                    "Отменить все изменения и восстановить оригинальные данные?",
                    "Отмена изменений",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DiscardChanges();
                }
            }
            else
            {
                MessageBox.Show("Нет изменений для отмены.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Обработчик кнопки "Закрыть" — с проверкой несохранённых изменений.
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Closing Event Handler

        /// <summary>
        /// Обработчик события закрытия окна.
        /// Проверяет несохранённые изменения и показывает диалог.
        /// </summary>
        private void TableViewWindow_Closing(object sender, CancelEventArgs e)
        {
            // Если уже в процессе закрытия (после подтверждения) — не показываем диалог
            if (_isClosing) return;

            // Проверяем, есть ли несохранённые изменения
            if (!CheckForChanges()) return;

            var result = MessageBox.Show(
                "Есть несохранённые изменения.\n\n" +
                "Да — Сохранить и закрыть\n" +
                "Нет — Отменить изменения и закрыть\n" +
                "Отмена — Остаться в редакторе",
                "Несохранённые изменения",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Cancel);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    _isClosing = true; // Устанавливаем флаг ДО сохранения, чтобы предотвратить повторный диалог
                    if (!SaveChanges())
                    {
                        // Если сохранение не удалось, отменяем закрытие
                        _isClosing = false;
                        e.Cancel = true;
                    }
                    // Не вызываем Close() — окно закроется само после завершения обработчика
                    break;
                case MessageBoxResult.No:
                    _isClosing = true; // Устанавливаем флаг ДО отмены изменений
                    DiscardChanges();
                    // Не вызываем Close() — окно закроется само
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true; // Остаёмся в окне
                    break;
            }
        }

        #endregion
    }
}
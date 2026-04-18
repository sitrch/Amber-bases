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
        private ObservableCollection<StandartBarLength> _StandartBarLengths;
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
            else if (_currentEntityType == typeof(StandartBarLength)) _StandartBarLengths = _currentCollection as ObservableCollection<StandartBarLength>;
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
            var displayName = DisplayNameProvider.GetTypeName(entityType.Name);
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
            if (_StandartBarLengths != null)
                snapshots["StandartBarLengths"] = CloneCollection(_StandartBarLengths);
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
                   HasCollectionChanged("StandartBarLengths", _StandartBarLengths) ||
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
            var freshData = GetItemsGeneric(_currentEntityType, _dbPath);
            if (freshData != null)
            {
                _currentCollection.Clear();
                foreach (var item in freshData)
                {
                    _currentCollection.Add(item);
                }
            }
            _originalSnapshots = CloneAllCollections();
        }

        private IEnumerable GetItemsGeneric(Type entityType, string dbPath)
        {
            var method = typeof(IDictionaryDataService).GetMethod("GetItems").MakeGenericMethod(entityType);
            return (IEnumerable)method.Invoke(_dataService, new object[] { dbPath });
        }

        /// <summary>
        /// Обновляет DataGrid и очищает историю изменений.
        /// </summary>
        private void RefreshDataGrid()
        {
            TableControl?.RefreshDataGrid();
        }

        /// <summary>
        /// Сохраняет изменения в базу данных.
        /// </summary>
        private bool SaveChanges()
        {
            try
            {
                SyncCurrentCollection();
                _originalSnapshots = CloneAllCollections();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void SyncCurrentCollection()
        {
            var items = _currentCollection.Cast<BaseDictionaryModel>().ToList();
            var dbItems = GetItems(_currentEntityType, _dbPath);
            foreach (var item in items)
            {
                if (item.Id == 0) AddItem(item, _dbPath);
                else UpdateItem(item, _dbPath);
            }
            var currentIds = items.Where(x => x.Id > 0).Select(x => x.Id).ToList();
            foreach (var dbItem in dbItems.Where(x => !currentIds.Contains(x.Id)))
            {
                DeleteItem(_currentEntityType, dbItem.Id, _dbPath);
            }
        }

        private List<BaseDictionaryModel> GetItems(Type entityType, string dbPath)
        {
            var method = typeof(IDictionaryDataService).GetMethod("GetItems").MakeGenericMethod(entityType);
            return ((System.Collections.IEnumerable)method.Invoke(_dataService, new object[] { dbPath }))
                .Cast<BaseDictionaryModel>().ToList();
        }

        private void AddItem(BaseDictionaryModel item, string dbPath)
        {
            var method = typeof(IDictionaryDataService).GetMethod("AddItem").MakeGenericMethod(item.GetType());
            method.Invoke(_dataService, new object[] { item, dbPath });
        }

        private void UpdateItem(BaseDictionaryModel item, string dbPath)
        {
            var method = typeof(IDictionaryDataService).GetMethod("UpdateItem").MakeGenericMethod(item.GetType());
            method.Invoke(_dataService, new object[] { item, dbPath });
        }

        private void DeleteItem(Type entityType, int id, string dbPath)
        {
            var method = typeof(IDictionaryDataService).GetMethod("DeleteItem").MakeGenericMethod(entityType);
            method.Invoke(_dataService, new object[] { id, dbPath });
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
            if (_allCollections.TryGetValue(typeof(StandartBarLength), out var wl))
                _StandartBarLengths = new ObservableCollection<StandartBarLength>((IEnumerable<StandartBarLength>)wl);
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
            if (_StandartBarLengths != null) dict[typeof(StandartBarLength)] = _StandartBarLengths;
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
                    RefreshDataGrid();
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
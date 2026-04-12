using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Reflection;

namespace AmberBases.UI.Tracking
{
    /// <summary>
    /// Трекер изменений таблицы на основе DataTable с поддержкой Undo/Redo.
    /// Использует подход snapshots - каждый снимок хранит полную копию состояния.
    /// </summary>
    public class EditActionTracker
    {
        private class Snapshot
        {
            public long Timestamp { get; set; }
            public List<RowState> Rows { get; set; } = new List<RowState>();
        }

        private class RowState
        {
            public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
            public bool Deleted { get; set; }
        }

        private readonly IList _sourceCollection;
        private readonly Type _entityType;
        private readonly PropertyInfo[] _entityProperties;
        private readonly INotifyCollectionChanged _notifyCollection;

        private DataTable _trackingTable;
        private readonly List<Snapshot> _history = new List<Snapshot>();
        private int _historyIndex = -1;
        private bool _hasUnsavedChanges;
        private bool _isDetectingChanges;
        private IList _preUndoSnapshot;

        /// <summary>
        /// Событие изменения состояния CanUndo/CanRedo.
        /// </summary>
        public event Action<bool, bool> StateChanged;

        /// <summary>
        /// Можно ли отменить действие.
        /// </summary>
        public bool CanUndo => _historyIndex > 0;

        /// <summary>
        /// Можно ли повторить действие.
        /// </summary>
        public bool CanRedo => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

        /// <summary>
        /// Есть ли несохранённые изменения.
        /// </summary>
        public bool HasUnsavedChanges => _hasUnsavedChanges;

        /// <summary>
        /// Таблица отслеживания для биндинга к DataGrid.
        /// </summary>
        public DataTable TrackingTable => _trackingTable;

        // Навигационные свойства, которые должны быть исключены из трекинга (complex reference types)
        private static readonly HashSet<string> ExcludedNavigationProperties = new HashSet<string>
        {
            // ProfileArticle navigation
            "Manufacturer", "System", "Color", "StandartBarLength", "ProfileType",
            // ProfileSystem navigation
            "Provider",
            // Color navigation
            "CoatingType"
        };

        public EditActionTracker(IList sourceCollection, Type entityType)
        {
            _sourceCollection = sourceCollection ?? throw new ArgumentNullException(nameof(sourceCollection));
            _entityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            
            _entityProperties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !ExcludedNavigationProperties.Contains(p.Name))
                .Where(p => IsSimpleType(p.PropertyType))
                .ToArray();

            _notifyCollection = sourceCollection as INotifyCollectionChanged;
        }

        /// <summary>
        /// Инициализация трекера: создаёт DataTable-копию, заполняет данными.
        /// </summary>
        public void Initialize()
        {
            CreateTrackingTable();
            var initialSnapshot = CreateSnapshotFromSource();
            _history.Clear();
            _history.Add(initialSnapshot);
            _historyIndex = 0;
            _hasUnsavedChanges = false;
            PopulateTableFromSnapshot(initialSnapshot);
            SubscribeToCollectionChanges();
            OnStateChanged();
        }

        /// <summary>
        /// Создаёт структуру DataTable с колонками свойств модели.
        /// </summary>
        private void CreateTrackingTable()
        {
            _trackingTable = new DataTable("TrackingTable");

            foreach (var prop in _entityProperties)
            {
                var columnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var column = new DataColumn(prop.Name, columnType);
                _trackingTable.Columns.Add(column);
            }
        }

        /// <summary>
        /// Создаёт снимок текущего состояния источника данных.
        /// </summary>
        private Snapshot CreateSnapshotFromSource()
        {
            var snapshot = new Snapshot
            {
                Timestamp = GetTimestampMillis()
            };

            foreach (var item in _sourceCollection)
            {
                var rowState = CreateRowStateFromItem(item, deleted: false);
                snapshot.Rows.Add(rowState);
            }

            return snapshot;
        }

        /// <summary>
        /// Создаёт RowState из объекта модели.
        /// </summary>
        private RowState CreateRowStateFromItem(object item, bool deleted)
        {
            var rowState = new RowState { Deleted = deleted };

            foreach (var prop in _entityProperties)
            {
                try
                {
                    var value = prop.GetValue(item);
                    rowState.Values[prop.Name] = value;
                }
                catch
                {
                    rowState.Values[prop.Name] = null;
                }
            }

            return rowState;
        }

        /// <summary>
        /// Заполняет DataTable данными из снимка.
        /// </summary>
        private void PopulateTableFromSnapshot(Snapshot snapshot)
        {
            _trackingTable.Rows.Clear();

            foreach (var rowState in snapshot.Rows)
            {
                if (rowState.Deleted) continue;

                var row = _trackingTable.NewRow();
                foreach (var prop in _entityProperties)
                {
                    var value = rowState.Values.TryGetValue(prop.Name, out var v) ? v : null;
                    row[prop.Name] = value ?? DBNull.Value;
                }
                _trackingTable.Rows.Add(row);
            }
        }

        /// <summary>
        /// Подписывается на изменения коллекции.
        /// </summary>
        private void SubscribeToCollectionChanges()
        {
            if (_notifyCollection != null)
            {
                _notifyCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        /// <summary>
        /// Отписывается от изменений коллекции.
        /// </summary>
        public void Unsubscribe()
        {
            if (_notifyCollection != null)
            {
                _notifyCollection.CollectionChanged -= OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isDetectingChanges) return;
            DetectAndRecordChanges();
        }

        /// <summary>
        /// Автоматическое обнаружение и запись изменений.
        /// </summary>
        public void DetectAndRecordChanges(bool updateTable = true)
        {
            if (_trackingTable == null) return;
            if (_isDetectingChanges) return;

            _isDetectingChanges = true;

            try
            {
                // Если были undo-операции — обрезаем ветку
                if (_historyIndex < _history.Count - 1)
                {
                    _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
                }

                var newSnapshot = CreateSnapshotFromSource();
                _history.Add(newSnapshot);
                _historyIndex = _history.Count - 1;

                if (updateTable)
                {
                    PopulateTableFromSnapshot(newSnapshot);
                }

                _hasUnsavedChanges = true;
                OnStateChanged();
            }
            finally
            {
                _isDetectingChanges = false;
            }
        }

        /// <summary>
        /// Отменить последнее действие.
        /// </summary>
        public bool Undo()
        {
            Console.WriteLine($"[Undo] Start. CanUndo={CanUndo}, _historyIndex={_historyIndex}, HistoryCount={_history.Count}, SourceCount={_sourceCollection.Count}");

            if (!CanUndo)
            {
                Console.WriteLine("[Undo] CanUndo is false, exiting");
                return false;
            }

            _historyIndex--;
            var targetSnapshot = _history[_historyIndex];

            Console.WriteLine($"[Undo] Restoring snapshot at index {_historyIndex}, timestamp={targetSnapshot.Timestamp}, rows={targetSnapshot.Rows.Count}");

            // Восстанавливаем коллекцию из снимка
            RestoreCollectionFromSnapshot(targetSnapshot);

            // Не сбрасываем _hasUnsavedChanges — undo не является сохранением в БД
            // Если мы были в несохранённом состоянии, то после undo всё равно есть несохранённые изменения

            Console.WriteLine($"[Undo] Done. SourceCount={_sourceCollection.Count}, TableRows={_trackingTable?.Rows.Count}");

            OnStateChanged();
            return true;
        }

        /// <summary>
        /// Повторить отменённое действие.
        /// </summary>
        public bool Redo()
        {
            if (!CanRedo) return false;

            _historyIndex++;
            var targetSnapshot = _history[_historyIndex];

            RestoreCollectionFromSnapshot(targetSnapshot);

            // Не сбрасываем _hasUnsavedChanges — redo не является сохранением в БД
            OnStateChanged();
            return true;
        }

        /// <summary>
        /// Явно помечает изменения как сохранённые. Вызывать после успешного сохранения в БД.
        /// </summary>
        public void MarkChangesAsSaved()
        {
            // Обновляем базовый снимок, чтобы отразить сохранённое состояние
            var currentSnapshot = CreateSnapshotFromSource();
            _history.Clear();
            _history.Add(currentSnapshot);
            _historyIndex = 0;
            _hasUnsavedChanges = false;
            OnStateChanged();
        }

        /// <summary>
        /// Восстанавливает коллекцию и таблицу из снимка.
        /// </summary>
        private void RestoreCollectionFromSnapshot(Snapshot snapshot)
        {
            _isDetectingChanges = true;
            try
            {
                // Сначала обновляем DataTable
                PopulateTableFromSnapshot(snapshot);

                // Затем обновляем исходную коллекцию
                _sourceCollection.Clear();

                foreach (var rowState in snapshot.Rows)
                {
                    if (rowState.Deleted) continue;

                    var newItem = Activator.CreateInstance(_entityType);
                    foreach (var prop in _entityProperties)
                    {
                        if (!prop.CanWrite) continue;
                        try
                        {
                            if (rowState.Values.TryGetValue(prop.Name, out var value) && value != null)
                            {
                                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                if (value.GetType() == propType)
                                {
                                    prop.SetValue(newItem, value);
                                }
                                else
                                {
                                    value = Convert.ChangeType(value, propType);
                                    prop.SetValue(newItem, value);
                                }
                            }
                        }
                        catch { }
                    }
                    _sourceCollection.Add(newItem);
                }
            }
            finally
            {
                _isDetectingChanges = false;
            }
        }

        /// <summary>
        /// Сохраняет текущее состояние исходной коллекции перед undo.
        /// </summary>
        private void SaveCurrentState()
        {
            var snapshot = new ArrayList();
            foreach (var item in _sourceCollection)
            {
                snapshot.Add(DeepClone(item));
            }
            _preUndoSnapshot = snapshot;
        }

        /// <summary>
        /// Восстанавливает коллекцию из snapshot.
        /// </summary>
        private void RestoreFromSnapshot()
        {
            if (_preUndoSnapshot == null) return;

            _isDetectingChanges = true;
            try
            {
                _sourceCollection.Clear();
                foreach (var item in _preUndoSnapshot)
                {
                    _sourceCollection.Add(item);
                }
                _preUndoSnapshot = null;
            }
            finally
            {
                _isDetectingChanges = false;
            }
        }

        /// <summary>
        /// Обрезать ветвь при ветвлении (новое редактирование после undo).
        /// </summary>
        public void PruneBranch()
        {
            if (_historyIndex < _history.Count - 1)
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            }

            if (_preUndoSnapshot != null)
            {
                RestoreFromSnapshot();
                _preUndoSnapshot = null;
            }
        }

        /// <summary>
        /// Получить текущий timestamp в миллисекундах.
        /// </summary>
        private static long GetTimestampMillis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Глубокое клонирование объекта через рефлексию.
        /// </summary>
        public object DeepClone(object source)
        {
            if (source == null) return null;

            var clone = Activator.CreateInstance(_entityType);

            if (source is DataRowView drv)
            {
                foreach (var prop in _entityProperties)
                {
                    if (prop.CanWrite)
                    {
                        try
                        {
                            if (drv.Row.Table.Columns.Contains(prop.Name))
                            {
                                var value = drv[prop.Name];
                                if (value == DBNull.Value) value = null;
                                
                                if (value != null)
                                {
                                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                    value = Convert.ChangeType(value, propType);
                                }
                                prop.SetValue(clone, value);
                            }
                        }
                        catch { }
                    }
                }
            }
            else
            {
                foreach (var prop in _entityProperties)
                {
                    if (prop.CanRead && prop.CanWrite)
                    {
                        try
                        {
                            var value = prop.GetValue(source);
                            prop.SetValue(clone, value);
                        }
                        catch { }
                    }
                }
            }
            return clone;
        }

        private void OnStateChanged()
        {
            StateChanged?.Invoke(CanUndo, CanRedo);
        }

        /// <summary>
        /// Проверяет, является ли тип простым (скалярным) — подходит для DataColumn.
        /// Исключает сложные reference-типы с навигационными свойствами.
        /// </summary>
        private static bool IsSimpleType(Type type)
        {
            // Примитивные типы
            if (type.IsPrimitive) return true;
            if (type == typeof(string)) return true;
            if (type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid)) return true;
            if (type == typeof(TimeSpan) || type == typeof(DateTimeOffset)) return true;
            
            // Nullable обёртки над простыми типами
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null) return IsSimpleType(underlying);
            
            // Enum
            if (type.IsEnum) return true;
            
            // Всё остальное (сложные классы моделей) — исключаем
            return false;
        }
    }
}

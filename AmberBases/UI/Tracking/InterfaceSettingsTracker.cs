using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace AmberBases.UI.Tracking
{
    /// <summary>
    /// Трекер настроек интерфейса (сохраняет ширину колонок и положение сплиттеров).
    /// Настройки сохраняются в отдельный json файл.
    /// </summary>
    public class InterfaceSettingsTracker
    {
        private readonly string _settingsFilePath;
        private Dictionary<string, object> _settings;

        public InterfaceSettingsTracker()
        {
            _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_settings.json");
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    _settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                }
                catch
                {
                    _settings = new Dictionary<string, object>();
                }
            }
            else
            {
                _settings = new Dictionary<string, object>();
            }
        }

        private void SaveSettings()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения настроек UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохраняет состояние свернутости Ribbon для конкретной вкладки.
        /// </summary>
        /// <param name="tabName">Название вкладки (например, "Excel", "Dictionaries")</param>
        /// <param name="isMinimized">true если лента свернута</param>
        public void SaveRibbonMinimizedState(string tabName, bool isMinimized)
        {
            _settings[$"RibbonMinimized_{tabName}"] = isMinimized;
            SaveSettings();
        }

        /// <summary>
        /// Загружает сохранённое состояние свернутости Ribbon для вкладки.
        /// </summary>
        /// <param name="tabName">Название вкладки</param>
        /// <returns>true если лента должна быть свернута</returns>
        public bool LoadRibbonMinimizedState(string tabName)
        {
            if (_settings.TryGetValue($"RibbonMinimized_{tabName}", out var val))
                return Convert.ToBoolean(val);
            return false; // по умолчанию лента развёрнута
        }

        /// <summary>
        /// Сохраняет состояние свёрнутости лент (RibbonGroup) для конкретной вкладки.
        /// Использует список пар (имя ленты, состояние свёрнутости).
        /// </summary>
        /// <param name="tabName">Название вкладки</param>
        /// <param name="states">Список пар: имя ленты и состояние свёрнутости</param>
        public void SaveRibbonGroupStates(string tabName, List<(string groupName, bool isCollapsed)> states)
        {
            foreach (var state in states)
            {
                string key = $"RibbonGroupCollapsed_{tabName}_{state.groupName}";
                _settings[key] = state.isCollapsed;
            }
            SaveSettings();
        }

        /// <summary>
        /// Загружает состояние свёрнутости лент для конкретной вкладки.
        /// </summary>
        /// <param name="tabName">Название вкладки</param>
        /// <param name="groupNames">Список имён лент, для которых нужно загрузить состояние</param>
        /// <returns>Список пар: имя ленты и состояние свёрнутости (false по умолчанию)</returns>
        public List<(string groupName, bool isCollapsed)> LoadRibbonGroupStates(string tabName, List<string> groupNames)
        {
            var result = new List<(string groupName, bool isCollapsed)>();
            foreach (var groupName in groupNames)
            {
                string key = $"RibbonGroupCollapsed_{tabName}_{groupName}";
                bool isCollapsed = false;
                if (_settings.TryGetValue(key, out var val))
                {
                    isCollapsed = Convert.ToBoolean(val);
                }
                result.Add((groupName, isCollapsed));
            }
            return result;
        }

        /// <summary>
        /// Сохраняет состояние свёрнутости одной ленты для вкладки.
        /// </summary>
        /// <param name="tabName">Название вкладки</param>
        /// <param name="groupName">Имя ленты (RibbonGroup)</param>
        /// <param name="isCollapsed">true если лента свёрнута</param>
        public void SaveRibbonGroupState(string tabName, string groupName, bool isCollapsed)
        {
            string key = $"RibbonGroupCollapsed_{tabName}_{groupName}";
            _settings[key] = isCollapsed;
            SaveSettings();
        }

        /// <summary>
        /// Загружает состояние свёрнутости одной ленты для вкладки.
        /// </summary>
        /// <param name="tabName">Название вкладки</param>
        /// <param name="groupName">Имя ленты (RibbonGroup)</param>
        /// <returns>true если лента должна быть свёрнута</returns>
        public bool LoadRibbonGroupState(string tabName, string groupName)
        {
            string key = $"RibbonGroupCollapsed_{tabName}_{groupName}";
            if (_settings.TryGetValue(key, out var val))
                return Convert.ToBoolean(val);
            return false; // по умолчанию лента развёрнута
        }

        /// <summary>
        /// Отслеживает и сохраняет высоту панели через GridSplitter.
        /// Настройки сохраняются в ui_settings.json.
        /// </summary>
        /// <param name="panel">Граничная панель, высоту которой нужно отслеживать (Border или Grid)</param>
        /// <param name="rowDefinition">RowDefinition панели</param>
        /// <param name="gridSplitter">GridSplitter для перетаскивания</param>
        /// <param name="key">Ключ для сохранения настроек</param>
        /// <param name="minHeight">Минимальная высота (по умолчанию 30)</param>
        /// <param name="maxHeight">Максимальная высота (по умолчанию 200)</param>
        public void TrackPanelHeight(System.Windows.FrameworkElement panel, RowDefinition rowDefinition, GridSplitter gridSplitter, string key, double minHeight = 30, double maxHeight = 200)
        {
            const double defaultHeight = 42;

            bool heightRestored = false;
            if (_settings.TryGetValue($"{key}_PanelHeight", out var heightObj))
            {
                try
                {
                    double height = Convert.ToDouble(heightObj);
                    if (height >= minHeight && height <= maxHeight)
                    {
                        rowDefinition.Height = new GridLength(height, GridUnitType.Pixel);
                        panel.Height = height;
                        heightRestored = true;
                    }
                }
                catch { }
            }

            if (!heightRestored)
            {
                rowDefinition.Height = new GridLength(defaultHeight, GridUnitType.Pixel);
                panel.Height = defaultHeight;
            }

            gridSplitter.DragCompleted += (s, e) =>
            {
                _settings[$"{key}_PanelHeight"] = rowDefinition.Height.Value;
                SaveSettings();
            };
        }

        public void SaveFilterColumn(string gridName, string columnName)
        {
            _settings[$"{gridName}_FilterColumn"] = columnName;
            SaveSettings();
        }

        public string LoadFilterColumn(string gridName, string defaultColumn)
        {
            if (_settings.TryGetValue($"{gridName}_FilterColumn", out var val))
                return val?.ToString() ?? defaultColumn;
            return defaultColumn;
        }
    }
}

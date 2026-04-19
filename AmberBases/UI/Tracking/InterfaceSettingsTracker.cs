using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace AmberBases.UI.Tracking
{
    public class ColumnSettingsEntry
    {
        public string name { get; set; }
        public double width { get; set; }
    }

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

        public void SaveRibbonMinimizedState(string tabName, bool isMinimized)
        {
            _settings[$"RibbonMinimized_{tabName}"] = isMinimized;
            SaveSettings();
        }

        public bool LoadRibbonMinimizedState(string tabName)
        {
            if (_settings.TryGetValue($"RibbonMinimized_{tabName}", out var val))
                return Convert.ToBoolean(val);
            return false;
        }

        public void SaveRibbonGroupStates(string tabName, List<(string groupName, bool isCollapsed)> states)
        {
            foreach (var state in states)
            {
                string key = $"RibbonGroupCollapsed_{tabName}_{state.groupName}";
                _settings[key] = state.isCollapsed;
            }
            SaveSettings();
        }

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

        public void SaveRibbonGroupState(string tabName, string groupName, bool isCollapsed)
        {
            string key = $"RibbonGroupCollapsed_{tabName}_{groupName}";
            _settings[key] = isCollapsed;
            SaveSettings();
        }

        public bool LoadRibbonGroupState(string tabName, string groupName)
        {
            string key = $"RibbonGroupCollapsed_{tabName}_{groupName}";
            if (_settings.TryGetValue(key, out var val))
                return Convert.ToBoolean(val);
            return false;
        }

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

        public object GetSetting(string key)
        {
            return _settings.TryGetValue(key, out var val) ? val : null;
        }

        public void SaveColumnOrder(string entityType, List<ColumnSettingsEntry> columns)
        {
            _settings[$"ColumnOrder_{entityType}"] = columns;
            SaveSettings();
        }

        public List<ColumnSettingsEntry> LoadColumnOrder(string entityType)
        {
            var key = $"ColumnOrder_{entityType}";
            if (_settings.TryGetValue(key, out var val))
            {
                if (val is List<ColumnSettingsEntry> cols)
                    return cols;
                if (val is Newtonsoft.Json.Linq.JArray jArray)
                {
                    var list = jArray.ToObject<List<ColumnSettingsEntry>>();
                    if (list != null && list.Count > 0 && list[0].name != null)
                        return list;
                    try
                    {
                        var stringList = new List<string>();
                        foreach (var item in jArray)
                        {
                            if (item.Type == Newtonsoft.Json.Linq.JTokenType.String)
                            {
                                stringList.Add(item.ToString());
                            }
                        }
                        if (stringList.Count > 0)
                        {
                            return stringList.Select(s => new ColumnSettingsEntry { name = s, width = 100 }).ToList();
                        }
                    }
                    catch { }
                }
            }
            return null;
        }
    }
}
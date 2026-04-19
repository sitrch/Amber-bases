using System.Collections.Generic;
using System.Linq;
using AmberBases.UI.Tracking;

namespace AmberBases.UI;

public static class ColumnOrderStore
{
    private static InterfaceSettingsTracker _settings;

    public static void Initialize(InterfaceSettingsTracker settings)
    {
        _settings = settings;
    }

    public static List<ColumnSettingsEntry> GetColumns(string entityType)
    {
        var key = $"ColumnOrder_{entityType}";
        var value = _settings?.GetSetting(key);
        // Already materialized list of entries
        if (value is List<ColumnSettingsEntry> cols)
            return cols;
        // Simple string list
        if (value is List<string> stringList)
        {
            return stringList.Select(s => new ColumnSettingsEntry { name = s, width = 100 }).ToList();
        }
        // JSON array of strings or objects (handle robustly without direct deserialization)
        if (value is Newtonsoft.Json.Linq.JArray jArray)
        {
            // First, try as List<ColumnSettingsEntry>
            try
            {
                var list = jArray.ToObject<List<ColumnSettingsEntry>>();
                if (list != null && list.Count > 0 && list[0].name != null)
                    return list;
            }
            catch { }
            // Then try as List<string>
            try
            {
                var stringList2 = jArray.ToObject<List<string>>();
                if (stringList2 != null)
                    return stringList2.Select(s => new ColumnSettingsEntry { name = s, width = 100 }).ToList();
            }
            catch { }
        }
        // Legacy formats: JObject with Columns array
        if (value is Newtonsoft.Json.Linq.JObject jObj)
        {
            if (jObj.TryGetValue("Columns", out var colsToken) && colsToken is Newtonsoft.Json.Linq.JArray colsArray)
            {
                var result = new List<ColumnSettingsEntry>();
                foreach (var item in colsArray)
                {
                    if (item is Newtonsoft.Json.Linq.JObject obj)
                    {
                        var name = obj["Name"]?.ToString() ?? obj["name"]?.ToString();
                        var widthToken = obj["WidthPercent"] ?? obj["Width"];
                        var width = 100.0;
                        if (widthToken != null && double.TryParse(widthToken.ToString(), out var w))
                            width = w;
                        if (!string.IsNullOrEmpty(name))
                            result.Add(new ColumnSettingsEntry { name = name, width = width });
                    }
                    else if (item is Newtonsoft.Json.Linq.JValue jval && jval.Type == Newtonsoft.Json.Linq.JTokenType.String)
                    {
                        result.Add(new ColumnSettingsEntry { name = jval.ToString(), width = 100 });
                    }
                }
                if (result.Count > 0) return result;
            }
        }
        return null;
    }

    public static void SaveColumns(string entityType, List<ColumnSettingsEntry> columns)
    {
        if (columns == null || columns.Count == 0)
            return;

        _settings?.SaveColumnOrder(entityType, columns);
    }
}

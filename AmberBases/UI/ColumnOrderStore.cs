using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace AmberBases.UI;

public static class ColumnOrderStore
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AmberBases");

    private static readonly string SettingsFile = Path.Combine(SettingsFolder, "ColumnOrder.json");

    private static Dictionary<string, List<string>> _orders = new Dictionary<string, List<string>>();

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                _orders = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json)
                    ?? new Dictionary<string, List<string>>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ColumnOrderStore] Failed to load: {ex.Message}");
            _orders = new Dictionary<string, List<string>>();
        }
    }

    public static void Save()
    {
        try
        {
            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            var json = JsonConvert.SerializeObject(_orders, Formatting.Indented);
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ColumnOrderStore] Failed to save: {ex.Message}");
        }
    }

    public static List<string> GetOrder(string entityType)
    {
        return _orders.TryGetValue(entityType, out var order) ? order : null;
    }

    public static void SaveOrder(string entityType, List<string> order)
    {
        if (order == null || order.Count == 0)
            return;

        _orders[entityType] = order;
        Save();
    }
}
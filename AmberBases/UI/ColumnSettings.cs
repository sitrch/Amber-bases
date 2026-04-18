using System;
using System.Collections.Generic;

namespace AmberBases.UI;

public static class ColumnSettings
{
    private static readonly Dictionary<string, Dictionary<string, ColumnInfo>> _settings = new();
    private static readonly Dictionary<string, HashSet<string>> _excludedNavProps = new();
    private static readonly HashSet<string> _defaultExcludedNavProps = new()
    {
        "Manufacturer", "System", "Color", "StandartBarLength", "ProfileType",
        "Provider", "CoatingType", "Article"
    };

    public static void SetColumn(string entityType, string propertyName, string displayName, bool visible = true)
    {
        if (!_settings.ContainsKey(entityType))
            _settings[entityType] = new Dictionary<string, ColumnInfo>();
        
        _settings[entityType][propertyName] = new ColumnInfo(displayName, visible);
    }

    public static void SetExcludedNavProps(string entityType, params string[] propertyNames)
    {
        _excludedNavProps[entityType] = new HashSet<string>(propertyNames);
    }

    public static string GetColumnName(string entityType, string propertyName)
    {
        if (_settings.TryGetValue(entityType, out var typeSettings) &&
            typeSettings.TryGetValue(propertyName, out var colInfo))
            return colInfo.Name;
        
        return DisplayNameProvider.GetPropertyName(propertyName);
    }

    public static bool IsColumnVisible(string entityType, string propertyName)
    {
        if (_settings.TryGetValue(entityType, out var typeSettings) &&
            typeSettings.TryGetValue(propertyName, out var colInfo))
            return colInfo.Visible;
        
        return DisplayNameProvider.IsPropertyVisible(propertyName);
    }

    public static ColumnInfo GetColumnInfo(string entityType, string propertyName)
    {
        if (_settings.TryGetValue(entityType, out var typeSettings) &&
            typeSettings.TryGetValue(propertyName, out var colInfo))
            return colInfo;
        
        return DisplayNameProvider.GetColumnInfo(propertyName);
    }

    public static bool IsNavPropExcluded(string entityType, string propertyName)
    {
        if (_excludedNavProps.TryGetValue(entityType, out var excluded) &&
            excluded.Contains(propertyName))
            return true;
        
        return _defaultExcludedNavProps.Contains(propertyName);
    }

    public static void Initialize()
    {
        // Настройки для ProfileArticle
        SetColumn("ProfileArticle", "Name", "Артикул");
        SetColumn("ProfileArticle", "Code", "Код");

        // Настройки для CProfile
        SetColumn("CProfile", "Name", "Название профиля");
        SetColumn("CProfile", "Code", "Код");
        SetColumn("CProfile", "BOMArticle", "Код BOM", false); // скрыт

        // Настройки для Customer
        SetColumn("Customer", "Name", "Название");
        SetColumn("Customer", "Address", "Адрес");

        // Настройки для CustomerContact
        SetColumn("CustomerContact", "LastName", "Фамилия");
        SetColumn("CustomerContact", "FirstName", "Имя");
        SetColumn("CustomerContact", "Phone", "Телефон");

        // Исключить навигационные свойства для конкретных таблиц
        SetExcludedNavProps("CProfile", "Manufacturer", "System", "Color", "StandartBarLength");
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using AmberBases.Core.Models;

namespace AmberBases.UI;

public static class ColumnSettings
{
    private static bool _initialized;

    private static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
    }

    public static string GetColumnName(string entityType, string propertyName)
    {
        Initialize();
        return DisplayNameProvider.GetPropertyName(propertyName);
    }

    public static bool IsColumnVisible(string entityType, string propertyName)
    {
        Initialize();
        return DisplayNameProvider.IsPropertyVisible(propertyName);
    }

    public static ColumnInfo GetColumnInfo(string entityType, string propertyName)
    {
        Initialize();
        return DisplayNameProvider.GetColumnInfo(propertyName);
    }

    public static bool IsNavPropExcluded(string entityType, string propertyName)
    {
        var baseType = typeof(AmberBases.Core.Models.Dictionaries.BaseDictionaryModel);
        var type = baseType.Assembly.GetType($"AmberBases.Core.Models.Dictionaries.{entityType}");
        if (type == null) return false;

        var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null) return false;

        var propType = prop.PropertyType;
        if (propType.IsClass && propType != typeof(string) && !propType.IsPrimitive)
        {
            return true;
        }

        return false;
    }
}
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
        return DisplayNameProvider.GetPropertyName(entityType, propertyName);
    }

    public static bool IsColumnVisible(string entityType, string propertyName)
    {
        Initialize();
        return DisplayNameProvider.IsPropertyVisible(entityType, propertyName);
    }

    public static ColumnInfo GetColumnInfo(string entityType, string propertyName)
    {
        Initialize();
        return DisplayNameProvider.GetColumnInfo(entityType, propertyName);
    }

    public static bool IsNavPropExcluded(string entityType, string propertyName)
    {
        var info = GetColumnInfo(entityType, propertyName);
        return info != null && !info.Visible;
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using AmberBases.Core.Models;

public class ColumnInfo
{
    public string Name { get; set; }
    public bool Visible { get; set; } = true;

    public ColumnInfo() { }

    public ColumnInfo(string name, bool visible = true)
    {
        Name = name;
        Visible = visible;
    }
}

public static class DisplayNameProvider
{
    private static readonly Dictionary<string, string> TypeNames = new()
    {
        { "SystemProvider", "Поставщики систем" },
        { "ProfileSystem", "Профильные системы" },
        { "Color", "Цвета" },
        { "StandartBarLength", "Длина хлыста" },
        { "StandartBarLengths", "Длины хлыстов" },
        { "ProfileType", "Типы профилей" },
        { "ProfileArticle", "Артикулы" },
        { "CProfile", "Профили" },
        { "Applicability", "Применимость" },
        { "CoatingType", "Типы покрытий" },
        { "Customer", "Клиенты" },
        { "CustomerContact", "Контакты клиентов" }
    };

    private static readonly Dictionary<(string type, string prop), ColumnInfo> _propertyCache = new();
    private static bool _initialized;

    private static void Initialize()
    {
        if (_initialized) return;

        var baseType = typeof(AmberBases.Core.Models.Dictionaries.BaseDictionaryModel);
        var assembly = baseType.Assembly;

        var excludedTypes = new HashSet<string>
        {
            typeof(object).FullName,
            typeof(string).FullName,
            typeof(ValueType).FullName
        };

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in props)
                {
                    if (prop.DeclaringType != type)
                        continue;

                    var displayAttr = prop.GetCustomAttribute<ColumnDisplayNameAttribute>();
                    var visibleAttr = prop.GetCustomAttribute<ColumnVisibleAttribute>();

                    if (displayAttr == null && visibleAttr == null)
                    {
                        var propType = prop.PropertyType;
                        var isNavProp = propType.IsClass && !excludedTypes.Contains(propType.FullName);
                        if (isNavProp)
                            continue;

                        _propertyCache[(type.Name, prop.Name)] = new ColumnInfo(prop.Name, !isNavProp);
                        continue;
                    }

                    string name = displayAttr?.Name ?? prop.Name;
                    bool visible = displayAttr?.Visible ?? visibleAttr?.Visible ?? true;

                    if (displayAttr != null && !displayAttr.Visible)
                    {
                        visible = false;
                    }

                    _propertyCache[(type.Name, prop.Name)] = new ColumnInfo(name, visible);
                }
            }
        }

        _initialized = true;
    }

    public static string GetTypeName(string typeName) =>
        TypeNames.TryGetValue(typeName, out var name) ? name : typeName;

    public static string GetPropertyName(string propertyName) =>
        GetPropertyName(null, propertyName);

    public static string GetPropertyName(string entityType, string propertyName)
    {
        Initialize();
        var info = GetColumnInfo(entityType, propertyName);
        return info?.Name ?? propertyName;
    }

    public static bool IsPropertyVisible(string propertyName) =>
        IsPropertyVisible(null, propertyName);

    public static bool IsPropertyVisible(string entityType, string propertyName)
    {
        Initialize();
        var info = GetColumnInfo(entityType, propertyName);
        return info?.Visible ?? true;
    }

    public static ColumnInfo GetColumnInfo(string propertyName) =>
        GetColumnInfo(null, propertyName);

    public static ColumnInfo GetColumnInfo(string entityType, string propertyName)
    {
        Initialize();

        if (entityType != null)
        {
            var key = (entityType, propertyName);
            if (_propertyCache.TryGetValue(key, out var info))
                return info;
        }

        foreach (var kvp in _propertyCache)
        {
            if (kvp.Key.Item2 == propertyName)
                return kvp.Value;
        }

        return new ColumnInfo(propertyName, true);
    }

    public static string GetTypeName<T>() => GetTypeName(typeof(T).Name);
}
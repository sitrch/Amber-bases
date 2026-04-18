using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmberBases.Core.Models.Dictionaries;

namespace AmberBases.Helpers;

public static class ReflectionHelper
{
    public static bool IsNavigationalProperty(PropertyInfo prop)
    {
        if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
            return true;
        if (typeof(BaseDictionaryModel).IsAssignableFrom(prop.PropertyType))
            return true;
        return false;
    }

    public static bool IsSimpleType(Type type)
    {
        if (type.IsPrimitive) return true;
        if (type == typeof(string)) return true;
        if (type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid)) return true;
        if (type == typeof(TimeSpan) || type == typeof(DateTimeOffset)) return true;

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null) return IsSimpleType(underlying);

        if (type.IsEnum) return true;

        return false;
    }

    public static object GetPropertyValue(object source, PropertyInfo prop)
    {
        return prop.GetValue(source);
    }

    public static void SetPropertyValue(object target, PropertyInfo prop, object value)
    {
        if (!prop.CanWrite) return;

        try
        {
            if (value == null || value == DBNull.Value)
            {
                var underlying = Nullable.GetUnderlyingType(prop.PropertyType);
                prop.SetValue(target, underlying != null ? null :
                    (prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null));
            }
            else
            {
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                prop.SetValue(target, Convert.ChangeType(value, targetType));
            }
        }
        catch { }
    }

    public static PropertyInfo[] GetModelProperties(Type modelType)
    {
        return modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();
    }

    public static PropertyInfo[] GetSimpleProperties(Type modelType)
    {
        return modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !IsNavigationalProperty(p))
            .ToArray();
    }
}
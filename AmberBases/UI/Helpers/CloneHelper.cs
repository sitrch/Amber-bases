using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmberBases.Core.Models.Dictionaries;
using AmberBases.Helpers;

namespace AmberBases.UI;

public static class CloneHelper
{
    public static object DeepClone(object source)
    {
        if (source == null) return null;

        var type = source.GetType();
        if (type.IsValueType || type == typeof(string))
            return source;

        if (source is ICloneable cloneable)
            return cloneable.Clone();

        if (source is IList list)
            return CloneList(list, type);

        if (source is IEnumerable enumerable)
            return CloneEnumerable(enumerable, type);

        return CloneObject(source);
    }

    private static object CloneList(IList source, Type listType)
    {
        var elementType = listType.IsArray 
            ? listType.GetElementType() 
            : listType.GenericTypeArguments.FirstOrDefault() ?? typeof(object);
        
        var newList = (IList)Activator.CreateInstance(listType);
        foreach (var item in source)
        {
            newList.Add(DeepClone(item));
        }
        return newList;
    }

    private static object CloneEnumerable(IEnumerable source, Type type)
    {
        var elementType = type.GenericTypeArguments.FirstOrDefault() ?? typeof(object);
        var listType = typeof(List<>).MakeGenericType(elementType);
        var newList = (IList)Activator.CreateInstance(listType);
        foreach (var item in source)
        {
            newList.Add(DeepClone(item));
        }
        return newList;
    }

    private static object CloneObject(object source)
    {
        var type = source.GetType();
        var clone = Activator.CreateInstance(type);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            if (ReflectionHelper.IsNavigationalProperty(prop)) continue;

            var value = prop.GetValue(source);
            if (value == null) continue;

            var clonedValue = DeepClone(value);
            prop.SetValue(clone, clonedValue);
        }

        return clone;
    }
}

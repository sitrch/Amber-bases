using System;
using System.Collections.Generic;

namespace AmberBases.UI;

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

    private static readonly Dictionary<string, ColumnInfo> PropertyNames = new()
    {
        { "Name", new ColumnInfo("Название") },
        { "Description", new ColumnInfo("Описание") },
        { "Title", new ColumnInfo("Заголовок") },
        { "ColorName", new ColumnInfo("Название цвета") },
        { "RAL", new ColumnInfo("RAL код") },
        { "Length", new ColumnInfo("Длина") },
        { "Code", new ColumnInfo("Код") },
        { "BOMArticle", new ColumnInfo("Код BOM") },
        { "Article", new ColumnInfo("Артикул") },
        { "CutWisibleWidth", new ColumnInfo("Ширина реза") },
        { "Info", new ColumnInfo("Информация") },
        { "Position", new ColumnInfo("Позиция") },
        { "Address", new ColumnInfo("Адрес") },
        { "JobTitle", new ColumnInfo("Должность") },
        { "LastName", new ColumnInfo("Фамилия") },
        { "FirstName", new ColumnInfo("Имя") },
        { "MiddleName", new ColumnInfo("Отчество") },
        { "Phone", new ColumnInfo("Телефон") },
        { "ManufacturerId", new ColumnInfo("Производители") },
        { "SystemId", new ColumnInfo("Система") },
        { "ColorId", new ColumnInfo("Цвет") },
        { "StandartBarLengthId", new ColumnInfo("Длина хлыста") },
        { "ProfileTypeId", new ColumnInfo("Тип профиля") },
        { "CoatingTypeId", new ColumnInfo("Тип покрытия") },
        { "Id", new ColumnInfo("ID", false) }
    };

    public static string GetTypeName(string typeName) =>
        TypeNames.TryGetValue(typeName, out var name) ? name : typeName;

    public static string GetPropertyName(string propertyName) =>
        PropertyNames.TryGetValue(propertyName, out var info) ? info.Name : propertyName;

    public static bool IsPropertyVisible(string propertyName) =>
        PropertyNames.TryGetValue(propertyName, out var info) ? info.Visible : true;

    public static ColumnInfo GetColumnInfo(string propertyName)
    {
        if (PropertyNames.TryGetValue(propertyName, out var info))
            return info;
        return new ColumnInfo(propertyName, true);
    }

    public static string GetTypeName<T>() => GetTypeName(typeof(T).Name);
}

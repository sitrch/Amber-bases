using System;
using System.Collections.Generic;
using AmberBases.Core.Models.Dictionaries;

namespace AmberBases.Core.Repositories;

public interface IRepository<T> where T : BaseDictionaryModel
{
    List<T> GetAll(string dbPath);
    void Add(T item, string dbPath);
    void Update(T item, string dbPath);
    void Delete(int id, string dbPath);
    void Sync(string dbPath, IEnumerable<T> items);
}

public static class TableNameMapper
{
    private static readonly Dictionary<Type, string> _mappings = new()
    {
        { typeof(SystemProvider), "SystemProviders" },
        { typeof(ProfileSystem), "ProfileSystems" },
        { typeof(Color), "Colors" },
        { typeof(StandartBarLength), "StandartBarLengths" },
        { typeof(ProfileArticle), "ProfileArticles" },
        { typeof(ProfileType), "ProfileTypes" },
        { typeof(CProfile), "Profiles" },
        { typeof(Applicability), "Applicabilities" },
        { typeof(Customer), "Customers" },
        { typeof(CustomerContact), "CustomerContacts" },
        { typeof(CoatingType), "CoatingTypes" }
    };

    public static string GetTableName<T>() where T : BaseDictionaryModel
    {
        var type = typeof(T);
        if (_mappings.TryGetValue(type, out var tableName))
            return tableName;
        return type.Name + "s";
    }

    public static string GetTableName(Type entityType)
    {
        if (_mappings.TryGetValue(entityType, out var tableName))
            return tableName;
        return entityType.Name + "s";
    }
}

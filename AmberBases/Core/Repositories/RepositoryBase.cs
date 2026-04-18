using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using AmberBases.Core.Builders;
using AmberBases.Core.Models.Dictionaries;
using AmberBases.Helpers;

namespace AmberBases.Core.Repositories;

public abstract class RepositoryBase<T> : IRepository<T> where T : BaseDictionaryModel, new()
{
    protected string TableName => TableNameMapper.GetTableName<T>();

    public List<T> GetAll(string dbPath)
    {
        var result = new List<T>();
        var builder = new SqlBuilder<T>(TableName);
        var properties = ReflectionHelper.GetSimpleProperties(typeof(T));

        using (var connection = DbConnectionHelper.CreateAndOpen(dbPath))
        using (var command = new SQLiteCommand(builder.GetSelectSql(), connection))
        using (var reader = command.ExecuteReader())
        {
            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                columnNames.Add(reader.GetName(i));

            var propertyToColumnIndex = new Dictionary<string, int>();
            foreach (var prop in properties)
            {
                propertyToColumnIndex[prop.Name] = columnNames.Contains(prop.Name) 
                    ? reader.GetOrdinal(prop.Name) : -1;
            }

            while (reader.Read())
            {
                var item = new T();
                foreach (var prop in properties)
                {
                    if (!propertyToColumnIndex.TryGetValue(prop.Name, out int colIndex) || colIndex == -1)
                        continue;

                    try
                    {
                        var value = reader.IsDBNull(colIndex) ? null : reader.GetValue(colIndex);
                        ReflectionHelper.SetPropertyValue(item, prop, value);
                    }
                    catch { }
                }
                result.Add(item);
            }
        }
        return result;
    }

    public void Add(T item, string dbPath)
    {
        var builder = new SqlBuilder<T>(TableName);
        using (var connection = DbConnectionHelper.CreateAndOpen(dbPath))
        using (var command = new SQLiteCommand(builder.GetInsertSql(), connection))
        {
            foreach (var param in builder.GetParameters(item))
                command.Parameters.AddWithValue(param.Key, param.Value);
            item.Id = Convert.ToInt32(command.ExecuteScalar());
        }
    }

    public void Update(T item, string dbPath)
    {
        var builder = new SqlBuilder<T>(TableName);
        using (var connection = DbConnectionHelper.CreateAndOpen(dbPath))
        using (var command = new SQLiteCommand(builder.GetUpdateSql(), connection))
        {
            foreach (var param in builder.GetParameters(item))
                command.Parameters.AddWithValue(param.Key, param.Value);
            command.Parameters.AddWithValue("@Id", item.Id);
            command.ExecuteNonQuery();
        }
    }

    public void Delete(int id, string dbPath)
    {
        var builder = new SqlBuilder<T>(TableName);
        using (var connection = DbConnectionHelper.CreateAndOpen(dbPath))
        using (var command = new SQLiteCommand(builder.GetDeleteSql(), connection))
        {
            command.Parameters.AddWithValue("@Id", id);
            command.ExecuteNonQuery();
        }
    }

    public void Sync(string dbPath, IEnumerable<T> items)
    {
        var existing = GetAll(dbPath).ToDictionary(x => x.Id);
        var newItems = items.ToList();

        using (var connection = DbConnectionHelper.CreateAndOpen(dbPath))
        using (var transaction = connection.BeginTransaction())
        {
            foreach (var item in newItems)
            {
                if (item.Id == 0 || !existing.ContainsKey(item.Id))
                    Add(item, dbPath);
                else if (HasChanges(existing[item.Id], item))
                    Update(item, dbPath);
            }
            transaction.Commit();
        }
    }

    protected virtual bool HasChanges(T original, T updated) => true;
}

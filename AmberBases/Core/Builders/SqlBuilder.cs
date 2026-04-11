using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AmberBases.Core.Models.Dictionaries;

namespace AmberBases.Core.Builders
{
    /// <summary>
    /// Генератор SQL-запросов для моделей, основанный на рефлексии.
    /// </summary>
    public class SqlBuilder<T> where T : BaseDictionaryModel
    {
        private readonly string _tableName;
        private readonly PropertyInfo[] _properties;
        private readonly List<string> _columns;
        private readonly List<string> _nonIdColumns;

        public SqlBuilder(string tableName)
        {
            _tableName = tableName;
            _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                // Исключаем навигационные свойства (обычно это коллекции или классы-сущности)
                .Where(p => 
                {
                    bool isGenericCollection = p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() != typeof(Nullable<>);
                    return !isGenericCollection && !typeof(BaseDictionaryModel).IsAssignableFrom(p.PropertyType);
                })
                .ToArray();

            _columns = _properties.Select(p => p.Name).ToList();
            _nonIdColumns = _columns.Where(c => !c.Equals("Id", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public string GetCreateTableSql()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE IF NOT EXISTS [{_tableName}] (");
            
            var columnDefs = new List<string>();
            foreach (var prop in _properties)
            {
                if (prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    columnDefs.Add($"[Id] INTEGER PRIMARY KEY AUTOINCREMENT");
                }
                else
                {
                    string sqliteType = GetSqliteType(prop.PropertyType);
                    columnDefs.Add($"[{prop.Name}] {sqliteType}");
                }
            }
            
            sb.Append(string.Join(",\n    ", columnDefs));
            sb.AppendLine("\n);");
            
            return sb.ToString();
        }

        public string GetSelectSql()
        {
            return $"SELECT * FROM [{_tableName}] ORDER BY [Position]";
        }

        public string GetInsertSql()
        {
            var columns = string.Join(", ", _nonIdColumns.Select(c => $"[{c}]"));
            var parameters = string.Join(", ", _nonIdColumns.Select(c => $"@{c}"));
            
            return $"INSERT INTO [{_tableName}] ({columns}) VALUES ({parameters});\nSELECT last_insert_rowid();";
        }

        public string GetUpdateSql()
        {
            var setClause = string.Join(", ", _nonIdColumns.Select(c => $"[{c}] = @{c}"));
            return $"UPDATE [{_tableName}] SET {setClause} WHERE [Id] = @Id;";
        }

        public string GetDeleteSql()
        {
            return $"DELETE FROM [{_tableName}] WHERE [Id] = @Id;";
        }

        public Dictionary<string, object> GetParameters(T entity)
        {
            var parameters = new Dictionary<string, object>();
            foreach (var prop in _properties)
            {
                var value = prop.GetValue(entity);
                parameters.Add($"@{prop.Name}", value ?? DBNull.Value);
            }
            return parameters;
        }

        private string GetSqliteType(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            if (underlyingType == typeof(int) || underlyingType == typeof(long) || underlyingType == typeof(bool))
                return "INTEGER";
            if (underlyingType == typeof(double) || underlyingType == typeof(float) || underlyingType == typeof(decimal))
                return "REAL";
            return "TEXT";
        }
    }
}
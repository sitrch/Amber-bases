using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using AmberBases.Core.Models.Dictionaries;
using AmberBases.Core.Builders;
using AmberBases.Helpers;

namespace AmberBases.Services
{
    public class SqliteDictionaryDataService : IDictionaryDataService
    {
        public void InitializeDatabase(string dbPath)
        {
            bool isNew = !File.Exists(dbPath);
            if (isNew)
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                
                using (var transaction = connection.BeginTransaction())
                {
                    CreateTable<SystemProvider>("SystemProviders", connection);
                    AddColumnIfNotExists(connection, "SystemProviders", "Information", "TEXT");
                    CreateTable<ProfileSystem>("ProfileSystems", connection);
                    AddColumnIfNotExists(connection, "ProfileSystems", "Description", "TEXT");
                    CreateTable<Color>("Colors", connection);
                    AddColumnIfNotExists(connection, "Colors", "ColorName", "TEXT");
                    AddColumnIfNotExists(connection, "Colors", "RAL", "INTEGER");
                    AddColumnIfNotExists(connection, "Colors", "CoatingTypeId", "INTEGER");
                    CreateTable<StandartBarLength>("StandartBarLengths", connection);
                    CreateTable<ProfileType>("ProfileTypes", connection);
                    CreateTable<Applicability>("Applicabilities", connection);
                    CreateTable<ProfileArticle>("ProfileArticles", connection);
                    AddColumnIfNotExists(connection, "ProfileArticles", "ManufacturerId", "INTEGER");
                    AddColumnIfNotExists(connection, "ProfileArticles", "SystemId", "INTEGER");
                    AddColumnIfNotExists(connection, "ProfileArticles", "Code", "TEXT");
                    AddColumnIfNotExists(connection, "ProfileArticles", "BOMArticle", "TEXT");
                    AddColumnIfNotExists(connection, "ProfileArticles", "Title", "TEXT");
                    AddColumnIfNotExists(connection, "ProfileArticles", "Description", "TEXT");
                    AddColumnIfNotExists(connection, "ProfileArticles", "ColorId", "INTEGER");
                    AddColumnIfNotExists(connection, "ProfileArticles", "CutWisibleWidth", "REAL");
                    AddColumnIfNotExists(connection, "ProfileArticles", "StandartBarLength", "REAL");
                    AddColumnIfNotExists(connection, "ProfileArticles", "ProfileTypeId", "INTEGER");
                    AddColumnIfNotExists(connection, "ProfileArticles", "StandartBarLengthId", "INTEGER");
                    CreateTable<CProfile>("Profiles", connection);
                    CreateTable<Customer>("Customers", connection);
                    CreateTable<CustomerContact>("CustomerContacts", connection);
                    CreateTable<CoatingType>("CoatingTypes", connection);

                    transaction.Commit();
                }
            }
        }

        private void CreateTable<T>(string tableName, SQLiteConnection connection) where T : BaseDictionaryModel
        {
            var builder = new SqlBuilder<T>(tableName);
            ExecuteNonQuery(connection, builder.GetCreateTableSql());
        }

        private void ExecuteNonQuery(SQLiteConnection connection, string query)
        {
            using (var command = new SQLiteCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private bool ColumnExists(SQLiteConnection connection, string tableName, string columnName)
        {
            using (var command = new SQLiteCommand($"PRAGMA table_info({tableName});", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private void AddColumnIfNotExists(SQLiteConnection connection, string tableName, string columnName, string sqlDef)
        {
            if (!ColumnExists(connection, tableName, columnName))
            {
                ExecuteNonQuery(connection, $"ALTER TABLE {tableName} ADD COLUMN {columnName} {sqlDef};");
            }
        }

        // Обобщенный метод получения списка
        public List<T> GetItems<T>(string tableName, string dbPath) where T : BaseDictionaryModel, new()
        {
            var result = new List<T>();
            var builder = new SqlBuilder<T>(tableName);
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(builder.GetSelectSql(), connection))
                using (var reader = command.ExecuteReader())
                {
                    // Получаем схему таблицы и индексы колонок вручную
                    var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        columnNames.Add(reader.GetName(i));
                    }
                    
                    // Создаём маппинг: имя свойства -> индекс колонки (или -1 если колонки нет)
                    var propertyToColumnIndex = new Dictionary<string, int>();
                    foreach (var prop in properties)
                    {
                        // Игнорируем коллекции
                        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                            continue;
                        
                        // Игнорируем навигационные свойства (классы-сущности)
                        if (typeof(BaseDictionaryModel).IsAssignableFrom(prop.PropertyType))
                            continue;
                        
                        // Проверяем существование колонки
                        if (columnNames.Contains(prop.Name))
                        {
                            propertyToColumnIndex[prop.Name] = reader.GetOrdinal(prop.Name);
                        }
                        else
                        {
                            propertyToColumnIndex[prop.Name] = -1;
                        }
                    }
                    
                    while (reader.Read())
                    {
                        var item = new T();
                        foreach (var prop in properties)
                        {
                            int colIndex;
                            if (!propertyToColumnIndex.TryGetValue(prop.Name, out colIndex))
                                continue;
                            
                            // Колонка не существует в таблице - пропускаем
                            if (colIndex == -1)
                                continue;
                            
                            try
                            {
                                // Проверяем на NULL
                                if (reader.IsDBNull(colIndex))
                                {
                                    // Для nullable типов - null, для value types - значение по умолчанию
                                    if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                                    {
                                        prop.SetValue(item, null);
                                    }
                                    else
                                    {
                                        // Значение по умолчанию для типа (0, false, и т.д.)
                                        prop.SetValue(item, prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null);
                                    }
                                }
                                else
                                {
                                    Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                    var val = Convert.ChangeType(reader.GetValue(colIndex), targetType);
                                    prop.SetValue(item, val);
                                }
                            }
                            catch
                            {
                                // Игнорируем ошибки маппинга для простоты
                            }
                        }
                        result.Add(item);
                    }
                }
            }
            return result;
        }

        // Обобщенный метод добавления
        public void AddItem<T>(T item, string tableName, string dbPath) where T : BaseDictionaryModel
        {
            var builder = new SqlBuilder<T>(tableName);
            using (var connection = DbConnectionHelper.CreateAndOpen(dbPath))
            using (var command = new SQLiteCommand(builder.GetInsertSql(), connection))
            {
                var parameters = builder.GetParameters(item);
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }
                item.Id = Convert.ToInt32(command.ExecuteScalar());
            }
        }

        // Обобщенный метод обновления
        public void UpdateItem<T>(T item, string tableName, string dbPath) where T : BaseDictionaryModel
        {
            var builder = new SqlBuilder<T>(tableName);
            using (var connection = DbConnectionHelper.CreateAndOpen(dbPath))
            using (var command = new SQLiteCommand(builder.GetUpdateSql(), connection))
            {
                var parameters = builder.GetParameters(item);
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }
                command.Parameters.AddWithValue("@Id", item.Id);
                command.ExecuteNonQuery();
            }
        }

        // Обобщенный метод удаления
        public void DeleteItem<T>(int id, string tableName, string dbPath) where T : BaseDictionaryModel
        {
            var builder = new SqlBuilder<T>(tableName);
            using (var connection = DbConnectionHelper.CreateAndOpen(dbPath))
            using (var command = new SQLiteCommand(builder.GetDeleteSql(), connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }

        // --- SystemProvider ---
        public List<SystemProvider> GetSystemProviders(string dbPath) => GetItems<SystemProvider>("SystemProviders", dbPath);
        public void AddSystemProvider(SystemProvider provider, string dbPath) => AddItem(provider, "SystemProviders", dbPath);
        public void UpdateSystemProvider(SystemProvider provider, string dbPath) => UpdateItem(provider, "SystemProviders", dbPath);
        public void DeleteSystemProvider(int id, string dbPath) => DeleteItem<SystemProvider>(id, "SystemProviders", dbPath);

        // --- ProfileSystem ---
        public List<ProfileSystem> GetProfileSystems(string dbPath) => GetItems<ProfileSystem>("ProfileSystems", dbPath);
        public void AddProfileSystem(ProfileSystem system, string dbPath) => AddItem(system, "ProfileSystems", dbPath);
        public void UpdateProfileSystem(ProfileSystem system, string dbPath) => UpdateItem(system, "ProfileSystems", dbPath);
        public void DeleteProfileSystem(int id, string dbPath) => DeleteItem<ProfileSystem>(id, "ProfileSystems", dbPath);

        // --- Color ---
        public List<Color> GetColors(string dbPath) => GetItems<Color>("Colors", dbPath);
        public void AddColor(Color color, string dbPath) => AddItem(color, "Colors", dbPath);
        public void UpdateColor(Color color, string dbPath) => UpdateItem(color, "Colors", dbPath);
        public void DeleteColor(int id, string dbPath) => DeleteItem<Color>(id, "Colors", dbPath);

        // --- StandartBarLength ---
        public List<StandartBarLength> GetStandartBarLengths(string dbPath) => GetItems<StandartBarLength>("StandartBarLengths", dbPath);
        public void AddStandartBarLength(StandartBarLength length, string dbPath) => AddItem(length, "StandartBarLengths", dbPath);
        public void UpdateStandartBarLength(StandartBarLength length, string dbPath) => UpdateItem(length, "StandartBarLengths", dbPath);
        public void DeleteStandartBarLength(int id, string dbPath) => DeleteItem<StandartBarLength>(id, "StandartBarLengths", dbPath);

        // --- ProfileArticle ---
        public List<ProfileArticle> GetProfileArticles(string dbPath) => GetItems<ProfileArticle>("ProfileArticles", dbPath);
        public void AddProfileArticle(ProfileArticle article, string dbPath) => AddItem(article, "ProfileArticles", dbPath);
        public void UpdateProfileArticle(ProfileArticle article, string dbPath) => UpdateItem(article, "ProfileArticles", dbPath);
        public void DeleteProfileArticle(int id, string dbPath) => DeleteItem<ProfileArticle>(id, "ProfileArticles", dbPath);

        // --- ProfileType ---
        public List<ProfileType> GetProfileTypes(string dbPath) => GetItems<ProfileType>("ProfileTypes", dbPath);
        public void AddProfileType(ProfileType item, string dbPath) => AddItem(item, "ProfileTypes", dbPath);
        public void UpdateProfileType(ProfileType item, string dbPath) => UpdateItem(item, "ProfileTypes", dbPath);
        public void DeleteProfileType(int id, string dbPath) => DeleteItem<ProfileType>(id, "ProfileTypes", dbPath);

        // --- CProfile ---
        public List<CProfile> GetCProfiles(string dbPath) => GetItems<CProfile>("Profiles", dbPath);
        public void AddCProfile(CProfile profile, string dbPath) => AddItem(profile, "Profiles", dbPath);
        public void UpdateCProfile(CProfile profile, string dbPath) => UpdateItem(profile, "Profiles", dbPath);
        public void DeleteCProfile(int id, string dbPath) => DeleteItem<CProfile>(id, "Profiles", dbPath);

        // --- Applicability ---
        public List<Applicability> GetApplicabilities(string dbPath) => GetItems<Applicability>("Applicabilities", dbPath);
        public void AddApplicability(Applicability item, string dbPath) => AddItem(item, "Applicabilities", dbPath);
        public void UpdateApplicability(Applicability item, string dbPath) => UpdateItem(item, "Applicabilities", dbPath);
        public void DeleteApplicability(int id, string dbPath) => DeleteItem<Applicability>(id, "Applicabilities", dbPath);
        
        // --- Customers ---
        public List<Customer> GetCustomers(string dbPath) => GetItems<Customer>("Customers", dbPath);
        public void AddCustomer(Customer customer, string dbPath) => AddItem(customer, "Customers", dbPath);
        public void UpdateCustomer(Customer customer, string dbPath) => UpdateItem(customer, "Customers", dbPath);
        public void DeleteCustomer(int id, string dbPath) => DeleteItem<Customer>(id, "Customers", dbPath);

        // --- CustomerContacts ---
        public List<CustomerContact> GetCustomerContacts(string dbPath) => GetItems<CustomerContact>("CustomerContacts", dbPath);
        public void AddCustomerContact(CustomerContact contact, string dbPath) => AddItem(contact, "CustomerContacts", dbPath);
        public void UpdateCustomerContact(CustomerContact contact, string dbPath) => UpdateItem(contact, "CustomerContacts", dbPath);
        public void DeleteCustomerContact(int id, string dbPath) => DeleteItem<CustomerContact>(id, "CustomerContacts", dbPath);
        
        // --- CoatingType ---
        public List<CoatingType> GetCoatingTypes(string dbPath) => GetItems<CoatingType>("CoatingTypes", dbPath);
        public void AddCoatingType(CoatingType coatingType, string dbPath) => AddItem(coatingType, "CoatingTypes", dbPath);
        public void UpdateCoatingType(CoatingType coatingType, string dbPath) => UpdateItem(coatingType, "CoatingTypes", dbPath);
        public void DeleteCoatingType(int id, string dbPath) => DeleteItem<CoatingType>(id, "CoatingTypes", dbPath);

        List<T> IDictionaryDataService.GetItems<T>(string dbPath) => GetItems<T>(GetTableName<T>(), dbPath);
        void IDictionaryDataService.AddItem<T>(T item, string dbPath) => AddItem(item, GetTableName<T>(), dbPath);
        void IDictionaryDataService.UpdateItem<T>(T item, string dbPath) => UpdateItem(item, GetTableName<T>(), dbPath);
        void IDictionaryDataService.DeleteItem<T>(int id, string dbPath) => DeleteItem<T>(id, GetTableName<T>(), dbPath);

        private static readonly Dictionary<Type, string> _tableNames = new()
        {
            { typeof(CProfile), "Profiles" }
        };

        private static string GetTableName<T>()
        {
            if (_tableNames.TryGetValue(typeof(T), out var name))
                return name;
            var typeName = typeof(T).Name;
            if (typeName.EndsWith("y")) return typeName.Substring(0, typeName.Length - 1) + "ies";
            return typeName + "s";
        }
    }
}

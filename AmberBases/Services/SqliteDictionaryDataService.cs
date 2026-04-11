using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using AmberBases.Core.Models.Dictionaries;
using AmberBases.Core.Builders;

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
                    // Create tables using reflection
                    CreateTable<SystemProvider>("SystemProviders", connection);
                    // Add Information column to SystemProviders if not exists
                    try { ExecuteNonQuery(connection, "ALTER TABLE SystemProviders ADD COLUMN Information TEXT;"); } catch { /* Column exists */ }
                    CreateTable<ProfileSystem>("ProfileSystems", connection);
                    // Add Description column to ProfileSystems if not exists
                    try { ExecuteNonQuery(connection, "ALTER TABLE ProfileSystems ADD COLUMN Description TEXT;"); } catch { /* Column exists */ }
                    CreateTable<Color>("Colors", connection);
                    // Add columns to Colors if not exists
                    try { ExecuteNonQuery(connection, "ALTER TABLE Colors ADD COLUMN ColorName TEXT;"); } catch { /* Column exists */ }
                    try { ExecuteNonQuery(connection, "ALTER TABLE Colors ADD COLUMN RAL INTEGER;"); } catch { /* Column exists */ }
                    try { ExecuteNonQuery(connection, "ALTER TABLE Colors ADD COLUMN CoatingTypeId INTEGER;"); } catch { /* Column exists */ }
                    CreateTable<WhipLength>("WhipLengths", connection);
                    CreateTable<ProfileType>("ProfileTypes", connection);
                    CreateTable<Applicability>("Applicabilities", connection);
                    CreateTable<ProfileArticle>("ProfileArticles", connection);
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
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(builder.GetInsertSql(), connection))
                {
                    var parameters = builder.GetParameters(item);
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                    var id = Convert.ToInt32(command.ExecuteScalar());
                    item.Id = id;
                }
            }
        }

        // Обобщенный метод обновления
        public void UpdateItem<T>(T item, string tableName, string dbPath) where T : BaseDictionaryModel
        {
            var builder = new SqlBuilder<T>(tableName);
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
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
        }

        // Обобщенный метод удаления
        public void DeleteItem<T>(int id, string tableName, string dbPath) where T : BaseDictionaryModel
        {
            var builder = new SqlBuilder<T>(tableName);
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(builder.GetDeleteSql(), connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
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

        // --- WhipLength ---
        public List<WhipLength> GetWhipLengths(string dbPath) => GetItems<WhipLength>("WhipLengths", dbPath);
        public void AddWhipLength(WhipLength length, string dbPath) => AddItem(length, "WhipLengths", dbPath);
        public void UpdateWhipLength(WhipLength length, string dbPath) => UpdateItem(length, "WhipLengths", dbPath);
        public void DeleteWhipLength(int id, string dbPath) => DeleteItem<WhipLength>(id, "WhipLengths", dbPath);

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
    }
}

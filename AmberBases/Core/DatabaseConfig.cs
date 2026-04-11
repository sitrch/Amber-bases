using System;

namespace AmberBases.Core
{
    /// <summary>
    /// Конфигурация базы данных.
    /// Централизованное хранение настроек базы данных.
    /// </summary>
    public static class DatabaseConfig
    {
        // Имя файла базы данных SQLite (основная)
        public const string SqliteDatabaseFile = "AmberBases.sqlite";
        
        // Имя файла базы данных SQLite (словари)
        public const string SqliteDictionariesDatabaseFile = "AmberDictionaries.sqlite";
    }
}

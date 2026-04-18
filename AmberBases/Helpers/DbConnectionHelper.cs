using System.Data.SQLite;

namespace AmberBases.Helpers;

public static class DbConnectionHelper
{
    private const string ConnectionStringTemplate = "Data Source={0};Version=3;";

    public static string BuildConnectionString(string dbPath) =>
        string.Format(ConnectionStringTemplate, dbPath);

    public static SQLiteConnection CreateConnection(string dbPath)
    {
        return new SQLiteConnection(BuildConnectionString(dbPath));
    }

    public static SQLiteConnection CreateAndOpen(string dbPath)
    {
        var connection = CreateConnection(dbPath);
        connection.Open();
        return connection;
    }
}
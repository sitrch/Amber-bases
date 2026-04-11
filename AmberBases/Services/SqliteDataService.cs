using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace AmberBases.Services
{
    /// <summary>
    /// Сервис для работы с базой данных SQLite.
    /// Реализует ISqliteDataService.
    /// </summary>
    public class SqliteDataService : ISqliteDataService
    {
        /// <summary>
        /// Сохраняет DataSet в файл базы данных SQLite, создавая таблицы и вставляя данные.
        /// </summary>
        /// <param name="dataSet">DataSet для сохранения.</param>
        /// <param name="dbFilePath">Путь к файлу базы данных.</param>
        public void SaveDataSet(DataSet dataSet, string dbFilePath)
        {
            if (File.Exists(dbFilePath))
            {
                File.Delete(dbFilePath);
            }

            SQLiteConnection.CreateFile(dbFilePath);

            string connectionString = $"Data Source={dbFilePath};Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    foreach (DataTable table in dataSet.Tables)
                    {
                        CreateTable(connection, table);
                        InsertData(connection, table);
                    }
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Создает таблицу в SQLite на основе структуры DataTable.
        /// </summary>
        private void CreateTable(SQLiteConnection connection, DataTable table)
        {
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE IF NOT EXISTS [{table.TableName}] (");

            for (int i = 0; i < table.Columns.Count; i++)
            {
                sb.Append($"[{table.Columns[i].ColumnName}] TEXT");
                if (i < table.Columns.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(");");

            using (var command = new SQLiteCommand(sb.ToString(), connection))
            {
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Вставляет данные из DataTable в соответствующую таблицу SQLite.
        /// </summary>
        private void InsertData(SQLiteConnection connection, DataTable table)
        {
            if (table.Rows.Count == 0 || table.Columns.Count == 0) return;

            var sbInsert = new StringBuilder();
            sbInsert.Append($"INSERT INTO [{table.TableName}] (");

            for (int i = 0; i < table.Columns.Count; i++)
            {
                sbInsert.Append($"[{table.Columns[i].ColumnName}]");
                if (i < table.Columns.Count - 1) sbInsert.Append(", ");
            }
            
            sbInsert.Append(") VALUES (");

            for (int i = 0; i < table.Columns.Count; i++)
            {
                sbInsert.Append($"@p{i}");
                if (i < table.Columns.Count - 1) sbInsert.Append(", ");
            }
            sbInsert.Append(");");

            using (var command = new SQLiteCommand(sbInsert.ToString(), connection))
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    command.Parameters.Add(new SQLiteParameter($"@p{i}"));
                }

                foreach (DataRow row in table.Rows)
                {
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        command.Parameters[$"@p{i}"].Value = row[i] == DBNull.Value ? (object)DBNull.Value : row[i].ToString();
                    }
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}

using System.Data;

namespace AmberBases.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с базой данных SQLite.
    /// </summary>
    public interface ISqliteDataService
    {
        /// <summary>
        /// Сохраняет DataSet в файл базы данных SQLite, создавая таблицы и вставляя данные.
        /// </summary>
        /// <param name="dataSet">DataSet для сохранения.</param>
        /// <param name="dbFilePath">Путь к файлу базы данных.</param>
        void SaveDataSet(DataSet dataSet, string dbFilePath);
    }
}

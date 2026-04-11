using System.Data;

namespace AmberBases.Services
{
    /// <summary>
    /// Интерфейс сервиса для чтения данных из Excel файлов.
    /// </summary>
    public interface IExcelDataService
    {
        /// <summary>
        /// Загружает все данные из всех листов Excel файла в DataSet.
        /// </summary>
        /// <param name="filePath">Путь к файлу Excel.</param>
        /// <returns>DataSet с загруженными таблицами.</returns>
        DataSet LoadData(string filePath);
    }
}

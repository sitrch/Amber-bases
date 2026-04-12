using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AmberBases.Dataset
{
    public class KeyMappingRules
    {
        /// <summary>
        /// Возвращает таблицу "Плоскости" с правилами маппинга.
        /// </summary>
        /// <param name="loadedTableNames">Список имен загруженных таблиц</param>
        /// <param name="floorTable">Таблица "Плоскости"</param>
        /// <returns>DataTable таблица "Плоскости"</returns>
        public static DataTable GetMappingRules(IEnumerable<string> loadedTableNames, DataTable floorTable = null)
        {
            return floorTable;
        }

        /// <summary>
        /// Получает таблицу "РядыРигелей" для указанной плоскости из DataSet.
        /// </summary>
        /// <param name="dataSet">DataSet с загруженными данными из Excel</param>
        /// <param name="плоскость">Идентификатор плоскости (например, "(5-1)-(5-6)")</param>
        /// <returns>DataTable таблица "РядыРигелей{плоскость}" или null, если таблица не найдена</returns>
        public static DataTable GetРядыРигелейForPlane(DataSet dataSet, string плоскость)
        {
            string tableName = $"РядыРигелей{плоскость}";
            return dataSet.Tables.Contains(tableName) ? dataSet.Tables[tableName] : null;
        }
    }
}

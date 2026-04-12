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
    }
}

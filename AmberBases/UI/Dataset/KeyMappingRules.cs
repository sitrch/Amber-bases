using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AmberBases.Dataset
{
    public class KeyMappingRules
    {
        /// <summary>
        /// Возвращает таблицу с правилами поиска ключей в ячейках баз данных.
        /// Главная таблица - РядыРигелейXXX.
        /// </summary>
        /// <param name="loadedTableNames">Список имен загруженных таблиц</param>
        /// <returns>DataTable с правилами маппинга</returns>
        public static DataTable GetMappingRules(IEnumerable<string> loadedTableNames)
        {
            DataTable table = new DataTable("KeyMappingRules");
            table.Columns.Add("MainTable", typeof(string));
            table.Columns.Add("ColumnName", typeof(string));
            table.Columns.Add("DataType", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("TargetTable", typeof(string));
            table.Columns.Add("TargetColumn", typeof(string));

            var regex = new Regex(@"^РядыСтоек(.*)$", RegexOptions.IgnoreCase);
            var requiredTables = new HashSet<string>();

            foreach (var tableName in loadedTableNames)
            {
                var match = regex.Match(tableName);
                if (match.Success)
                {
                    string xxx = match.Groups[1].Value;
                    string mainTable = $"РядыРигелей{xxx}";

                    void AddRule(string colName, string dataType, string desc, string targetTable, string targetCol = "")
                    {
                        table.Rows.Add(mainTable, colName, dataType, desc, targetTable, targetCol);
                        requiredTables.Add(targetTable);
                    }

                    AddRule("ТипРядовСтоек", "int/float", "ID типа ряда стоек (указывает на столбец 'тип')", $"РядыСтоек{xxx}", "тип");
                    AddRule("ТипРядовЗаполнений", "string", "Тип заполнения (значение ячейки = имя столбца в целевой таблице)", $"РядыЗаполнений{xxx}", "{Value}");
                    AddRule("ТипКреплений", "string", "Тип крепления (значение ячейки = имя столбца в целевой таблице)", $"РядыКреплений{xxx}", "{Value}");
                    AddRule("Подпорки", "string", "Тип подпорки (значение ячейки = имя столбца в целевой таблице)", $"РядыКреплений{xxx}", "{Value}");
                    AddRule("Опоры", "string", "Тип опоры (значение ячейки = имя столбца в целевой таблице)", $"РядыОпор{xxx}", "{Value}");
                }
            }

            var missingTables = requiredTables.Where(t => !loadedTableNames.Contains(t)).ToList();
            if (missingTables.Any())
            {
                MessageBox.Show(
                    $"Следующие целевые таблицы для связей отсутствуют:\n{string.Join("\n", missingTables)}",
                    "Внимание: Отсутствуют таблицы маппинга",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return table;
        }
    }
}

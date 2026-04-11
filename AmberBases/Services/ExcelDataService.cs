using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ExcelDataReader;
using AmberBases.Dataset;

namespace AmberBases.Services
{
    /// <summary>
    /// Сервис для чтения данных из Excel файлов.
    /// Реализует IExcelDataService.
    /// </summary>
    public class ExcelDataService : IExcelDataService
    {
        /// <summary>
        /// Загружает все данные из всех листов Excel файла в DataSet.
        /// </summary>
        /// <param name="filePath">Путь к файлу Excel.</param>
        /// <returns>DataSet с загруженными таблицами.</returns>
        public DataSet LoadData(string filePath)
        {
            var resultDataSet = new DataSet("AmberBases");

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var excelDataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                // Выводим доступные таблицы для отладки
                Console.WriteLine($"Доступные таблицы в файле: {string.Join(", ", excelDataSet.Tables.Cast<DataTable>().Select(t => t.TableName))}");

                foreach (DataTable originalTable in excelDataSet.Tables)
                {
                    try
                    {
                        var processedTable = ProcessTable(originalTable, originalTable.TableName);
                        resultDataSet.Tables.Add(processedTable);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при обработке листа {originalTable.TableName}: {ex.Message}");
                    }
                }
            }

            var tableNames = resultDataSet.Tables.Cast<DataTable>().Select(t => t.TableName).ToList();
            var mappingRulesTable = KeyMappingRules.GetMappingRules(tableNames);
            
            if (mappingRulesTable.Rows.Count == 0)
            {
                MessageBox.Show("Не найдены таблицы 'РядыСтоек...', соответствующие правилу маппинга.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
            resultDataSet.Tables.Add(mappingRulesTable);

            return resultDataSet;
        }

        /// <summary>
        /// Обрабатывает исходную таблицу от ExcelDataReader (очищает пустые строки, уникализирует колонки).
        /// </summary>
        private DataTable ProcessTable(DataTable sourceTable, string tableName)
        {
            var dataTable = new DataTable(tableName);

            // Обработка колонок
            for (int col = 0; col < sourceTable.Columns.Count; col++)
            {
                var columnName = sourceTable.Columns[col].ColumnName;
                
                // Пропускаем столбцы с пустыми или автогенерированными именами
                if (string.IsNullOrWhiteSpace(columnName) || columnName.StartsWith("Column", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                // Уникальность имени колонки
                string uniqueColName = columnName;
                int suffix = 1;
                while (dataTable.Columns.Contains(uniqueColName))
                {
                    uniqueColName = $"{columnName}_{suffix++}";
                }

                dataTable.Columns.Add(uniqueColName, typeof(string));
            }

            // Обработка строк
            foreach (DataRow sourceRow in sourceTable.Rows)
            {
                var dataRow = dataTable.NewRow();
                bool hasData = false;
                int dataRowIndex = 0;
                
                for (int col = 0; col < sourceTable.Columns.Count; col++)
                {
                    var columnName = sourceTable.Columns[col].ColumnName;
                    
                    // Пропускаем те же столбцы, что были пропущены при создании таблицы
                    if (string.IsNullOrWhiteSpace(columnName) || columnName.StartsWith("Column", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    var cellValue = sourceRow[col]?.ToString();
                    dataRow[dataRowIndex] = cellValue;
                    if (!string.IsNullOrWhiteSpace(cellValue))
                        hasData = true;
                    
                    dataRowIndex++;
                }
                
                if (hasData)
                    dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }
    }
}
using AmberBases.Dataset;
using AmberBases.Facade;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmberBases.Facade
{
    public static class DataExtractor
    {
        /// <summary>
        /// Извлекает данные о рядах ригелей для заданного этажа
        /// </summary>
        /// <param name="dataSet">DataSet с загруженными данными из Excel</param>
        /// <param name="плоскость">Идентификатор плоскости (например, "(5-1)-(5-6)")</param>
        /// <param name="этаж">Номер этажа</param>
        /// <returns>Объект CFloor, содержащий данные о рядах ригелей</returns>
        public static CFloor GetFloor(DataSet dataSet, string плоскость, int этаж)
        {
            // Получаем таблицу "РядыРигелей" для указанной плоскости
            string tableName = $"РядыРигелей{плоскость}";
            DataTable рядыРигелей = dataSet.Tables.Contains(tableName) ? dataSet.Tables[tableName] : null;

            if (рядыРигелей == null)
            {
                return null;
            }

            // Фильтрация строк по заданному этажу
            DataRow[] rows = рядыРигелей.Select($"Этаж = { этаж}", "РядИзделия ASC");

            if (rows.Length == 0)
            {
                return null;
            }

            // Создаем объект CFloor
            CFloor floor = new CFloor();

            // Берем самый верхний ряд (с минимальным значением "РядИзделия")
            // Поскольку у нас отсортированный массив по возрастанию, берем первый элемент
            DataRow bottomRow = rows[0];

            // Заполняем поля первого уровня значениями из самого верхнего ряда
            floor.Плоскость = плоскость;
            floor.Этаж = этаж;
            floor.НизСтойки = Convert.ToDouble(bottomRow["НизСтойки"]);
            floor.ВысотаСтойки = Convert.ToDouble(bottomRow["ВысотаСтойки"]);
            floor.Зазор = Convert.ToDouble(bottomRow["Зазор"]);
            floor.ТипРядовСтойки = bottomRow["ТипРядовСтойки"].ToString();
            floor.УровеньЧП = Convert.ToDouble(bottomRow["УровеньЧП"]);
            floor.РядовРигелейИзделия = Convert.ToInt32(bottomRow["РядовРигелейИзделия"]);

            // Заполняем список FloorData
            floor.FloorDataList = new List<FloorData>();

            foreach (DataRow row in rows)
            {
                FloorData floorData = new FloorData();
                floorData.РядФасада = Convert.ToInt32(row["РядФасада"]);
                floorData.РядИзделия = Convert.ToInt32(row["РядИзделия"]);
                floorData.ПоЦентрам = Convert.ToDouble(row["ПоЦентрам"]);
                floorData.Установка = Convert.ToDouble(row["Установка"]);
                floorData.ТипРядовЗаполнений = row["ТипРядовЗаполнений"].ToString();
                floorData.ТипКреплений = row["ТипКреплений"].ToString();
                floorData.Подполки = row["Подполки"].ToString();
                floorData.Опоры = row["Опоры"].ToString();
                floor.FloorDataList.Add(floorData);
            }

            return floor;
        }

        /// <summary>
        /// Извлекает данные о ряде ригеля для заданного этажа и ряда фасада
        /// </summary>
        /// <param name="dataSet">DataSet с загруженными данными из Excel</param>
        /// <param name="плоскость">Идентификатор плоскости (например, "(5-1)-(5-6)")</param>
        /// <param name="этаж">Номер этажа</param>
        /// <param name="рядФасада">Номер ряда фасада</param>
        /// <returns>Объект FloorData с данными о ряде ригеля, или null если не найдено</returns>
        public static FloorData GetFloorData(DataSet dataSet, string плоскость, int этаж, int рядФасада)
        {
            // Получаем таблицу "РядыРигелей" для указанной плоскости
            string tableName = $"РядыРигелей{плоскость}";
            DataTable рядыРигелей = dataSet.Tables.Contains(tableName) ? dataSet.Tables[tableName] : null;

            if (рядыРигелей == null)
            {
                return null;
            }

            // Фильтрация строк по заданному этажу и ряду фасада
            DataRow[] rows = рядыРигелей.Select($"Этаж = { этаж} AND РядФасада = { рядФасада}", "РядИзделия ASC");

            if (rows.Length == 0)
            {
                return null;
            }

            // Берем первую строку (должна быть одна)
            DataRow row = rows[0];

            // Создаем объект FloorData
            FloorData floorData = new FloorData();
            floorData.РядФасада = Convert.ToInt32(row["РядФасада"]);
            floorData.РядИзделия = Convert.ToInt32(row["РядИзделия"]);
            floorData.ПоЦентрам = Convert.ToDouble(row["ПоЦентрам"]);
            floorData.Установка = Convert.ToDouble(row["Установка"]);
            floorData.ТипРядовЗаполнений = row["ТипРядовЗаполнений"].ToString();
            floorData.ТипКреплений = row["ТипКреплений"].ToString();
            floorData.Подполки = row["Подполки"].ToString();
            floorData.Опоры = row["Опоры"].ToString();
            floorData.НизОкна = Convert.ToDouble(row["НизОкна"]);
            floorData.РучкаОтНизаОкна = Convert.ToDouble(row["РучкаОтНизаОкна"]);

            return floorData;
        }
    }
}

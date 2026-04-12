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
        /// <param name="рядыРигелей">Таблица "РядыРигелей"</param>
        /// <param name="этаж">Номер этажа</param>
        /// <returns>Объект CFloor, содержащий данные о рядах ригелей</returns>
        public static CFloor GetFloor(DataTable рядыРигелей, int этаж)
        {
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
            floor.Этаж = этаж;
            floor.НизСтойки = Convert.ToDouble(bottomRow["НизСтойки"]);
            floor.ВысотаСтойки = Convert.ToDouble(bottomRow["ВысотаСтойки"]);
            floor.Зазор = Convert.ToDouble(bottomRow["Зазор"]);
            floor.ТипРядовСтойки = bottomRow["ТипРядовСтойки"].ToString();
            floor.УровеньЧП = Convert.ToDouble(bottomRow["УровеньЧП"]);
            floor.РядовРигелейИзделия = Convert.ToInt32(bottomRow["РядовРигелейИзделия"]);

            // Заполняем список FloorStruct
            floor.FloorStructsList = new List<FloorStruct>();

            foreach (DataRow row in rows)
            {
                FloorStruct floorStruct = new FloorStruct();
                floorStruct.РядФасада = Convert.ToInt32(row["РядФасада"]);
                floorStruct.РядИзделия = Convert.ToInt32(row["РядИзделия"]);
                floorStruct.ПоЦентрам = Convert.ToDouble(row["ПоЦентрам"]);
                floorStruct.Установка = Convert.ToDouble(row["Установка"]);
                floorStruct.ТипРядовЗаполнений = row["ТипРядовЗаполнений"].ToString();
                floorStruct.ТипКреплений = row["ТипКреплений"].ToString();
                floorStruct.Подполки = row["Подполки"].ToString();
                floorStruct.Опоры = row["Опоры"].ToString();
            }

            return floor;
        }
    }
}
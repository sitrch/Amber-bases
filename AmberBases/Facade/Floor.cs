using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmberBases.Facade
{
    public class CFloor
    {
        public string Плоскость;
        public int Этаж;
        public double НизСтойки;
        public double ВысотаСтойки;
        public double Зазор;        
        public string ТипРядовСтойки;
        public double УровеньЧП; // Уровень чистового пола
        public List<FloorStruct> FloorStructsList;
        public int РядовРигелейИзделия;


    }

    public struct FloorStruct
    {
        public int РядФасада;
        public int РядИзделия;
        public double ПоЦентрам;
        public double Установка;
        public string ТипРядовЗаполнений;
        public string ТипКреплений;
        public string Подполки;
        public string Опоры;
        public double НизОкна;
        public double РучкаОтНизаОкна;

    }
}

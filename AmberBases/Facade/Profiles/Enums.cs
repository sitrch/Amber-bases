using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmberBases.Facade
{
   public enum МестоУстановки
    {
        Цех,
        Монтаж
    }

    public static class РазделСпецификации
    {
        public const string Раскрой = "Раскрой";
        public const string Материалы = "Материалы";
        public const string Уплотнители = "Уплотнители";
        public const string Метизы = "Метизы";
        public const string Комплектация = "Комплектация";
        public const string Кронштейны = "Кронштейны";
        public const string Заполнения = "Заполнения";
        public const string ЗаполненияСтворок = "ЗаполненияСтворок";

        public static class Детали
        {
            public const string Стойки = "Стойки";
            public const string Ригели = "Ригели";
            public const string Прижимы = "Прижимы";
            public const string КрышкиСтойки = "КрышкиСтойки";
            public const string КрышкиРигеля = "КрышкиРигеля";
            public const string Проставки = "Проставки";
            public const string ПрижимыГнутые = "ПрижимыГнутые";
            public const string КрышкиСтойкиГнутые = "КрышкиСтойкиГнутые";
            public const string КрышкиРигеляГнутые = "КрышкиРигеляГнутые"; 
        }
        

    }
}


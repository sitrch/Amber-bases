using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmberBases.Facade
{
    public class CBaseProfiles
    {
        public CBaseArticle Article;      // Артикул
        public string Title;        // Название
        public string Description;  // Описание
        public double CustomBarLength;    // Стандартная длина хлыста
        public string ProfileType;          // Тип профиля
    }

    public class CBaseDetail
    {
        public string Article;      // Артикул детали
        public CBaseArticle BaseArticle;      // Артикул исходного профиля

        public string Plane;        // Плоскость
        public int Floor;           // Этаж
        public int Mullion;         // Стойка

        public string РазделСпецификации;
        public string МестоУстановки;
        public double Длина;
        public string Ориентация;
    }

    public class CBaseArticle
    {
        public string Manufacturer; // Производитель
        public string System;       // Система профилей
        public string Code;         // Код заказа
        public string BOMArticle;   // Артикул полный, для заказа
        public string Article;      // Артикул
        public string Title;        // Название
        public string Description;  // Описание
        public string Color;        // Цвет
        public double CutWisibleWidth; // Ширина отображения в раскрое
        public double StandartBarLength;    // Стандартная длина хлыста
        public string ProfileType;          // Тип профиля
    }
}

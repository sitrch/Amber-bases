using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmberBases.Core.Models.Dictionaries;
using DocumentFormat.OpenXml.Presentation;

namespace AmberBases.Facade
{
    public class CBaseDetail
    {
        public string Article;      // Артикул детали
        public ProfileArticle BaseArticle; // Артикул исходного профиля

        public string Плоскость;   // Плоскость
        public int Этаж;           // Этаж
        public int РядСтойки;      // Стойка

        public static string РазделСпецификации;
        public static МестоУстановки МестоУстановки;
        public double Длина;
        public Vector3 Ориентация;
        public Point Установка; // От Начальной точки
        public Point Смещение; // От точки устанвки
    }
}

using AmberBases.Core.Models.Dictionaries;
using AmberBases.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AmberBases.Facade.Profiles
{
    public class Стойка: CProfile
    {
        public ProfileArticle БазовыйАртикул;
        public string Плоскость;
        public int Этаж;
        public int РядСтоек;
        public Стойка(ProfileArticle БазовыйАртикул, string Плоскость, int Этаж, int РядСтоек)
        {
            this.БазовыйАртикул = БазовыйАртикул;
            this.Плоскость = Плоскость;
            this.Этаж = Этаж;
            this.РядСтоек = РядСтоек;


        }
    }
}

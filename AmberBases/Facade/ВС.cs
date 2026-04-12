using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmberBases.Facade
{
    public class БлокВС
    {
        public double РядВС;
        public double РядПодВС;
        public double РядНадВС;
        public double Ширина;
        public БлокВС(double РядВС, double РядПодВС, double РядНадВС, double Ширина)
        {
            this.РядВС = РядВС;
            this.РядНадВС = РядНадВС;
            this.РядПодВС = РядПодВС;
            this.Ширина = Ширина;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.WzLib
{
    public class Wz_Convex
    {
        public Wz_Convex(Wz_Vector[] points)
        {
            this.Points = points;
        }

        public Wz_Vector[] Points { get; set; }
    }
}

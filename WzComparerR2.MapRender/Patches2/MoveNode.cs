using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.MapRender.Patches2
{
    public class MoveNode
    {
        public MoveNode()
        {
        }

        public int MoveW { get; set; }
        public int MoveH { get; set; }
        public int MoveP { get; set; }
        public int MoveDelay { get; set; }

        public Vector2 StartPos { get; set; }
        public int StartTime { get; set; }
        public int TotalPeriod => MoveP + MoveDelay;

        public bool IsFirstNode => StartTime == 0;
        public bool IsLastNode { get; set; }
    }
}

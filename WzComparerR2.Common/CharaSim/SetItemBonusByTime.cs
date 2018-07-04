using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public class SetItemBonusByTime
    {
        public SetItemBonusByTime()
        {
            this.Props = new Dictionary<GearPropType, int>();
        }

        public int TermStart { get; set; }
        public Dictionary<GearPropType, int> Props { get; private set; }
    }
}

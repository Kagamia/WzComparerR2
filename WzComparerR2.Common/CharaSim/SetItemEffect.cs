using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public class SetItemEffect
    {
        public SetItemEffect()
        {
            props = new SortedDictionary<GearPropType, object>();
            enabled = false;
        }
        private SortedDictionary<GearPropType, object> props;
        private bool enabled;

        public SortedDictionary<GearPropType, object> Props
        {
            get { return props; }
        }

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }
    }
}

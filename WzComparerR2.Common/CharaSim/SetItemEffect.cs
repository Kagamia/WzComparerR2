using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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

        public IEnumerable<KeyValuePair<GearPropType, object>> PropsV5
        {
            get { return props.Where(kv => Gear.IsV5SupportPropType(kv.Key)); }
        }

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }
    }
}

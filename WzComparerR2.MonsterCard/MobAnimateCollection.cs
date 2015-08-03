using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace WzComparerR2.MonsterCard
{
    public class MobAnimateCollection : KeyedCollection<string, MobAnimate>
    {
        protected override string GetKeyForItem(MobAnimate item)
        {
            return item.Name;
        }
    }
}

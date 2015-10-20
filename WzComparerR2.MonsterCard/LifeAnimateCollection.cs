using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace WzComparerR2.MonsterCard
{
    public class LifeAnimateCollection : KeyedCollection<string, LifeAnimate>
    {
        protected override string GetKeyForItem(LifeAnimate item)
        {
            return item.Name;
        }
    }
}

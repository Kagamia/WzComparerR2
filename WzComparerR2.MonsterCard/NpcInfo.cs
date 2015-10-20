using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.MonsterCard
{
    public class NpcInfo
    {
        public NpcInfo()
        {
            this.Animates = new LifeAnimateCollection();
        }

        public int? ID { get; set; }
        public bool Shop { get; set; }

        public int? Link { get; set; }

        public BitmapOrigin Default { get; set; }

        public LifeAnimateCollection Animates { get; private set; }
    }
}

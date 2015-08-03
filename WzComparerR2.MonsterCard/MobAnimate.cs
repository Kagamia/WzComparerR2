using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.Common;

namespace WzComparerR2.MonsterCard
{
    public class MobAnimate
    {
        public MobAnimate(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }
        public Gif AnimateGif { get; set; }
    }
}

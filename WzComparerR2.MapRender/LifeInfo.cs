using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.MapRender
{
    public class LifeInfo
    {
        public LifeInfo()
        {
            this.speed = -100;
        }

        public int level;
        public int maxHP;
        public int maxMP;
        public int speed;
        public int PADamage;
        public int PDDamage;
        public int PDRate;
        public int MADamage;
        public int MDDamage;
        public int MDRate;
        public int acc;
        public int eva;
        public int pushed;
        public int exp;
        public ElemAttr elemAttr;
        public bool undead;
        public bool boss;

        public struct ElemAttr
        {
            public ElemResistance I;
            public ElemResistance L;
            public ElemResistance F;
            public ElemResistance S;
            public ElemResistance H;
            public ElemResistance D;
            public ElemResistance P;
        }

        public enum ElemResistance : byte
        {
            Normal = 0,
            Immune = 1,
            Resist = 2,
            Weak = 3,
        }
    }
}

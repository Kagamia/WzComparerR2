using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.MonsterCard
{
    public class MobInfo
    {
        public MobInfo()
        {
            this.ElemAttr = new MobElemAttr(null);
            this.Revive = new List<int>();
            this.Animates = new LifeAnimateCollection();

            this.FirstAttack = false;
            this.BodyAttack = true;
            this.DamagedByMob = false;
        }

        public int? ID { get; set; }
        public int Level { get; set; }
        public string DefaultHP { get; set; }
        public string DefaultMP { get; set; }
        public string FinalMaxHP { get; set; }
        public string FinalMaxMP { get; set; }
        public int MaxHP { get; set; }
        public int MaxMP { get; set; }
        public int HPRecovery { get; set; }
        public int MPRecovery { get; set; }
        public int? Speed { get; set; }
        public int? FlySpeed { get; set; }
        public int PADamage { get; set; }
        public int MADamage { get; set; }
        public int PDRate { get; set; }
        public int MDRate { get; set; }
        public int Acc { get; set; }
        public int Eva { get; set; }
        public int Pushed { get; set; }
        public int Exp { get; set; }
        public bool Boss { get; set; }
        public bool Undead { get; set; }
        public int Category { get; set; }
        public bool FirstAttack { get; set; }
        public bool BodyAttack { get; set; }
        public int RemoveAfter { get; set; }
        public bool DamagedByMob { get; set; }
        public bool Invincible { get; set; }
        public bool NotAttack { get; set; }
        public int FixedDamage { get; set; }
        public MobElemAttr ElemAttr { get; set; }

        public int? Link { get; set; }
        public bool Skeleton { get; set; }

        public List<int> Revive { get; private set; }
        public LifeAnimateCollection Animates { get; private set; }
    }
}

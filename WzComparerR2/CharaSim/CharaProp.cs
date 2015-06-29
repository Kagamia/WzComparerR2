using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public class CharaProp
    {
        public CharaProp()
        {
        }

        public CharaProp(int totalMax)
        {
            this.totalMax = totalMax;
        }

        private int baseVal; //基础值
        private int gearAdd; //装备附加值
        private int buffAdd; //技能buff增加值
        private int eBuffAdd; //技能增加的enhance值
        private int rate; //装备潜能百分比
        private int aBuffRate; //主动buff百分比 如骰子
        private int pBuffRate; //被动buff百分比 如盾防精通
        private int totalMax;
        private bool smart; //当前的技能buff增加值是否为smart
        
        public int BaseVal
        {
            get { return baseVal; }
            set { baseVal = value; }
        }
        
        public int GearAdd
        {
            get { return gearAdd; }
            set { gearAdd = value; }
        }
        
        public int BuffAdd
        {
            get { return buffAdd; }
            set { buffAdd = value; }
        }
        
        public int EBuffAdd
        {
            get { return eBuffAdd; }
            set { eBuffAdd = value; }
        }
        
        public int Rate
        {
            get { return rate; }
            set { rate = value; }
        }

        public int ABuffRate
        {
            get { return aBuffRate; }
            set { aBuffRate = value; }
        }
        
        public int PBuffRate
        {
            get { return pBuffRate; }
            set { pBuffRate = value; }
        }
        
        public bool Smart
        {
            get { return smart; }
            set { smart = value; }
        }

        public int TotalMax
        {
            get { return totalMax; }
            set { totalMax = value; }
        }

        public int GetSum()
        {
            int origSum = (baseVal + gearAdd + buffAdd + eBuffAdd) * (100 + rate + aBuffRate + pBuffRate) / 100;
            return this.totalMax > 0 ? Math.Min(this.totalMax, origSum) : origSum;
        }

        public int GetGearReqSum()
        {
            int origSum = (baseVal + gearAdd + buffAdd) * (100 + rate + aBuffRate + pBuffRate) / 100;
            return this.totalMax > 0 ? Math.Min(this.totalMax, origSum) : origSum;
        }

        public void ResetAdd()
        {
            gearAdd = 0;
            eBuffAdd = 0;
            buffAdd = 0;
            rate = 0;
            aBuffRate = 0;
            pBuffRate = 0;
            smart = false;
        }

        public void ResetAll()
        {
            baseVal = 0;
            ResetAdd();
        }

        public override string ToString()
        {
            int sum = GetSum();
            return baseVal == sum ? baseVal.ToString() :
                string.Format("{0} ({1}+{2})", sum, baseVal, sum - baseVal);
        }

        public string ToStringDetail(out int red)
        {
            int sum = GetSum();
            int baseSum = (baseVal + gearAdd) +
                (baseVal + gearAdd + buffAdd + eBuffAdd) * (rate + aBuffRate) / 100;
            if (buffAdd == 0 && eBuffAdd == 0 && pBuffRate == 0 && baseSum <= sum)
            {
                red = Math.Sign(aBuffRate);
                return baseSum.ToString();
            }

            red = Math.Sign(sum - baseSum);
            return (sum == baseSum) ? sum.ToString() :
                string.Format("{0} ({1}{2}{3})", sum, baseSum, (sum - baseSum >= 0) ? "+" : "-", sum - baseSum);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender
{
    public class LifeInfo
    {
        public LifeInfo()
        {
            this.speed = -100;
        }

        public int level;
        public long maxHP;
        public long maxMP;
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

        public static LifeInfo CreateFromNode(Wz_Node mobNode)
        {
            if (mobNode == null)
            {
                return null;
            }

            var lifeInfo = new LifeInfo();
            var infoNode = mobNode.Nodes["info"];

            if (infoNode != null)
            {
                foreach (Wz_Node node in infoNode.Nodes)
                {
                    switch (node.Text)
                    {
                        case "level": lifeInfo.level = node.GetValueEx<int>(0); break;
                        case "maxHP": lifeInfo.maxHP = node.GetValueEx<long>(0); break;
                        case "maxMP": lifeInfo.maxMP = node.GetValueEx<long>(0); break;
                        case "speed": lifeInfo.speed = node.GetValueEx<int>(0); break;
                        case "PADamage": lifeInfo.PADamage = node.GetValueEx<int>(0); break;
                        case "PDDamage": lifeInfo.PDDamage = node.GetValueEx<int>(0); break;
                        case "PDRate": lifeInfo.PDRate = node.GetValueEx<int>(0); break;
                        case "MADamage": lifeInfo.MADamage = node.GetValueEx<int>(0); break;
                        case "MDDamage": lifeInfo.MDDamage = node.GetValueEx<int>(0); break;
                        case "MDRate": lifeInfo.MDRate = node.GetValueEx<int>(0); break;
                        case "acc": lifeInfo.acc = node.GetValueEx<int>(0); break;
                        case "eva": lifeInfo.eva = node.GetValueEx<int>(0); break;
                        case "pushed": lifeInfo.pushed = node.GetValueEx<int>(0); break;
                        case "exp": lifeInfo.exp = node.GetValueEx<int>(0); break;
                        case "undead": lifeInfo.undead = node.GetValueEx<int>(0) != 0; break;
                        case "boss": lifeInfo.boss = node.GetValueEx<int>(0) != 0; break;
                        case "elemAttr":
                            string elem = node.GetValueEx<string>(string.Empty);
                            for (int i = 0; i < elem.Length; i += 2)
                            {
                                LifeInfo.ElemResistance resist = (LifeInfo.ElemResistance)(elem[i + 1] - 48);
                                switch (elem[i])
                                {
                                    case 'I': lifeInfo.elemAttr.I = resist; break;
                                    case 'L': lifeInfo.elemAttr.L = resist; break;
                                    case 'F': lifeInfo.elemAttr.F = resist; break;
                                    case 'S': lifeInfo.elemAttr.S = resist; break;
                                    case 'H': lifeInfo.elemAttr.H = resist; break;
                                    case 'D': lifeInfo.elemAttr.D = resist; break;
                                    case 'P': lifeInfo.elemAttr.P = resist; break;
                                }
                            }
                            break;
                    }
                }
            }
            return lifeInfo;
        }
    }
}

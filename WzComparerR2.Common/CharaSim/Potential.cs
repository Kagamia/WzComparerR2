using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Potential
    {
        public Potential()
        {
            props = new Dictionary<GearPropType, int>();
        }
        public int code;
        public int optionType;
        public int reqLevel;
        public Dictionary<GearPropType, int> props;
        public int weight;
        public string stringSummary;

        /// <summary>
        /// 指示潜能是否是附加潜能。
        /// </summary>
        public bool IsPotentialEx
        {
            get { return this.code / 1000 % 10 == 2; }
        }

        public override string ToString()
        {
            return this.code.ToString("d6") + " " + ConvertSummary()
                + (weight > 0 ? (" - " + weight) : null);
        }

        public string ConvertSummary()
        {
            if (string.IsNullOrEmpty(this.stringSummary))
                return null;
            List<string> types = new List<string>(this.props.Keys.Count);
            foreach (GearPropType k in this.props.Keys)
                types.Add(k.ToString());
            types.Sort((a, b) => b.Length.CompareTo(a.Length));
            string str = this.stringSummary;
            foreach (string s in types)
            {
                GearPropType t = (GearPropType)Enum.Parse(typeof(GearPropType), s);
                str = str.Replace("#" + s, this.props[t].ToString());
            }
            return str;
        }

        public static int GetPotentialLevel(int gearReqLevel)
        {
            if (gearReqLevel <= 0) return 1;
            else if (gearReqLevel >= 200) return 20;
            else return (gearReqLevel + 9) / 10;
        }

        public static bool CheckOptionType(int optionType, GearType gearType)
        {
            switch (optionType)
            {
                case 0: return true;
                case 10: return Gear.IsWeapon(gearType) || 
                    (Gear.IsSubWeapon(gearType) && gearType != GearType.shield);
                case 11:
                    return !CheckOptionType(10, gearType);
                case 20: return gearType == GearType.pants
                    || gearType == GearType.shoes
                    || gearType == GearType.cap
                    || gearType == GearType.coat
                    || gearType == GearType.longcoat
                    || gearType == GearType.glove
                    || gearType == GearType.cape;
                case 40: return gearType == GearType.ring
                    || gearType == GearType.earrings
                    || gearType == GearType.pendant
                    || gearType == GearType.belt;
                case 51: return gearType == GearType.cap;
                case 52: return gearType == GearType.coat || gearType == GearType.longcoat;
                case 53: return gearType == GearType.pants || gearType == GearType.longcoat;
                case 54: return gearType == GearType.glove;
                case 55: return gearType == GearType.shoes;
                default: return false;
            }
        }

        public static Potential CreateFromNode(Wz_Node potentialNode, int pLevel)
        {
            Potential potential = new Potential();
            if (potentialNode == null || !Int32.TryParse(potentialNode.Text, out potential.code))
                return null;
            foreach (Wz_Node subNode in potentialNode.Nodes)
            {
                if (subNode.Text == "info")
                {
                    foreach (Wz_Node infoNode in subNode.Nodes)
                    {
                        switch (infoNode.Text)
                        {
                            case "optionType":
                                potential.optionType = Convert.ToInt32(infoNode.Value);
                                break;
                            case "reqLevel":
                                potential.reqLevel = Convert.ToInt32(infoNode.Value);
                                break;
                            case "weight":
                                potential.weight = Convert.ToInt32(infoNode.Value);
                                break;
                            case "string":
                                potential.stringSummary = Convert.ToString(infoNode.Value);
                                break;
                        }
                    }
                }
                else if (subNode.Text == "level")
                {
                    Wz_Node levelNode = subNode.FindNodeByPath(pLevel.ToString());
                    if (levelNode != null)
                    {
                        foreach (Wz_Node propNode in levelNode.Nodes)
                        {
                            try
                            {
                                GearPropType propType = (GearPropType)Enum.Parse(typeof(GearPropType), propNode.Text);
                                int value = (propType == GearPropType.face ? 0 : Convert.ToInt32(propNode.Value));
                                potential.props.Add(propType, value);
                            }
                            catch
                            {
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return potential;
        }

        public static Potential LoadFromWz(int optID, int optLevel, GlobalFindNodeFunction findNode)
        {
            Wz_Node itemWz = findNode("Item\\ItemOption.img");
            if (itemWz == null)
                return null;

            Potential opt = Potential.CreateFromNode(itemWz.FindNodeByPath(optID.ToString("d6")), optLevel);
            return opt;
        }
    }
}

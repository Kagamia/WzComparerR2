using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class GearLevelInfo
    {
        public GearLevelInfo()
        {
            this.BonusProps = new Dictionary<GearPropType, Range>();
            this.Skills = new Dictionary<int, int>();
            this.EquipmentSkills = new Dictionary<int, int>();
        }

        public int Level { get; set; }
        public Dictionary<GearPropType, Range> BonusProps { get; private set; }
        public int Exp { get; set; }

        public string HS { get; set; }
        public int Prob { get; set; }
        public int ProbTotal { get; set; }
        public Dictionary<int, int> Skills { get; private set; }
        public Dictionary<int, int> EquipmentSkills { get; private set; }

        public static GearLevelInfo CreateFromNode(Wz_Node node)
        {
            GearLevelInfo info = new GearLevelInfo();

            foreach (Wz_Node child in node.Nodes)
            {
                if (child.Text == "exp")
                {
                    info.Exp = child.GetValue(0);
                }
                else 
                {
                    string prefix;
                    if (child.Text.EndsWith("Min") || child.Text.EndsWith("Max"))
                    {
                        prefix = child.Text.Substring(0, child.Text.Length - 3);
                    }
                    else
                    {
                        prefix = child.Text;
                    }
                    
                    Range range;
                    try
                    {
                        GearPropType propType = (GearPropType)Enum.Parse(typeof(GearPropType), prefix, true);
                        info.BonusProps.TryGetValue(propType, out range);
                        if (child.Text.EndsWith("Min"))
                        {
                            range.Min = child.GetValue(0);
                            info.BonusProps[propType] = range;
                        }
                        else if (child.Text.EndsWith("Max"))
                        {
                            range.Max = child.GetValue(0);
                            info.BonusProps[propType] = range;
                        }
                        else
                        {
                            range.Min = range.Max = child.GetValue(0);
                            info.BonusProps[propType] = range;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            return info;
        }

        public struct Range
        {
            public Range(int min, int max)
            {
                this.min = min;
                this.max = max;
            }

            private int min;
            private int max;
            public int Min
            {
                get { return min; }
                set { min = value; }
            }
            public int Max
            {
                get { return max; }
                set { max = value; }
            }
        }
    }
}

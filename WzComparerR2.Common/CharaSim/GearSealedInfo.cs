using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class GearSealedInfo
    {
        public GearSealedInfo()
        {
            this.BonusProps = new Dictionary<GearPropType, int>();
        }

        public int Level { get; set; }
        public Dictionary<GearPropType, int> BonusProps { get; private set; }
        public int Exp { get; set; }
        public bool HasIcon { get; set; }
        public BitmapOrigin Icon { get; set; }
        public BitmapOrigin IconRaw { get; set; }

        public static GearSealedInfo CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            GearSealedInfo info = new GearSealedInfo();

            foreach (Wz_Node child in node.Nodes)
            {
                switch (child.Text)
                {
                    case "exp":
                        info.Exp = child.GetValue(0);
                        break;

                    case "icon":
                        info.Icon = BitmapOrigin.CreateFromNode(child, findNode);
                        info.HasIcon = true;
                        break;

                    case "iconRaw":
                        info.IconRaw = BitmapOrigin.CreateFromNode(child, findNode);
                        info.HasIcon = true;
                        break;

                    default:
                        try
                        {
                            GearPropType propType = (GearPropType)Enum.Parse(typeof(GearPropType), child.Text, true);
                            info.BonusProps[propType] = child.GetValue(0);
                        }
                        finally
                        {
                        }
                        break;
                }
            }
            return info;
        }
    }
}

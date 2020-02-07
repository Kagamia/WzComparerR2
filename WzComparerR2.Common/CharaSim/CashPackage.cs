using System;
using System.Collections.Generic;
using System.Text;

using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class CashPackage : ItemBase
    {
        public CashPackage()
        {
            SN = new List<int>();
        }
        public string name;
        public string desc;
        public int onlyCash;
        public List<int> SN;

        public static CashPackage CreateFromNode(Wz_Node itemNode, Wz_Node cashPackageNode, GlobalFindNodeFunction findNode)
        {
            CashPackage cashPackage = new CashPackage();
            int value;
            if (itemNode == null
                || !Int32.TryParse(itemNode.Text, out value)
                && !((value = itemNode.Text.IndexOf(".img")) > -1 && Int32.TryParse(itemNode.Text.Substring(0, value), out value)))
            {
                return null;
            }
            cashPackage.ItemID = value;

            Wz_Node pngNode;
            foreach (Wz_Node subNode in itemNode.Nodes)
            {
                switch (subNode.Text)
                {
                    case "icon":
                        pngNode = subNode;
                        if (pngNode.Value is Wz_Uol)
                        {
                            Wz_Uol uol = pngNode.Value as Wz_Uol;
                            Wz_Node uolNode = uol.HandleUol(subNode);
                            if (uolNode != null)
                            {
                                pngNode = uolNode;
                            }
                        }
                        if (pngNode.Value is Wz_Png)
                        {
                            cashPackage.Icon = BitmapOrigin.CreateFromNode(pngNode, findNode);
                        }

                        break;
                    case "name":
                        cashPackage.name = Convert.ToString(subNode.Value);
                        break;
                    case "desc":
                        cashPackage.desc = Convert.ToString(subNode.Value);
                        break;
                    case "onlyCash":
                        cashPackage.onlyCash = Convert.ToInt32(subNode.Value);
                        break;
                }
            }

            if (cashPackageNode != null)
            {
                Wz_Node snNode = cashPackageNode.FindNodeByPath("SN");
                if (snNode != null)
                {
                    foreach (Wz_Node subNode in snNode.Nodes)
                    {
                        int SN = Convert.ToInt32(subNode.Value);
                        cashPackage.SN.Add(SN);
                    }
                }
            }

            return cashPackage;
        }
    }
}

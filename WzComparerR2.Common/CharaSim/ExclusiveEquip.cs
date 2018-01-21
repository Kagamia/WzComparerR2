using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class ExclusiveEquip
    {
        public ExclusiveEquip()
        {
            this.Items = new List<int>();
        }
        public string Info { get; set; }
        public List<int> Items { get; private set; }
        public string Msg { get; set; }

        public static ExclusiveEquip CreateFromNode(Wz_Node exclusiveEquipNode)
        {
            if (exclusiveEquipNode == null)
                return null;

            ExclusiveEquip exclusiveEquip = new ExclusiveEquip();

            foreach (Wz_Node subNode in exclusiveEquipNode.Nodes)
            {
                switch (subNode.Text)
                {
                    case "info":
                        exclusiveEquip.Info = Convert.ToString(subNode.Value);
                        break;
                    case "item":
                        foreach (Wz_Node itemNode in subNode.Nodes)
                        {
                            int itemID = Convert.ToInt32(itemNode.Value);
                            exclusiveEquip.Items.Add(itemID);
                        }
                        break;
                    case "msg":
                        exclusiveEquip.Msg = Convert.ToString(subNode.Value);
                        break;
                }
            }

            return exclusiveEquip;
        }
    }
}

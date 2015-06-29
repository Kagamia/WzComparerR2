using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;

namespace WzComparerR2.CharaSim
{
    public static class CharaSimLoader
    {
        static CharaSimLoader()
        {
            loadedSetItems = new Dictionary<int, SetItem>();
        }

        private static Dictionary<int, SetItem> loadedSetItems;

        public static Dictionary<int, SetItem> LoadedSetItems
        {
            get { return loadedSetItems; }
        } 

        public static void LoadSetItems()
        {
            //搜索setItemInfo.img
            Wz_Node etcWz = PluginManager.FindWz(Wz_Type.Etc);
            if (etcWz == null)
                return;
            Wz_Node setItemNode = etcWz.FindNodeByPath("SetItemInfo.img", true);
            if (setItemNode == null)
                return;

            //搜索ItemOption.img
            Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item);
            if (itemWz == null)
                return;
            Wz_Node optionNode = itemWz.FindNodeByPath("ItemOption.img", true);
            if (optionNode == null)
                return;

            loadedSetItems.Clear();
            foreach (Wz_Node node in setItemNode.Nodes)
            {
                int setItemIndex;
                if (Int32.TryParse(node.Text, out setItemIndex))
                {
                    SetItem setItem = SetItem.CreateFromNode(node, optionNode);
                    if (setItem != null)
                        loadedSetItems[setItemIndex] = setItem;
                }
            }
        }

        public static int GetActionDelay(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                return 0;
            }
            Wz_Node actionNode = PluginManager.FindWz("Character/00002000.img/" + actionName);
            if (actionNode == null)
            {
                return 0;
            }

            int delay = 0;
            foreach (Wz_Node frameNode in actionNode.Nodes)
            {
                Wz_Node delayNode = frameNode.Nodes["delay"];
                if (delayNode != null)
                {
                    delay += Math.Abs(delayNode.GetValue<int>());
                }
            }

            return delay;
        }
    }
}

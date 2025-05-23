﻿using System;
using System.Collections.Generic;
using System.Text;

using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Item : ItemBase
    {
        public Item()
        {
            this.Props = new Dictionary<ItemPropType, long>();
            this.Specs = new Dictionary<ItemSpecType, long>();
            this.Recipes = new List<int>();
        }

        public int Level { get; set; }

        public Dictionary<ItemPropType, long> Props { get; private set; }
        public Dictionary<ItemSpecType, long> Specs { get; private set; }
        public List<int> Recipes { get; private set; }

        public bool Cash
        {
            get { return GetBooleanValue(ItemPropType.cash); }
        }

        public bool TimeLimited
        {
            get { return GetBooleanValue(ItemPropType.timeLimited); }
        }

        public bool GetBooleanValue(ItemPropType type)
        {
            return this.Props.TryGetValue(type, out long value) && value != 0;
        }

        public static Item CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            Item item = new Item();
            int value;
            if (node == null
                || !Int32.TryParse(node.Text, out value)
                && !((value = node.Text.IndexOf(".img")) > -1 && Int32.TryParse(node.Text.Substring(0, value), out value)))
            {
                return null;
            }
            item.ItemID = value;

            // in msn the node could be UOL.
            if (node.Value is Wz_Uol)
            {
                if ((node = node.ResolveUol()) == null)
                {
                    return item;
                }
            }

            Wz_Node infoNode = node.FindNodeByPath("info");
            if (infoNode != null)
            {
                foreach (Wz_Node subNode in infoNode.Nodes)
                {
                    switch (subNode.Text)
                    {
                        case "icon":
                            if (subNode.Value is Wz_Uol || subNode.Value is Wz_Png)
                            {
                                item.Icon = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "iconRaw":
                            if (subNode.Value is Wz_Uol || subNode.Value is Wz_Png)
                            {
                                item.IconRaw = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "sample":
                            if (subNode.Value is Wz_Uol || subNode.Value is Wz_Png)
                            {
                                item.Sample = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "lv":
                            item.Level = Convert.ToInt32(subNode.Value);
                            break;

                        default:
                            if (!int.TryParse(subNode.Text, out _) && Enum.TryParse(subNode.Text, out ItemPropType type))
                            {
                                try
                                {
                                    item.Props.Add(type, Convert.ToInt64(subNode.Value));
                                }
                                finally
                                {
                                }
                            }
                            break;
                    }
                }
            }

            Wz_Node specNode = node.FindNodeByPath("spec");
            if (specNode != null)
            {
                foreach (Wz_Node subNode in specNode.Nodes)
                {
                    if (subNode.Text == "recipe")
                    {
                        if (subNode.Value == null && subNode.Nodes.Count > 0)
                        {
                            foreach (var recipeNode in subNode.Nodes)
                            {
                                item.Recipes.Add(recipeNode.GetValue<int>());
                            }
                        }
                        else
                        {
                            item.Recipes.Add(subNode.GetValue<int>());
                        }
                    }
                    else if(Enum.TryParse(subNode.Text, out ItemSpecType type))
                    {
                        try
                        {
                            item.Specs.Add(type, Convert.ToInt64(subNode.Value));
                        }
                        finally
                        {
                        }
                    }
                }
            }
            return item;
        }

    }
}

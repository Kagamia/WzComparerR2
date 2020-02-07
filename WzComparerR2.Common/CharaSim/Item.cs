using System;
using System.Collections.Generic;
using System.Text;

using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Item : ItemBase
    {
        public Item()
        {
            this.Props = new Dictionary<ItemPropType, int>();
            this.Specs = new Dictionary<ItemSpecType, int>();
        }

        public int Level { get; set; }
        public string EndUseDate { get; set; }

        public List<GearLevelInfo> Levels { get; internal set; }

        public Dictionary<ItemPropType, int> Props { get; private set; }
        public Dictionary<ItemSpecType, int> Specs { get; private set; }

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
            int value;
            return this.Props.TryGetValue(type, out value) && value != 0;
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

            Wz_Node infoNode = node.FindNodeByPath("info");
            if (infoNode != null)
            {
                Wz_Node pngNode;
                foreach (Wz_Node subNode in infoNode.Nodes)
                {
                    switch (subNode.Text)
                    {
                        case "icon":
                            pngNode = subNode;
                            while (pngNode.Value is Wz_Uol)
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
                                item.Icon = BitmapOrigin.CreateFromNode(pngNode, findNode);
                            }
                            break;

                        case "iconRaw":
                            pngNode = subNode;
                            while (pngNode.Value is Wz_Uol)
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
                                item.IconRaw = BitmapOrigin.CreateFromNode(pngNode, findNode);
                            }
                            break;

                        case "sample":
                            if (subNode.Value is Wz_Png)
                            {
                                item.Sample = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "lv":
                            item.Level = Convert.ToInt32(subNode.Value);
                            break;

                        case "endUseDate":
                            item.EndUseDate = Convert.ToString(subNode.Value);
                            break;

                        case "exp":
                            foreach (Wz_Node subNode2 in subNode.Nodes)
                            {
                                ItemPropType type2;
                                if (Enum.TryParse("exp_" + subNode2.Text, out type2))
                                {
                                    try
                                    {
                                        item.Props.Add(type2, Convert.ToInt32(subNode2.Value));
                                    }
                                    finally
                                    {
                                    }
                                }
                            }
                            break;

                        case "level": //可升级信息
                            Wz_Node levelInfo = subNode.Nodes["info"];
                            item.Levels = new List<GearLevelInfo>();
                            if (levelInfo != null)
                            {
                                for (int i = 1; ; i++)
                                {
                                    Wz_Node levelInfoNode = levelInfo.Nodes[i.ToString()];
                                    if (levelInfoNode != null)
                                    {
                                        GearLevelInfo info = GearLevelInfo.CreateFromNode(levelInfoNode);
                                        int lv;
                                        Int32.TryParse(levelInfoNode.Text, out lv);
                                        info.Level = lv;
                                        item.Levels.Add(info);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }

                            Wz_Node levelCase = subNode.Nodes["case"];
                            if (levelCase != null)
                            {
                                int probTotal = 0;
                                foreach (Wz_Node caseNode in levelCase.Nodes)
                                {
                                    int prob = caseNode.Nodes["prob"].GetValueEx(0);
                                    probTotal += prob;
                                    for (int i = 0; i < item.Levels.Count; i++)
                                    {
                                        GearLevelInfo info = item.Levels[i];
                                        Wz_Node caseLevel = caseNode.Nodes[info.Level.ToString()];
                                        if (caseLevel != null)
                                        {
                                            //desc
                                            Wz_Node caseHS = caseLevel.Nodes["hs"];
                                            if (caseHS != null)
                                            {
                                                info.HS = caseHS.GetValue<string>();
                                            }

                                            //随机技能
                                            Wz_Node caseSkill = caseLevel.Nodes["Skill"];
                                            if (caseSkill != null)
                                            {
                                                foreach (Wz_Node skillNode in caseSkill.Nodes)
                                                {
                                                    int id = skillNode.Nodes["id"].GetValueEx(-1);
                                                    int level = skillNode.Nodes["level"].GetValueEx(-1);
                                                    if (id >= 0 && level >= 0)
                                                    {
                                                        info.Skills[id] = level;
                                                    }
                                                }
                                            }

                                            //装备技能
                                            Wz_Node equipSkill = caseLevel.Nodes["EquipmentSkill"];
                                            if (equipSkill != null)
                                            {
                                                foreach (Wz_Node skillNode in equipSkill.Nodes)
                                                {
                                                    int id = skillNode.Nodes["id"].GetValueEx(-1);
                                                    int level = skillNode.Nodes["level"].GetValueEx(-1);
                                                    if (id >= 0 && level >= 0)
                                                    {
                                                        info.EquipmentSkills[id] = level;
                                                    }
                                                }
                                            }
                                            info.Prob = prob;
                                        }
                                    }
                                }

                                foreach (var info in item.Levels)
                                {
                                    info.ProbTotal = probTotal;
                                }
                            }
                            item.Props.Add(ItemPropType.level, 1);
                            break;

                        default:
                            ItemPropType type;
                            if (Enum.TryParse(subNode.Text, out type))
                            {
                                try
                                {
                                    item.Props.Add(type, Convert.ToInt32(subNode.Value));
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
                    ItemSpecType type;
                    if (Enum.TryParse(subNode.Text, out type))
                    {
                        try
                        {
                            item.Specs.Add(type, Convert.ToInt32(subNode.Value));
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

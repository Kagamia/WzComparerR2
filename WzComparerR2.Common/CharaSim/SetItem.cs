using System;
using System.Collections.Generic;
using System.Text;

using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class SetItem
    {
        public SetItem()
        {
            ItemIDs = new SetItemIDList();
            Effects = new Dictionary<int, SetItemEffect>();
        }

        public int SetItemID { get; set; }
        public int CompleteCount { get; set; }
        public int currentCount;
        public bool Parts { get; set; }
        public bool ExpandToolTip { get; set; }
        public SetItemIDList ItemIDs { get; private set; }
        public string SetItemName { get; set; }
        public Dictionary<int, SetItemEffect> Effects { get; private set; }

        public static SetItem CreateFromNode(Wz_Node setItemNode, Wz_Node optionNode)
        {
            if (setItemNode == null)
                return null;

            SetItem setItem = new SetItem();
            int setItemID;
            if (int.TryParse(setItemNode.Text, out setItemID))
            {
                setItem.SetItemID = setItemID;
            }

            Dictionary<string, string> desc = new Dictionary<string, string>();

            foreach (Wz_Node subNode in setItemNode.Nodes)
            {
                switch (subNode.Text)
                {
                    case "setItemName":
                        setItem.SetItemName = Convert.ToString(subNode.Value);
                        break;
                    case "completeCount":
                        setItem.CompleteCount = Convert.ToInt32(subNode.Value);
                        break;
                    case "parts":
                        setItem.Parts = subNode.GetValue<int>() != 0;
                        break;
                    case "expandToolTip":
                        setItem.ExpandToolTip = subNode.GetValue<int>() != 0;
                        break;
                    case "ItemID":
                        foreach (Wz_Node itemNode in subNode.Nodes)
                        {
                            int idx = Convert.ToInt32(itemNode.Text);
                            if (itemNode.Nodes.Count == 0)
                            {
                                int itemID = Convert.ToInt32(itemNode.Value);
                                setItem.ItemIDs.Add(idx, new SetItemIDPart(itemID));
                            }
                            else
                            {
                                SetItemIDPart part = new SetItemIDPart();
                                int num;
                                foreach (Wz_Node itemNode2 in itemNode.Nodes)
                                {
                                    switch (itemNode2.Text)
                                    {
                                        case "representName":
                                            part.RepresentName = Convert.ToString(itemNode2.Value);
                                            break;
                                        case "typeName":
                                            part.TypeName = Convert.ToString(itemNode2.Value);
                                            break;
                                        case "byGender":
                                            part.ByGender = Convert.ToInt32(itemNode2.Value) != 0;
                                            break;
                                        default:
                                            if (Int32.TryParse(itemNode2.Text, out num) && num > 0)
                                            {
                                                part.ItemIDs[Convert.ToInt32(itemNode2.Value)] = false;
                                            }
                                            break;
                                    }
                                }
                                setItem.ItemIDs.Add(idx, part);
                            }
                        }
                        break;
                    case "Effect":
                        foreach (Wz_Node effectNode in subNode.Nodes)
                        {
                            int count = Convert.ToInt32(effectNode.Text);
                            SetItemEffect effect = new SetItemEffect();
                            foreach (Wz_Node propNode in effectNode.Nodes)
                            {
                                switch (propNode.Text)
                                {
                                    case "Option":
                                        if (optionNode != null)
                                        {
                                            List<Potential> potens = new List<Potential>();
                                            foreach (Wz_Node pNode in propNode.Nodes)
                                            {
                                                string optText = Convert.ToString(pNode.FindNodeByPath("option").Value).PadLeft(6, '0');
                                                Wz_Node opn = optionNode.FindNodeByPath(optText);
                                                if (opn == null)
                                                    continue;
                                                Potential p = Potential.CreateFromNode(opn, Convert.ToInt32(pNode.FindNodeByPath("level").Value));
                                                if (p != null)
                                                {
                                                    potens.Add(p);
                                                }
                                            }
                                            effect.Props.Add(GearPropType.Option, potens);
                                        }
                                        break;

                                    case "OptionToMob":
                                        List<SetItemOptionToMob> opToMobList = new List<SetItemOptionToMob>();
                                        for (int i = 1; ; i++)
                                        {
                                            Wz_Node optNode = propNode.FindNodeByPath(i.ToString());
                                            if (optNode == null)
                                            {
                                                break;
                                            }

                                            SetItemOptionToMob option = new SetItemOptionToMob();

                                            foreach (Wz_Node pNode in optNode.Nodes)
                                            {
                                                switch (pNode.Text)
                                                {
                                                    case "mob":
                                                        foreach (Wz_Node mobNode in pNode.Nodes)
                                                        {
                                                            option.Mobs.Add(mobNode.GetValue<int>());
                                                        }
                                                        break;

                                                    case "mobName":
                                                        option.MobName = pNode.GetValue<string>();
                                                        break;

                                                    default:
                                                        {
                                                            GearPropType type;
                                                            if (Enum.TryParse(pNode.Text, out type))
                                                            {
                                                                option.Props.Add(type, pNode.GetValue<int>());
                                                            }
                                                        }
                                                        break;
                                                }
                                            }

                                            opToMobList.Add(option);
                                        }
                                        effect.Props.Add(GearPropType.OptionToMob, opToMobList);
                                        break;

                                    case "activeSkill":
                                        List<SetItemActiveSkill> activeSkillList = new List<SetItemActiveSkill>();
                                        for (int i = 0; ; i++)
                                        {
                                            Wz_Node optNode = propNode.FindNodeByPath(i.ToString());
                                            if (optNode == null)
                                            {
                                                break;
                                            }

                                            SetItemActiveSkill activeSkill = new SetItemActiveSkill();
                                            foreach (Wz_Node pNode in optNode.Nodes)
                                            {
                                                switch (pNode.Text)
                                                {
                                                    case "id":
                                                        activeSkill.SkillID = pNode.GetValue<int>();
                                                        break;

                                                    case "level":
                                                        activeSkill.Level= pNode.GetValue<int>();
                                                        break;
                                                }
                                            }
                                            activeSkillList.Add(activeSkill);
                                        }
                                        effect.Props.Add(GearPropType.activeSkill, activeSkillList);
                                        break;

                                    case "bonusByTime":
                                        var bonusByTimeList = new List<SetItemBonusByTime>();
                                        for (int i = 0; ; i++)
                                        {
                                            Wz_Node optNode = propNode.FindNodeByPath(i.ToString());
                                            if (optNode == null)
                                            {
                                                break;
                                            }

                                            var bonusByTime = new SetItemBonusByTime();
                                            foreach (Wz_Node pNode in optNode.Nodes)
                                            {
                                                switch (pNode.Text)
                                                {
                                                    case "termStart":
                                                        bonusByTime.TermStart = pNode.GetValue<int>();
                                                        break;

                                                    default:
                                                        {
                                                            GearPropType type;
                                                            if (Enum.TryParse(pNode.Text, out type))
                                                            {
                                                                bonusByTime.Props.Add(type, pNode.GetValue<int>());
                                                            }
                                                        }
                                                        break;
                                                }
                                            }
                                            bonusByTimeList.Add(bonusByTime);
                                        }
                                        effect.Props.Add(GearPropType.bonusByTime, bonusByTimeList);
                                        break;

                                    default:
                                        {
                                            GearPropType type;
                                            if (Enum.TryParse(propNode.Text, out type))
                                            {
                                                effect.Props.Add(type, Convert.ToInt32(propNode.Value));
                                            }
                                        }
                                        break;
                                }
                            }
                            setItem.Effects.Add(count, effect);
                        }
                        break;
                    case "Desc":
                        foreach (var descNode in subNode.Nodes)
                        {
                            desc[descNode.Text] = Convert.ToString(descNode.Value);
                        }
                        break;
                }
            }

            //处理额外分组
            if (desc.Count > 0)
            {
                foreach (var kv in desc)
                {
                    SetItemIDPart combinePart = null;
                    string combineTypeName = null;
                    switch (kv.Key)
                    {
                        case "weapon":
                            combinePart = CombinePart(setItem, gearID => Gear.IsWeapon(Gear.GetGearType(gearID)));
                            combineTypeName = ItemStringHelper.GetSetItemGearTypeString(GearType.weapon);
                            break;

                        case "subweapon":
                            combinePart = CombinePart(setItem, gearID => Gear.IsSubWeapon(Gear.GetGearType(gearID)));
                            combineTypeName = ItemStringHelper.GetSetItemGearTypeString(GearType.subWeapon);
                            break;

                        case "pocket":
                            combinePart = CombinePart(setItem, gearID => Gear.GetGearType(gearID) == GearType.pocket);
                            combineTypeName = ItemStringHelper.GetSetItemGearTypeString(GearType.pocket);
                            break;
                    }

                    if (combinePart != null)
                    {
                        combinePart.RepresentName = kv.Value;
                        combinePart.TypeName = combineTypeName; ItemStringHelper.GetSetItemGearTypeString(GearType.weapon);
                    }
                }
            }

          
            return setItem;
        }

        /// <summary>
        /// 按一定条件合并装备部件的分组。
        /// </summary>
        /// <param name="predicate">装备id符合条件的判断方法。</param>
        /// <returns></returns>
        private static SetItemIDPart CombinePart(SetItem setItem, Predicate<int> predicate)
        {
            List<int> itemIDList = new List<int>();
            List<int> preRemovedPartIdx = new List<int>();
            int? idx = null;
            foreach (var part in setItem.ItemIDs.Parts)
            {
                bool add = false;
                foreach (var itemID in part.Value.ItemIDs.Keys)
                {
                    if (predicate(itemID)) //id满足条件
                    {
                        itemIDList.Add(itemID);
                        add = true;
                    }
                }
                
                if (add) //提取出被合并项的最大partID
                {
                    //idx = idx == null ? part.Key : Math.Max(part.Key, idx.Value);
                    if (!preRemovedPartIdx.Contains(part.Key))
                        preRemovedPartIdx.Add(part.Key);
                }

                idx = idx == null ? part.Key : Math.Max(part.Key, idx.Value);
            }
            if (itemIDList.Count > 0)
            {
                SetItemIDPart part = new SetItemIDPart(itemIDList);
                foreach (int i in preRemovedPartIdx)
                {
                    setItem.ItemIDs.Remove(i);
                }
                setItem.ItemIDs.Add(idx.Value + 1, part);
                return part;
            }
            return null;
        }
    }
}

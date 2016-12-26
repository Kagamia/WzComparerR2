using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Addition
    {
        public Addition()
        {
            Props = new Dictionary<string, string>();
            ConValue = new List<int>();
        }

        public AdditionType Type { get; set; }
        public GearPropType ConType { get; set; }
        public List<int> ConValue { get; private set; }
        public Dictionary<string, string> Props { get; private set; }

        public string GetPropString()
        {
            StringBuilder sb;
            switch (this.Type)
            {
                case AdditionType.boss:
                    sb = new StringBuilder();
                    sb.Append("攻击BOSS时，");
                    {
                        string v1;
                        if (this.Props.TryGetValue("prob", out v1))
                            sb.Append("有" + v1 + "的几率");
                        sb.Append("造成" + Props["damage"] + "%的额外伤害");
                    }
                    return sb.ToString();
                case AdditionType.critical:
                    sb = new StringBuilder();
                    {
                        string val;
                        if (this.Props.TryGetValue("prob", out val))
                        {
                            sb.AppendFormat("爆击率{0}%\r\n", val);
                        }
                        if (this.Props.TryGetValue("damage", out val))
                        {
                            sb.AppendFormat("爆击伤害增加{0}%\r\n", val);
                        }
                        if (sb.Length > 2)
                        {
                            sb.Remove(sb.Length - 2, 2);
                        }
                    }
                    return sb.ToString();
                case AdditionType.elemboost:
                    {
                        string v1, elem;
                        if (this.Props.TryGetValue("elemVol", out v1))
                        {
                            switch (v1[0])
                            {
                                case 'I': elem = "冰"; break;
                                case 'F': elem = "火"; break;
                                case 'L': elem = "雷"; break;
                                default: elem = v1[0].ToString(); break;
                            }
                            return elem + "属性效果强化" + v1.Substring(1) + "%";
                        }
                    }
                    break;
                case AdditionType.hpmpchange:
                    sb = new StringBuilder();
                    sb.Append("每10秒恢复");
                    {
                        string v1;
                        if (this.Props.TryGetValue("hpChangePerTime", out v1))
                        {
                            sb.Append("HP " + v1);
                        }
                    }
                    return sb.ToString();
                case AdditionType.mobcategory:
                    return "攻击" + ItemStringHelper.GetMobCategoryName(Convert.ToInt32(this.Props["category"])) + "怪物时，造成" + this.Props["damage"] + "%额外伤害";
                case AdditionType.mobdie:
                    sb = new StringBuilder();
                    {
                        string v1;
                        if (this.Props.TryGetValue("hpIncOnMobDie", out v1))
                        {
                            sb.AppendLine("怪物死亡时 HP恢复" + v1);
                        }
                        if (this.Props.TryGetValue("hpIncRatioOnMobDie", out v1))
                        {
                            sb.AppendLine("怪物死亡时 有" + Props["hpRatioProp"] + "%的几率 伤害的" + v1 + "%转换为HP (但不超过最大HP的10%。)");
                        }
                        if (this.Props.TryGetValue("mpIncOnMobDie", out v1))
                        {
                            sb.AppendLine("怪物死亡时 HP恢复" + v1);
                        }
                        if (this.Props.TryGetValue("mpIncRatioOnMobDie", out v1))
                        {
                            sb.AppendLine("怪物死亡时 有" + Props["mpRatioProp"] + "%的几率 伤害的" + v1 + "%转换为MP (但不超过最大MP的10%。)");
                        }
                    }
                    if (sb.Length > 0)
                    {
                        sb.Append("在部分地区功能可能会受到限制。");
                        return sb.ToString();
                    }
                    break;
                case AdditionType.skill:
                    switch (Convert.ToInt32(this.Props["id"]))
                    {
                        case 90000000: return "有一定几率增加必杀效果";
                        case 90001001: return "有一定几率增加眩晕效果";
                        case 90001002: return "有一定几率增加缓速术效果";
                        case 90001003: return "有一定几率增加毒效果";
                        case 90001004: return "有一定几率增加暗黑效果";
                        case 90001005: return "有一定几率增加封印效果";
                        case 90001006: return "有一定几率增加结冰效果";
                    }
                    break;
                case AdditionType.statinc:
                    sb = new StringBuilder();
                    {
                        foreach (var kv in Props)
                        {
                            try
                            {
                                GearPropType propType = (GearPropType)Enum.Parse(typeof(GearPropType), kv.Key);
                                sb.AppendLine(ItemStringHelper.GetGearPropString(propType, Convert.ToInt32(kv.Value)));
                            }
                            catch
                            {
                            }
                        }
                    }
                    if (sb.Length > 0)
                    {
                        return sb.ToString();
                    }
                    break;
                default: return null;
            }
            return null;
        }

        public string GetConString()
        {
            switch (this.ConType)
            {
                case GearPropType.reqJob:
                    string[] reqJobs = new string[this.ConValue.Count];
                    for (int i = 0; i < reqJobs.Length; i++)
                    {
                        reqJobs[i] = ItemStringHelper.GetJobName(this.ConValue[i]) ?? this.ConValue[i].ToString();
                    }
                    return "职业为" + string.Join(" 或者 ", reqJobs) + "时";
                case GearPropType.reqLevel:
                    return this.ConValue[0] + "级以上时";
                case GearPropType.reqCraft:
                    int lastExp;
                    return "手技经验值在" + this.ConValue[0] + "(" + getPersonalityLevel(this.ConValue[0], out lastExp) + "级" + lastExp + "点)以上时";
                case GearPropType.reqWeekDay:
                    string[] weekdays = new string[this.ConValue.Count];
                    for (int i = 0; i < this.ConValue.Count; i++)
                    {
                        weekdays[i] = GetWeekDayString(this.ConValue[i]);
                    }
                    return string.Join(", ", weekdays) + "时";
                default:
                    return null;
            }
        }

        private int getPersonalityLevel(int totalExp, out int lastExp)
        {
            int curExp = 0;
            for (int level = 0; ; level++)
            {
                if (level == 0)
                {
                    curExp = 20;
                }
                else if (level < 10)
                {
                    curExp = (int)Math.Round(curExp * 1.3, MidpointRounding.AwayFromZero);
                }
                else if (level < 20)
                {
                    curExp = (int)Math.Round(curExp * 1.1, MidpointRounding.AwayFromZero);
                }
                else if (level < 30)
                {
                    curExp = (int)Math.Round(curExp * 1.03, MidpointRounding.AwayFromZero);
                }
                else if (level < 70)
                {
                    curExp = (int)Math.Round(curExp * 1.015, MidpointRounding.AwayFromZero);
                }
                else if (level < 100)
                {
                    curExp = (int)Math.Round(curExp * 1.003, MidpointRounding.AwayFromZero);
                }
                else
                {
                    lastExp = 0;
                    return 100;
                }
                if (totalExp - curExp <= 0)
                {
                    lastExp = totalExp;
                    return level;
                }
                else
                {
                    totalExp -= curExp;
                }
            }
        }

        private static string GetWeekDayString(int weekDay)
        {
            switch (weekDay)
            {
                case 0: return "周日";
                case 1: return "周一";
                case 2: return "周二";
                case 3: return "周三";
                case 4: return "周四";
                case 5: return "周五";
                case 6: return "周六";
                default: return "周" + weekDay; //这怎么可能...
            }
        }

        public static Addition CreateFromNode(Wz_Node node)
        {
            if (node == null)
                return null;
            foreach (AdditionType type in Enum.GetValues(typeof(AdditionType)))
            {
                if (type.ToString() == node.Text)
                {
                    Addition addition = new Addition();
                    addition.Type = type;
                    Action<Wz_Node> addInt32 = n => addition.ConValue.Add(n.GetValue<int>());
                    Action<Wz_Node> addWeekDay = n =>
                    {
                        try
                        {
                            DayOfWeek weekday = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), n.GetValue<string>(), true);
                            addition.ConValue.Add((int)weekday);
                        }
                        catch { }
                    };

                    foreach (Wz_Node subNode in node.Nodes)
                    {
                        if (subNode.Text == "con")
                        {
                            Action<Wz_Node> addValueFunc = addInt32;
                            foreach (Wz_Node conNode in subNode.Nodes)
                            {
                                switch (conNode.Text)
                                {
                                    case "job":
                                        addition.ConType = GearPropType.reqJob;
                                        break;
                                    //case "lv": //已不被官方识别了
                                    case "level":
                                        addition.ConType = GearPropType.reqLevel;
                                        break;
                                    case "craft":
                                        addition.ConType = GearPropType.reqCraft;
                                        break;
                                    case "weekDay":
                                        addition.ConType = GearPropType.reqWeekDay;
                                        addValueFunc = addWeekDay; //改变解析方法
                                        break;
                                    default: //不识别的东西
                                        addition.ConType = (GearPropType)0;
                                        continue;
                                }

                                if (conNode.Nodes.Count > 0)
                                {
                                    foreach (Wz_Node conValNode in conNode.Nodes)
                                    {
                                        addValueFunc(conValNode);
                                    }
                                }
                                else
                                {
                                    addValueFunc(conNode);
                                }
                            }
                        }
                        else
                        {
                            addition.Props.Add(subNode.Text, Convert.ToString(subNode.Value));
                        }
                    }
                    return addition;
                }
            }
            return null;
        }
    }
}

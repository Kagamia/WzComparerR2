using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.Common;

namespace WzComparerR2
{
    public class SummaryParser
    {
        static SummaryParser()
        {
            List<string> lst = new List<string>() { "comboConAran" };
            lst.Sort((a, b) => a.Length.CompareTo(b.Length));
            GlobalVariables = new System.Collections.ObjectModel.ReadOnlyCollection<string>(lst);
        }

        public static string GetSkillSummary(string H, int Level, Dictionary<string, string> CommonProps, SummaryParams param)
        {
            if (H == null) return null;

            int idx = 0;
            StringBuilder sb = new StringBuilder();
            bool beginC = false;
            while (idx < H.Length)
            {
                if (H[idx] == '#')
                {
                    int end = idx, len = 0;
                    while ((++end) < H.Length)
                    {
                        if ((H[end] >= 'a' && H[end] <= 'z') ||
                            (H[end] >= 'A' && H[end] <= 'Z') ||
                            (H[end] >= '0' && H[end] <= '9') ||
                            H[end] == '_')
                        {
                            len++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    //优先匹配common
                    string prop = null;
                    if (CommonProps != null)
                    {
                        for (int i = len; i > 0; i--)
                        {
                            bool find = false;
                            string key = H.Substring(idx + 1, i);
                            foreach (var kv in CommonProps)
                            {
                                if (kv.Key.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    find = true;
                                    prop = kv.Value;
                                    break;
                                }
                            }
                            if (find)
                            {
                                len = i;
                                break;
                            }
                        }
                    }
                    if (prop != null)
                    {
                        double val = Calculator.Parse(prop, Level);
                        sb.Append(val);
                        idx += len + 1;
                        continue;
                    }
                    else //试图匹配全局变量
                    {
                        for (int i = len; i > 0; i--)
                        {
                            string key = H.Substring(idx + 1, i);
                            if (GlobalVariables.Contains(key))
                            {
                                prop = "[" + key + "]";
                                break;
                            }
                        }
                        if (prop != null)
                        {
                            sb.Append(param.GStart).Append(prop).Append(param.GEnd);
                            idx += len + 1;
                            continue;
                        }
                    }
                    //匹配#c...#段落
                    if (beginC)
                    {
                        beginC = false;
                        sb.Append(param.CEnd);
                        idx++;
                    }
                    else if (idx + 1 < H.Length && H[idx + 1] == 'c')
                    {
                        beginC = true;
                        sb.Append(param.CStart);
                        idx += 2;
                    }
                    else if (idx + 1 < H.Length && len == 0)//匹配省略c的段落
                    {
                        beginC = true;
                        sb.Append(param.CStart);
                        idx++;
                    }
                    else //无法匹配 取最长的common段
                    {
                        string key = H.Substring(idx + 1, len);
                        if (System.Text.RegularExpressions.Regex.IsMatch(key, @"^\d+$"))
                        {
                            sb.Append(key);
                        }
                        else
                        {
                            sb.Append(0);//默认值
                        }
                        idx += len + 1;
                    }
                }
                else if (H[idx] == '\\')
                {
                    if (idx + 1 < H.Length)
                    {
                        switch (H[idx + 1])
                        {
                            case 'r': sb.Append(param.R); break;
                            case 'n': sb.Append(param.N); break;
                            case '\\': sb.Append('\\'); break;
                            default: sb.Append(H[idx + 1]); break;
                        }
                        idx += 2;
                    }
                }
                else
                {
                    sb.Append(H[idx++]);
                }
            }
            return sb.ToString();
        }

        public static string GetSkillSummary(Skill skill, StringResult sr, SummaryParams param)
        {
            if (skill == null)
                return null;
            return GetSkillSummary(skill, skill.Level, sr, param);
        }

        public static string GetSkillSummary(Skill skill, int level, StringResult sr, SummaryParams param)
        {
            if (skill == null || sr == null)
                return null;

            string h = null;
            if (skill.PreBBSkill) //用level声明的技能
            {
                string hs;
                if (skill.Level == level && skill.Common.TryGetValue("hs", out hs))
                {
                    h = sr[hs];
                }
                else if (sr.SkillH.Count >= level)
                {
                    h = sr.SkillH[level - 1];
                }
            }
            else
            {
                if (sr.SkillH.Count > 0)
                {
                    h = sr.SkillH[0];
                }
            }

            return GetSkillSummary(h, level, skill.Common, param);
        }

        public static System.Collections.ObjectModel.ReadOnlyCollection<string> GlobalVariables { get; private set; }
    }
}

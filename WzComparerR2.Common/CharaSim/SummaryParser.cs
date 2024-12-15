using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.Common;

namespace WzComparerR2.CharaSim
{
    public class SummaryParser
    {
        static SummaryParser()
        {
            GlobalVariableMapping = new Dictionary<string, string>();
            GlobalVariableMapping["comboConAran"] = "aranComboCon";
        }

        public static string GetSkillSummary(string H, int Level, Dictionary<string, string> CommonProps, SummaryParams param, SkillSummaryOptions options = default)
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
                        if (H[end] == '_' ||
                            ('a' <= H[end] && H[end] <= 'z') ||
                            ('A' <= H[end] && H[end] <= 'Z') ||
                            (end - idx > 1 && '0' <= H[end] && H[end] <= '9')) //^[_A-Za-z][_A-Za-z0-9]*$
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
                    string propKey = null;
                    if (CommonProps != null)
                    {
                        for (int i = len; i > 0; i--)
                        {
                            propKey = H.Substring(idx + 1, i);
                            if (GetValueIgnoreCase(CommonProps, propKey, out prop))
                            {
                                len = i;
                                break;
                            }
                        }
                    }
                    if (prop != null)
                    {
                        try
                        {
                            decimal val = Calculator.Parse(prop, Level);
                            if (options.ConvertCooltimeMS && propKey == "cooltimeMS")
                            {
                                sb.AppendFormat("{0:f2}", val / 1000);
                            }
                            else if (options.ConvertPerM && propKey.EndsWith("PerM", StringComparison.Ordinal))
                            {
                                sb.AppendFormat("{0:f1}", val / 100);
                            }
                            else
                            {
                                sb.Append(val);
                            }
                        }
                        catch
                        {
                            if (options.IgnoreEvalError)
                            {
                                sb.Append("NaN");
                            }
                            else
                            {
                                throw;
                            }
                        }

                        idx += len + 1;
                        continue;
                    }
                    else //试图匹配全局变量
                    {
                        string key = null;
                        for (int i = len; i > 0; i--)
                        {
                            key = H.Substring(idx + 1, i);
                            if (GlobalVariableMapping.TryGetValue(key, out prop))
                            {
                                break;
                            }
                        }
                        if (prop != null)
                        {
                            if (prop != "" && GetValueIgnoreCase(CommonProps, prop, out prop))
                            {
                                try
                                {
                                    decimal val = Calculator.Parse(prop, Level);
                                    sb.Append(val);
                                }
                                catch
                                {
                                    if (options.IgnoreEvalError)
                                    {
                                        sb.Append("NaN");
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }
                            }
                            else
                            {
                                sb.Append(param.GStart).Append("[").Append(key).Append("]").Append(param.GEnd);
                            }
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
                    else if (len > 0)//无法匹配 取最长的common段
                    {
                        string key = H.Substring(idx + 1, len);
                        if (Regex.IsMatch(key, @"^\d+$"))
                        {
                            sb.Append(key);
                        }
                        else
                        {
                            sb.Append(0);//默认值
                        }
                        idx += len + 1;
                    }
                    else // skip last #
                    {
                        idx++;
                    }
                }
                else if (H[idx] == '\\')
                {
                    if (idx + 1 < H.Length)
                    {
                        switch (H[idx + 1])
                        {
                            case 'c': break; // \c忽略掉 原因不明
                            case 'r': sb.Append(param.R); break;
                            case 'n':
                                if (beginC && options.EndColorOnNewLine)
                                {
                                    beginC = false;
                                    sb.Append(param.CEnd);
                                }
                                sb.Append(param.N);
                                break;
                            case '\\': sb.Append('\\'); break;
                            default: sb.Append(H[idx + 1]); break;
                        }
                        idx += 2;
                    }
                    else //转义失败
                    {
                        idx++;
                    }
                }
                else
                {
                    sb.Append(H[idx++]);
                }
            }
            return sb.ToString();
        }

        private static bool GetValueIgnoreCase(Dictionary<string,string> dict, string key, out string value)
        {
            //bool find = false;
            foreach (var kv in dict)
            {
                if (kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kv.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }

        public static string GetSkillSummary(Skill skill, StringResult sr, SummaryParams param)
        {
            if (skill == null)
                return null;
            return GetSkillSummary(skill, skill.Level, sr, param);
        }

        public static string GetSkillSummary(Skill skill, int level, StringResult sr, SummaryParams param, SkillSummaryOptions options = default)
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
                else if (sr.SkillH.Count == 1)
                {
                    h = sr.SkillH[0];
                }
                var levelCommon = level <= skill.levelCommon.Count ? skill.levelCommon[level - 1] : skill.common;
                return GetSkillSummary(h, level, levelCommon, param, options);
            }
            else
            {
                if (sr.SkillH.Count > 0)
                {
                    h = sr.SkillH[0];
                }
                return GetSkillSummary(h, level, skill.Common, param, options);
            }
        }

        public static Dictionary<string,string> GlobalVariableMapping { get; private set; }
    }

    public struct SkillSummaryOptions
    {
        public bool ConvertCooltimeMS { get; set; }
        public bool ConvertPerM { get; set; }
        public bool IgnoreEvalError { get; set; }
        public bool EndColorOnNewLine { get; set; }
    }
}

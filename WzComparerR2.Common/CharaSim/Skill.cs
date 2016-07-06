using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Skill
    {
        public Skill()
        {
            this.level = 0;
            this.levelCommon = new List<Dictionary<string, string>>();
            this.common = new Dictionary<string, string>();
            this.PVPcommon = new Dictionary<string, string>();
            this.ReqSkill = new Dictionary<int, int>();
            this.Action = new List<string>();
        }

        private int level;
        internal List<Dictionary<string, string>> levelCommon;
        internal Dictionary<string, string> common;

        public Dictionary<string, string> Common
        {
            get
            {
                if (PreBBSkill && this.level > 0 && this.level <= levelCommon.Count)
                    return levelCommon[this.level - 1];
                else
                    return common;
            }
        }

        public Dictionary<string, string> PVPcommon { get; private set; }
        public int SkillID { get; set; }
        public BitmapOrigin Icon { get; set; }
        public BitmapOrigin IconMouseOver { get; set; }
        public BitmapOrigin IconDisabled { get; set; }

        public HyperSkillType Hyper { get; set; }

        public int Level
        {
            get { return level; }
            set
            {
                level = Math.Max(0, Math.Min(value, (this.CombatOrders ? 100 : this.MaxLevel)));
            }
        }

        public int ReqLevel { get; set; }
        public int ReqAmount { get; set; }
        public bool PreBBSkill { get; set; }
        public bool Invisible { get; set; }
        public bool CombatOrders { get; set; }
        public bool NotRemoved { get; set; }
        public int MasterLevel { get; set; }
        public Dictionary<int, int> ReqSkill { get; private set; }
        public List<string> Action { get; private set; }

        public int MaxLevel
        {
            get
            {
                string v;
                if (this.PreBBSkill)
                    return levelCommon.Count;
                else if (common.TryGetValue("maxLevel", out v))
                    return Convert.ToInt32(v);
                return 0;
            }
        }

        public static Skill CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            Skill skill = new Skill();
            int skillID;
            if (!Int32.TryParse(node.Text, out skillID))
                return null;
            skill.SkillID = skillID;

            foreach (Wz_Node childNode in node.Nodes)
            {
                switch (childNode.Text)
                {
                    case "icon":
                        skill.Icon = BitmapOrigin.CreateFromNode(childNode, findNode);
                        break;
                    case "iconMouseOver":
                        skill.IconMouseOver = BitmapOrigin.CreateFromNode(childNode, findNode);
                        break;
                    case "iconDisabled":
                        skill.IconDisabled = BitmapOrigin.CreateFromNode(childNode, findNode);
                        break;
                    case "common":
                        foreach (Wz_Node commonNode in childNode.Nodes)
                        {
                            if (commonNode.Value != null && !(commonNode.Value is Wz_Vector))
                            {
                                skill.common[commonNode.Text] = commonNode.Value.ToString();
                            }
                        }
                        break;
                    case "PVPcommon":
                        foreach (Wz_Node commonNode in childNode.Nodes)
                        {
                            if (commonNode.Value != null && !(commonNode.Value is Wz_Vector))
                            {
                                skill.PVPcommon[commonNode.Text] = commonNode.Value.ToString();
                            }
                        }
                        break;
                    case "level":
                        for (int i = 1; ; i++)
                        {
                            Wz_Node levelNode = childNode.FindNodeByPath(i.ToString());
                            if (levelNode == null)
                                break;
                            Dictionary<string, string> levelInfo = new Dictionary<string, string>();

                            foreach (Wz_Node commonNode in levelNode.Nodes)
                            {
                                if (commonNode.Value != null && !(commonNode.Value is Wz_Vector))
                                {
                                    levelInfo[commonNode.Text] = commonNode.Value.ToString();
                                }
                            }

                            skill.levelCommon.Add(levelInfo);
                        }
                        break;
                    case "hyper":
                        skill.Hyper = (HyperSkillType)childNode.GetValue<int>();
                        break;
                    case "invisible":
                        skill.Invisible = childNode.GetValue<int>() != 0;
                        break;
                    case "combatOrders":
                        skill.CombatOrders = childNode.GetValue<int>() != 0;
                        break;
                    case "notRemoved":
                        skill.NotRemoved = childNode.GetValue<int>() != 0;
                        break;
                    case "masterLevel":
                        skill.MasterLevel = childNode.GetValue<int>();
                        break;
                    case "reqLev":
                        skill.ReqLevel = childNode.GetValue<int>();
                        break;
                    case "req":
                        foreach (Wz_Node reqNode in childNode.Nodes)
                        {
                            if (reqNode.Text == "level")
                            {
                                skill.ReqLevel = reqNode.GetValue<int>();
                            }
                            else if (reqNode.Text == "reqAmount")
                            {
                                skill.ReqAmount = reqNode.GetValue<int>();
                            }
                            else
                            {
                                int reqSkill;
                                if (Int32.TryParse(reqNode.Text, out reqSkill))
                                {
                                    skill.ReqSkill[reqSkill] = reqNode.GetValue<int>();
                                }
                            }
                        }
                        break;
                    case "action":
                        for (int i = 0; ; i++)
                        {
                            Wz_Node idxNode = childNode.FindNodeByPath(i.ToString());
                            if (idxNode == null)
                                break;
                            skill.Action.Add(idxNode.GetValue<string>());
                        }
                        break;
                }
            }

            //判定技能声明版本
            skill.PreBBSkill = false;
            if (skill.levelCommon.Count > 0)
            {
                if (skill.common.Count <= 0
                    || (skill.common.Count == 1 && skill.common.ContainsKey("maxLevel")))
                {
                    skill.PreBBSkill = true;
                }
            }

            return skill;
        }
    }
}

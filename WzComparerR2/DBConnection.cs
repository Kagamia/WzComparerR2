using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using System.Drawing;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using WzComparerR2.CharaSimControl;
using WzComparerR2.CharaSim;

namespace WzComparerR2
{
    public class DBConnection
    {
        public DBConnection(StringLinker sl)
        {
            this.sl = sl;
        }

        private StringLinker sl;

        public DataSet GenerateSkillTable()
        {
            Wz_Node skillWz = PluginManager.FindWz(Wz_Type.Skill);
            if (skillWz == null)
                return null;

            Regex r = new Regex(@"^(\d+)\.img", RegexOptions.Compiled);

            DataSet ds = new DataSet();
            DataTable jobTable = new DataTable("ms_job");
            jobTable.Columns.Add("jobID", typeof(string));
            jobTable.Columns.Add("jobName", typeof(string));
 
            DataTable skillTable = new DataTable("ms_skill");
            skillTable.Columns.Add("jobID", typeof(string));
            skillTable.Columns.Add("skillID", typeof(string));
            skillTable.Columns.Add("skillName", typeof(string));
            skillTable.Columns.Add("skillDesc", typeof(string));
            skillTable.Columns.Add("maxLevel", typeof(int));
            skillTable.Columns.Add("invisible", typeof(bool));
            skillTable.Columns.Add("hyper", typeof(int));
            skillTable.Columns.Add("reqSkill", typeof(string));
            skillTable.Columns.Add("reqSkillLevel", typeof(int));
            skillTable.Columns.Add("reqLevel", typeof(int));

            DataTable skillLevelTable = new DataTable("ms_skillLevel");
            skillLevelTable.Columns.Add("skillID", typeof(string));
            skillLevelTable.Columns.Add("level", typeof(int));
            skillLevelTable.Columns.Add("levelDesc", typeof(string));

            DataTable skillCommonTable = new DataTable("ms_skillCommon");
            skillCommonTable.Columns.Add("skillID", typeof(string));
            skillCommonTable.Columns.Add("commonName", typeof(string));
            skillCommonTable.Columns.Add("commonValue", typeof(string));

            DataTable skillPVPCommonTable = new DataTable("ms_skillPVPCommon");
            skillPVPCommonTable.Columns.Add("skillID", typeof(string));
            skillPVPCommonTable.Columns.Add("commonName", typeof(string));
            skillPVPCommonTable.Columns.Add("commonValue", typeof(string));

            DataTable skillHTable = new DataTable("ms_skillH");
            skillHTable.Columns.Add("skillID", typeof(string));
            skillHTable.Columns.Add("desc", typeof(string));
            skillHTable.Columns.Add("pdesc", typeof(string));
            skillHTable.Columns.Add("h", typeof(string));
            skillHTable.Columns.Add("ph", typeof(string));
            skillHTable.Columns.Add("hch", typeof(string));

            StringResult sr;

            foreach (Wz_Node node in skillWz.Nodes)
            {
                //获取职业
                Match m = r.Match(node.Text);
                Wz_Image img = node.GetValue<Wz_Image>(null);
                if (!m.Success)
                {
                    continue;
                }
                if (img == null || !img.TryExtract())
                {
                    continue;
                }
                //导入职业
                string jobID = m.Result("$1");
                sl.StringSkill2.TryGetValue(jobID, out sr);
                jobTable.Rows.Add(jobID, (sr != null ? sr["bookName"] : null));

                //获取技能
                Wz_Node skillListNode = img.Node .FindNodeByPath("skill");
                if (skillListNode == null || skillListNode.Nodes.Count <= 0)
                {
                    continue;
                }

                foreach (Wz_Node skillNode in skillListNode.Nodes)
                {
                    Skill skill = Skill.CreateFromNode(skillNode,  PluginManager.FindWz);
                    if (skill == null)
                        continue;

                    // if (skill.Invisible) //过滤不可见技能
                    //     continue;

                    //导入技能
                    string skillID = skillNode.Text;
                    sl.StringSkill2.TryGetValue(skillID, out sr);

                    string reqSkill = null;
                    int reqSkillLevel = 0;
                    if (skill.ReqSkill.Count > 0)
                    {
                        foreach (var kv in skill.ReqSkill)
                        {
                            reqSkill = kv.Key.ToString();
                            reqSkillLevel = kv.Value;
                        }
                    }

                    skillTable.Rows.Add(
                        jobID,
                        skillID,
                        sr != null ? sr.Name : null,
                        sr != null ? sr.Desc : null,
                        skill.MaxLevel,
                        skill.Invisible,
                        skill.Hyper,
                        reqSkill,
                        reqSkillLevel,
                        skill.ReqLevel
                    );

                   
                    if (!skill.PreBBSkill)
                    {
                        //导入技能common
                        foreach (var kv in skill.Common)
                        {
                            skillCommonTable.Rows.Add(
                                skillID,
                                kv.Key,
                                kv.Value
                                );
                        }
                        foreach (var kv in skill.PVPcommon)
                        {
                            skillPVPCommonTable.Rows.Add(
                                skillID,
                                kv.Key,
                                kv.Value
                                );
                        }
                        //导入技能说明
                        skillHTable.Rows.Add(
                            skillID,
                            sr != null ? sr["desc"] : null,
                            sr != null ? sr["pdesc"] : null,
                            sr != null ? sr["h"] : null,
                            sr != null ? sr["ph"] : null,
                            sr != null ? sr["hch"] : null
                            );
                    }

                    //导入技能等级
                    for (int i = 1, j = skill.MaxLevel + (skill.CombatOrders ? 2 : 0); i <= j; i++)
                    {
                        skill.Level = i;
                        string levelDesc = SummaryParser.GetSkillSummary(skill, sr, SummaryParams.Default);
                        skillLevelTable.Rows.Add(
                            skillID,
                            i,
                            levelDesc);

                    }
                }

                img.Unextract();
            }

            ds.Tables.Add(jobTable);
            ds.Tables.Add(skillTable);
            ds.Tables.Add(skillLevelTable);
            ds.Tables.Add(skillCommonTable);
            ds.Tables.Add(skillPVPCommonTable);
            ds.Tables.Add(skillHTable);
            return ds;
        }

        public void OutputCsv(StreamWriter sw, DataTable dt)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                DataColumn col = dt.Columns[i];
                sw.Write(ConvertCell(col.ColumnName));

                if (i < dt.Columns.Count - 1)
                    sw.Write(",");
                else
                    sw.WriteLine();
            }

            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    sw.Write(ConvertCell(Convert.ToString(row[i])));

                    if (i < dt.Columns.Count - 1)
                        sw.Write(",");
                    else
                        sw.WriteLine();
                }
            }
        }

        private string ConvertCell(string input)
        {
            if (input != null)
            {
                input = ReplaceQoute(input);
                if (input.IndexOfAny(",\"\r\n".ToCharArray()) > -1)
                {
                    input = "\"" + input + "\"";
                }
            }
            return input;
        }

        private string ReplaceQoute(string input)
        {
            if (input == null)
                return null;
            if (!input.Contains("\""))
                return input;
            return input.Replace("\"", "\"\"");
        }

        public void ExportSkillOption(string outputDir)
        {
            Wz_Node skillOption = PluginBase.PluginManager.FindWz("Item/SkillOption.img");
            Wz_Node itemOption = PluginBase.PluginManager.FindWz("Item/ItemOption.img");
            Wz_Node item0259 = PluginBase.PluginManager.FindWz("Item/Consume/0259.img");
            Wz_Node skill8000 = PluginBase.PluginManager.FindWz("Skill/8000.img/skill");

            if (skillOption == null || itemOption == null || item0259 == null || skill8000 == null)
                return;

            ItemTooltipRender2 itemRender = new ItemTooltipRender2();
            itemRender.StringLinker = this.sl;
            itemRender.ShowObjectID = true;
            SkillTooltipRender2 skillRender = new SkillTooltipRender2();
            skillRender.StringLinker = this.sl;
            skillRender.ShowObjectID = true;
            skillRender.ShowDelay = true;

            string skillImageDir = Path.Combine(outputDir, "skills");
            string itemImageDir = Path.Combine(outputDir, "items");

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            if (!Directory.Exists(skillImageDir))
                Directory.CreateDirectory(skillImageDir);
            if (!Directory.Exists(itemImageDir))
                Directory.CreateDirectory(itemImageDir);

            StringResult sr;

            FileStream fs = new FileStream(Path.Combine(outputDir, "SkillOption.html"), FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            try
            {
                sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
                sw.WriteLine("<html>");
                sw.WriteLine("<head>");
                sw.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">");
                sw.WriteLine("<title>魂武器系统</title>");
                sw.WriteLine(@"<style type=""text/css"">");
                sw.WriteLine("table, tr, th, td { font-size:12px; border-collapse:collapse; border:1px solid #c0c0c0; }");
                sw.WriteLine("td { padding:4px 5px; }");
                sw.WriteLine(@"</style>");
                sw.WriteLine("</head>");
                sw.WriteLine("<body>");

                foreach (Wz_Node node in item0259.Nodes)
                {
                    int itemID;
                    if (Int32.TryParse(node.Text, out itemID) && itemID / 1000 == 2591) //02591___
                    {
                        sw.WriteLine(@"<table style=""width:500px; ""><tbody>");
                        sl.StringItem.TryGetValue(itemID, out sr);
                        if (sr != null)
                        {
                            sw.WriteLine(@"<tr style=""background-color:#ffcccc; ""><td>道具名称</td><td>{0} (id:{1})</td></tr>", sr == null ? "null" : sr.Name, itemID);
                        }

                        Item item = Item.CreateFromNode(node, PluginManager.FindWz);
                        if (item != null)
                        {
                            itemRender.Item = item;
                            string imageName = Path.Combine(itemImageDir, item.ItemID + ".png");
                            if (!File.Exists(imageName))
                            {
                                Bitmap itemImage = itemRender.Render();
                                itemImage.Save(imageName, System.Drawing.Imaging.ImageFormat.Png);
                                itemImage.Dispose();
                            }
                            sw.WriteLine(@"<tr><td>道具图片</td><td><img src=""items/{0}.png"" title=""{0}"" /></td></tr>", item.ItemID);
                        }

                        Wz_Node skillOptionNode = skillOption.FindNodeByPath("skill\\" + (itemID % 1000 + 1));
                        if (skillOptionNode != null)
                        {
                            int skillId = skillOptionNode.Nodes["skillId"].GetValueEx<int>(-1);
                            int reqLevel = skillOptionNode.Nodes["reqLevel"].GetValueEx<int>(-1);
                            int incTableId = skillOptionNode.Nodes["incTableID"].GetValueEx<int>(-1);
                            int incRTableId = skillOptionNode.Nodes["incRTableID"].GetValueEx<int>(-1);
                            Wz_Node incNode = null;
                            string per = null;
                            if (incTableId >= 0)
                            {
                                incNode = skillOption.FindNodeByPath("inc\\" + incTableId);
                            }
                            else if (incRTableId >= 0)
                            {
                                incNode = skillOption.FindNodeByPath("incR\\" + incRTableId);
                                per = "%";
                            }
                            if (incNode != null)
                            {
                                sw.WriteLine(@"<tr><td rowspan=""3"">魂珠属性</td><td>阶段{0}: 提升物攻/魔攻 + {1}{2}</td></tr>", incNode.Nodes[0].Text, incNode.Nodes[0].Value, per);
                                sw.WriteLine(@"<tr><td>...</td></tr>");
                                sw.WriteLine(@"<tr><td>阶段{0}: 提升物攻/魔攻 + {1}{2}</td></tr>", incNode.Nodes[incNode.Nodes.Count - 1].Text, incNode.Nodes[incNode.Nodes.Count - 1].Value, per);
                            }

                            sw.WriteLine("<tr><td>需求等级</td><td>{0}</td></tr>", reqLevel);
                            sl.StringSkill.TryGetValue(skillId, out sr);
                            if (sr != null)
                            {
                                sw.WriteLine("<tr><td>技能名称</td><td>{0} (id:{1})</td></tr>", sr == null ? "null" : sr.Name, skillId);
                            }

                            Skill skill = Skill.CreateFromNode(skill8000.Nodes[skillId.ToString("D7")], PluginManager.FindWz);
                            if (skill != null)
                            {
                                skill.Level = skill.MaxLevel;
                                skillRender.Skill = skill;

                                string imageName = Path.Combine(skillImageDir, skill.SkillID + ".png");
                                if (!File.Exists(imageName))
                                {
                                    Bitmap skillImage = skillRender.Render();
                                    skillImage.Save(Path.Combine(skillImageDir, skill.SkillID + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                                    skillImage.Dispose();
                                }
                                sw.WriteLine(@"<tr><td>技能图片</td><td><img src=""skills/{0}.png"" title=""{0}"" /></td></tr>", skill.SkillID);
                            }

                            List<KeyValuePair<int, Potential>> tempOptions = new List<KeyValuePair<int, Potential>>();
                            int totalProb = 0;
                            Wz_Node tempOptionNode = skillOptionNode.Nodes["tempOption"];
                           
                            if (tempOptionNode != null)
                            {
                                foreach (Wz_Node optionNode in tempOptionNode.Nodes)
                                {
                                    int id = optionNode.Nodes["id"].GetValueEx<int>(-1);
                                    int prob = optionNode.Nodes["prob"].GetValueEx<int>(-1);
                                    if (id >= 0 && prob >= 0)
                                    {
                                        Potential optionItem = Potential.CreateFromNode(itemOption.Nodes[id.ToString("000000")], (reqLevel + 9) / 10);
                                        if (optionItem != null)
                                        {
                                            totalProb += prob;
                                            tempOptions.Add(new KeyValuePair<int, Potential>(prob, optionItem));
                                        }
                                    }
                                }
                            }

                            if (tempOptions.Count > 0)
                            {
                                for (int i = 0; i < tempOptions.Count; i++)
                                {
                                    KeyValuePair<int, Potential> opt = tempOptions[i];
                                    sw.Write("<tr>");
                                    if (i == 0)
                                    {
                                        sw.Write(@"<td rowspan=""{0}"">附加属性</td>", tempOptions.Count);
                                    }
                                    sw.WriteLine("<td>{0} &nbsp; &nbsp;[潜能代码:{1:D6}, 获得几率:{2}/{3}({4:P2})])</td></tr>", opt.Value.ConvertSummary(),
                                        opt.Value.code, opt.Key, totalProb, (1.0 * opt.Key / totalProb));
                                }
                            }
                             
                        }

                        sw.WriteLine("</tbody></table><br/>");
                    }
                }

                sw.WriteLine("</body>");
                sw.WriteLine("</html>");
            }
            finally
            {
                sw.Close();
            }
        }
    }
}

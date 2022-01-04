using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Resource = CharaSimResource.Resource;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSimControl
{
    public class SkillTooltipRender2 : TooltipRender
    {
        public SkillTooltipRender2()
        {
        }

        public Skill Skill { get; set; }

        public override object TargetItem
        {
            get { return this.Skill; }
            set { this.Skill = value as Skill; }
        }

        public bool ShowProperties { get; set; } = true;
        public bool ShowDelay { get; set; }
        public bool ShowReqSkill { get; set; } = true;
        public bool DisplayCooltimeMSAsSec { get; set; } = true;
        public bool DisplayPermyriadAsPercent { get; set; } = true;
        public bool IsWideMode { get; set; } = true;

        public override Bitmap Render()
        {
            if (this.Skill == null)
            {
                return null;
            }

            CanvasRegion region = this.IsWideMode ? CanvasRegion.Wide : CanvasRegion.Original;

            int picHeight;
            Bitmap originBmp = RenderSkill(region, out picHeight);
            Bitmap tooltip = new Bitmap(originBmp.Width, picHeight);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制背景区域
            GearGraphics.DrawNewTooltipBack(g, 0, 0, tooltip.Width, tooltip.Height);

            //复制图像
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, originBmp.Width, picHeight), GraphicsUnit.Pixel);

            //左上角
            g.DrawImage(Resource.UIToolTip_img_Item_Frame2_cover, 3, 3);

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, Skill.SkillID.ToString("d7"), true);
            }

            if (originBmp != null)
                originBmp.Dispose();

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderSkill(CanvasRegion region, out int picH)
        {
            Bitmap bitmap = new Bitmap(region.Width, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            picH = 0;

            //获取文字
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringSkill.TryGetValue(Skill.SkillID, out sr))
            {
                sr = new StringResultSkill();
                sr.Name = "(null)";
            }

            //绘制技能名称
            format.Alignment = StringAlignment.Center;
            g.DrawString(sr.Name, GearGraphics.ItemNameFont2, Brushes.White, region.TitleCenterX, 10, format);

            //绘制图标
            if (Skill.Icon.Bitmap != null)
            {
                picH = 33;
                g.FillRectangle(GearGraphics.GearIconBackBrush2, 14, picH, 68, 68);
                g.DrawImage(GearGraphics.EnlargeBitmap(Skill.Icon.Bitmap),
                14 + (1 - Skill.Icon.Origin.X) * 2,
                picH + (33 - Skill.Icon.Bitmap.Height) * 2);
            }

            //绘制desc
            picH = 35;
<<<<<<< HEAD
            if (Skill.HyperStat)
                GearGraphics.DrawString(g, "[最高等級: " + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, 10, 274, ref picH, 16);
            else if (!Skill.PreBBSkill)
                GearGraphics.DrawString(g, "[最高等級: " + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, 90, 274, ref picH, 16);
=======
            if (!Skill.PreBBSkill)
                GearGraphics.DrawString(g, "[最高等级：" + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, region.SkillDescLeft, region.TextRight, ref picH, 16);
>>>>>>> CMS/master

            if (sr.Desc != null)
            {
                string hdesc = SummaryParser.GetSkillSummary(sr.Desc, Skill.Level, Skill.Common, SummaryParams.Default);
                //string hStr = SummaryParser.GetSkillSummary(skill, skill.Level, sr, SummaryParams.Default);
<<<<<<< HEAD
                GearGraphics.DrawString(g, hdesc, GearGraphics.ItemDetailFont2, Skill.Icon.Bitmap == null ? 10 : 90, 272, ref picH, 16);
=======
                GearGraphics.DrawString(g, hdesc, GearGraphics.ItemDetailFont2, region.SkillDescLeft, region.TextRight, ref picH, 16);
>>>>>>> CMS/master
            }
            if (Skill.TimeLimited)
            {
<<<<<<< HEAD
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr = time.ToString("有效期間 : yyyy年 M月 d日 HH時 mm分");
                GearGraphics.DrawString(g, "#c" + expireStr + "#", GearGraphics.ItemDetailFont2, Skill.Icon.Bitmap == null ? 10 : 90, 272, ref picH, 16);
            }
            /*if (Skill.ReqLevel > 0)
            {
                GearGraphics.DrawString(g, "#c[所需等級：" + Skill.ReqLevel.ToString() + "]#", GearGraphics.ItemDetailFont2, 90, 272, ref picH, 16);
            }
            if (Skill.ReqAmount > 0)
            {
                GearGraphics.DrawString(g, "#c" + ItemStringHelper.GetSkillReqAmount(Skill.SkillID, Skill.ReqAmount) + "#", GearGraphics.ItemDetailFont2, 90, 270, ref picH, 16);
            }*/
=======
                GearGraphics.DrawString(g, "#c[要求等级：" + Skill.ReqLevel.ToString() + "]#", GearGraphics.ItemDetailFont2, region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.ReqAmount > 0)
            {
                GearGraphics.DrawString(g, "#c" + ItemStringHelper.GetSkillReqAmount(Skill.SkillID, Skill.ReqAmount) + "#", GearGraphics.ItemDetailFont2, region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
>>>>>>> CMS/master

            //分割线
            picH = Math.Max(picH, 114);
            g.DrawLine(Pens.White, region.SplitterX1, picH, region.SplitterX2, picH);
            picH += 9;

            if (Skill.Level > 0)
            {
                string hStr = SummaryParser.GetSkillSummary(Skill, Skill.Level, sr, SummaryParams.Default, new SkillSummaryOptions
                {
                    ConvertCooltimeMS = this.DisplayCooltimeMSAsSec,
                    ConvertPerM = this.DisplayPermyriadAsPercent
                });
<<<<<<< HEAD
                GearGraphics.DrawString(g, "[現在等級 " + Skill.Level + "]", GearGraphics.ItemDetailFont, 8, 272, ref picH, 16);
                if (hStr != null)
                {
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, 8, 272, ref picH, 18);
=======
                GearGraphics.DrawString(g, "[现在等级 " + Skill.Level + "]", GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                if (hStr != null)
                {
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, region.LevelDescLeft, region.TextRight, ref picH, 16);
>>>>>>> CMS/master
                }
            }

            if (Skill.Level < Skill.MaxLevel && !Skill.DisableNextLevelInfo)
            {
                string hStr = SummaryParser.GetSkillSummary(Skill, Skill.Level + 1, sr, SummaryParams.Default, new SkillSummaryOptions
                {
                    ConvertCooltimeMS = this.DisplayCooltimeMSAsSec,
                    ConvertPerM = this.DisplayPermyriadAsPercent
                });
<<<<<<< HEAD
                GearGraphics.DrawString(g, "[下次等級 " + (Skill.Level + 1) + "]", GearGraphics.ItemDetailFont, 8, 272, ref picH, 16);
                if (hStr != null)
                {
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, 8, 272, ref picH, 18);
=======
                GearGraphics.DrawString(g, "[下次等级 " + (Skill.Level + 1) + "]", GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                if (hStr != null)
                {
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, region.LevelDescLeft, region.TextRight, ref picH, 16);
>>>>>>> CMS/master
                }
            }
            picH += 4;

            if (Skill.AddAttackToolTipDescSkill != 0)
            {
                g.DrawLine(Pens.White, 6, picH, 283, picH);
                picH += 8;
                GearGraphics.DrawString(g, "#$[組合技能]#", GearGraphics.ItemDetailFont, 8, 272, ref picH, 18);
                BitmapOrigin icon = new BitmapOrigin();
                Wz_Node skillNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\{0}.img\skill\{1}", Skill.AddAttackToolTipDescSkill / 10000, Skill.AddAttackToolTipDescSkill));
                if (skillNode != null)
                {
                    Skill skill = Skill.CreateFromNode(skillNode, PluginBase.PluginManager.FindWz);
                    icon = skill.Icon;
                }
                if (icon.Bitmap != null)
                {
                    g.DrawImage(icon.Bitmap, 10 - icon.Origin.X, picH + 32 - icon.Origin.Y);
                }
                string skillName;
                if (this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(Skill.AddAttackToolTipDescSkill, out sr))
                {
                    skillName = sr.Name;
                }
                else
                {
                    skillName = Skill.AddAttackToolTipDescSkill.ToString();
                }
                picH += 9;
                GearGraphics.DrawString(g, skillName, GearGraphics.ItemDetailFont, 47, 272, ref picH, 18);
                picH += 9;
            }

            if (Skill.AssistSkillLink != 0)
            {
                g.DrawLine(Pens.White, 6, picH, 283, picH);
                picH += 8;
                GearGraphics.DrawString(g, "#c[輔助技能]#", GearGraphics.ItemDetailFont, 8, 272, ref picH, 18);
                BitmapOrigin icon = new BitmapOrigin();
                Wz_Node skillNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\{0}.img\skill\{1}", Skill.AssistSkillLink / 10000, Skill.AssistSkillLink));
                if (skillNode != null)
                {
                    Skill skill = Skill.CreateFromNode(skillNode, PluginBase.PluginManager.FindWz);
                    icon = skill.Icon;
                }
                if (icon.Bitmap != null)
                {
                    g.DrawImage(icon.Bitmap, 10 - icon.Origin.X, picH + 32 - icon.Origin.Y);
                }
                string skillName;
                if (this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(Skill.AssistSkillLink, out sr))
                {
                    skillName = sr.Name;
                }
                else
                {
                    skillName = Skill.AssistSkillLink.ToString();
                }
                picH += 9;
                GearGraphics.DrawString(g, skillName, GearGraphics.ItemDetailFont, 47, 272, ref picH, 18);
                picH += 9;
            }

            List<string> skillDescEx = new List<string>();
            if (ShowProperties)
            {
                List<string> attr = new List<string>();
                if (Skill.ReqLevel > 0)
                {
                    attr.Add("要求等級: " + Skill.ReqLevel);
                }
                if (Skill.Invisible)
                {
                    attr.Add("隱藏技能");
                }
                if (Skill.Hyper != HyperSkillType.None)
                {
                    attr.Add("超級技能:" + Skill.Hyper);
                }
                if (Skill.CombatOrders)
                {
                    attr.Add("戰鬥命令加成");
                }
                if (Skill.NotRemoved)
                {
                    attr.Add("無法被移除");
                }
                if (Skill.MasterLevel > 0 && Skill.MasterLevel < Skill.MaxLevel)
                {
                    attr.Add("初始掌握:Lv." + Skill.MasterLevel);
                }

                if (attr.Count > 0)
                {
                    skillDescEx.Add("#c" + string.Join(", ", attr.ToArray()) + "#");
                }
            }

            if (ShowDelay && Skill.Action.Count > 0)
            {
                foreach (string action in Skill.Action)
                {
                    skillDescEx.Add("#c[技能延遲] " + action + ": " + CharaSimLoader.GetActionDelay(action) + " ms#");
                }
            }

            if (ShowReqSkill && Skill.ReqSkill.Count > 0)
            {
                foreach (var kv in Skill.ReqSkill)
                {
                    string skillName;
                    if (this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr))
                    {
                        skillName = sr.Name;
                    }
                    else
                    {
                        skillName = kv.Key.ToString();
                    }
                    skillDescEx.Add("#c[前置技能] " + skillName + ": " + kv.Value + " 級#");
                }
            }

            if (skillDescEx.Count > 0)
            {
                g.DrawLine(Pens.White, region.SplitterX1, picH, region.SplitterX2, picH);
                picH += 9;
                foreach (var descEx in skillDescEx)
                {
<<<<<<< HEAD
                    GearGraphics.DrawString(g, descEx, GearGraphics.ItemDetailFont, 8, 272, ref picH, 18);
=======
                    GearGraphics.DrawString(g, descEx, GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
>>>>>>> CMS/master
                }
                picH += 6;
            }

            picH += 4;

            format.Dispose();
            g.Dispose();
            return bitmap;
        }

        private class CanvasRegion
        {
            public int Width { get; private set; }
            public int TitleCenterX { get; private set; }
            public int SplitterX1 { get; private set; }
            public int SplitterX2 { get; private set; }
            public int SkillDescLeft { get; private set; }
            public int LevelDescLeft { get; private set; }
            public int TextRight { get; private set; }

            public static CanvasRegion Original { get; } = new CanvasRegion()
            {
                Width = 290,
                TitleCenterX = 144,
                SplitterX1 = 6,
                SplitterX2 = 283,
                SkillDescLeft = 90,
                LevelDescLeft = 8,
                TextRight = 272,
            };

            public static CanvasRegion Wide { get; } = new CanvasRegion()
            {
                Width = 430,
                TitleCenterX = 215,
                SplitterX1 = 6,
                SplitterX2 = 423,
                SkillDescLeft = 92,
                LevelDescLeft = 10,
                TextRight = 412,
            };
        }
    }
}

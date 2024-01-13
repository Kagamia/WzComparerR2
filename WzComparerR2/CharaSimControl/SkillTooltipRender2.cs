using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Resource = CharaSimResource.Resource;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;

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
        public bool IgnoreEvalError { get; set; } = false;
        public bool IsWideMode { get; set; } = true;

        public override Bitmap Render()
        {
            if (this.Skill == null)
            {
                return null;
            }

            CanvasRegion region = this.IsWideMode ? CanvasRegion.Wide : CanvasRegion.Original;

            int picHeight;
            List<int> splitterH;
            Bitmap originBmp = RenderSkill(region, out picHeight, out splitterH);
            Bitmap tooltip = new Bitmap(originBmp.Width, picHeight);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制背景区域
            GearGraphics.DrawNewTooltipBack(g, 0, 0, tooltip.Width, tooltip.Height);
            if (splitterH != null && splitterH.Count > 0)
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                foreach (var y in splitterH)
                {
                    DrawV6SkillDotline(g, region.SplitterX1, region.SplitterX2, y);
                }
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            }

            //复制图像
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, originBmp.Width, picHeight), GraphicsUnit.Pixel);

            //左上角
            g.DrawImage(Resource.UIToolTip_img_Skill_Frame_cover, 3, 3);

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, Skill.SkillID.ToString("d7"), true);
            }

            if (originBmp != null)
                originBmp.Dispose();

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderSkill(CanvasRegion region, out int picH, out List<int> splitterH)
        {
            Bitmap bitmap = new Bitmap(region.Width, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            var v6SkillSummaryFontColorTable = new Dictionary<string, Color>()
            {
                { "c", GearGraphics.SkillSummaryOrangeTextColor },
            };

            picH = 0;
            splitterH = new List<int>();

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
            picH = 33;
            g.DrawImage(Resource.UIToolTip_img_Skill_Frame_iconBackgrnd, 13, picH - 2);

            if (Skill.Icon.Bitmap != null)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(Skill.Icon.Bitmap),
                15 + (1 - Skill.Icon.Origin.X) * 2,
                picH + (33 - Skill.Icon.Bitmap.Height) * 2);
            }

            // for 6th job skills
            if (Skill.Origin)
            {
                g.DrawImage(Resource.UIWindow2_img_Skill_skillTypeIcon_origin, 16, 11);
            }

            //绘制desc
            picH = 35;
            if (!Skill.PreBBSkill)
                GearGraphics.DrawString(g, "[最高等级：" + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, region.SkillDescLeft, region.TextRight, ref picH, 16);

            if (sr.Desc != null)
            {
                string hdesc = SummaryParser.GetSkillSummary(sr.Desc, Skill.Level, Skill.Common, SummaryParams.Default);
                //string hStr = SummaryParser.GetSkillSummary(skill, skill.Level, sr, SummaryParams.Default);
                GearGraphics.DrawString(g, hdesc, GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.ReqLevel > 0)
            {
                GearGraphics.DrawString(g, "#c[要求等级：" + Skill.ReqLevel.ToString() + "]#", GearGraphics.ItemDetailFont2, region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.ReqAmount > 0)
            {
                GearGraphics.DrawString(g, "#c" + ItemStringHelper.GetSkillReqAmount(Skill.SkillID, Skill.ReqAmount) + "#", GearGraphics.ItemDetailFont2, region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            picH += 13;

            //delay rendering v6 splitter
            picH = Math.Max(picH, 114);
            splitterH.Add(picH);
            picH += 15;

            var skillSummaryOptions = new SkillSummaryOptions
            {
                ConvertCooltimeMS = this.DisplayCooltimeMSAsSec,
                ConvertPerM = this.DisplayPermyriadAsPercent,
                IgnoreEvalError = this.IgnoreEvalError,
                EndColorOnNewLine = true,
            };

            if (Skill.Level > 0)
            {
                string hStr = SummaryParser.GetSkillSummary(Skill, Skill.Level, sr, SummaryParams.Default, skillSummaryOptions);
                GearGraphics.DrawString(g, "[现在等级 " + Skill.Level + "]", GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                if (hStr != null)
                {
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
            }

            if (Skill.Level < Skill.MaxLevel)
            {
                string hStr = SummaryParser.GetSkillSummary(Skill, Skill.Level + 1, sr, SummaryParams.Default, skillSummaryOptions);
                GearGraphics.DrawString(g, "[下次等级 " + (Skill.Level + 1) + "]", GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                if (hStr != null)
                {
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
            }
            picH += 9;

            List<string> skillDescEx = new List<string>();
            if (ShowProperties)
            {
                List<string> attr = new List<string>();
                if (Skill.Invisible)
                {
                    attr.Add("隐藏技能");
                }
                if (Skill.Hyper != HyperSkillType.None)
                {
                    attr.Add("超级技能:" + Skill.Hyper);
                }
                if (Skill.CombatOrders)
                {
                    attr.Add("战斗命令加成");
                }
                if (Skill.NotRemoved)
                {
                    attr.Add("无法被移除");
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
                    skillDescEx.Add("#c[技能延时] " + action + ": " + CharaSimLoader.GetActionDelay(action) + " ms#");
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
                    skillDescEx.Add("#c[前置技能] " + skillName + ": " + kv.Value + " 级#");
                }
            }

            if (skillDescEx.Count > 0)
            {
                //delay rendering v6 splitter
                splitterH.Add(picH);
                picH += 9;
                foreach (var descEx in skillDescEx)
                {
                    GearGraphics.DrawString(g, descEx, GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
                picH += 9;
            }

            format.Dispose();
            g.Dispose();
            return bitmap;
        }

        private void DrawV6SkillDotline(Graphics g, int x1, int x2, int y)
        {
            // here's a trick that we won't draw left and right part because it looks the same as background border.
            var picCenter = Resource.UIToolTip_img_Skill_Frame_dotline_c;
            using (var brush = new TextureBrush(picCenter))
            {
                brush.TranslateTransform(x1, y);
                g.FillRectangle(brush, new Rectangle(x1, y, x2 - x1, picCenter.Height));
            }
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
                SplitterX1 = 4,
                SplitterX2 = 284,
                SkillDescLeft = 90,
                LevelDescLeft = 8,
                TextRight = 272,
            };

            public static CanvasRegion Wide { get; } = new CanvasRegion()
            {
                Width = 430,
                TitleCenterX = 215,
                SplitterX1 = 4,
                SplitterX2 = 424,
                SkillDescLeft = 92,
                LevelDescLeft = 10,
                TextRight = 412,
            };
        }
    }
}

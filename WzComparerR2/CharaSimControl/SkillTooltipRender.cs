using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class SkillTooltipRender : TooltipRender
    {
        public SkillTooltipRender()
        {
        }

        Skill skill;

        public Skill Skill
        {
            get { return skill; }
            set { skill = value; }
        }

        public override Bitmap Render()
        {
            if (this.skill == null)
            {
                return null;
            }

            int picHeight;
            Bitmap originBmp = RenderSkill(out picHeight);
            Bitmap tooltip = new Bitmap(290, picHeight);
            Graphics g = Graphics.FromImage(tooltip);

            int iconY = 33;
            //复制图像
            g.FillRectangle(GearGraphics.GearBackBrush, 2, 2, 286, picHeight - 4);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.FillRectangle(GearGraphics.GearIconBackBrush, 14, iconY, 68, 68);
            g.CompositingMode = CompositingMode.SourceOver;
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, 290, picHeight - 2), GraphicsUnit.Pixel);

            //边框
            g.DrawLines(GearGraphics.GearBackPen, GearGraphics.GetBorderPath(0, 290, picHeight));

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderSkill(out int picH)
        {
            //int h = 128;
            Bitmap bitmap = new Bitmap(290, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);

            picH = 33; //iconY

            StringResult sr;
            if (!StringLinker.StringSkill.TryGetValue(skill.SkillID, out sr))
            {
                sr = new StringResultSkill();
                sr.Name = "(null)";
            }

            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;

            g.DrawString(sr.Name, GearGraphics.ItemNameFont, Brushes.White, 143, 10, format);//绘制标题
            if (skill.Icon.Bitmap != null)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(skill.Icon.Bitmap),
                14 + (1 - skill.Icon.Origin.X) * 2,
                picH + (33 - skill.Icon.Bitmap.Height) * 2);//绘制图标
            }

            //绘制desc
            picH = 35;
            GearGraphics.DrawString(g, "[最高等级：" + skill.MaxLevel + "]", GearGraphics.ItemDetailFont, 90, 270, ref picH, 16);
            if (sr.Desc != null)
            {
                GearGraphics.DrawString(g, sr.Desc, GearGraphics.ItemDetailFont, 90, 270, ref picH, 16);
            }

            picH = Math.Max(picH, 114);
            g.DrawLine(Pens.White, 6, picH, 283, picH); //分割线
            picH += 5;

            if (skill.Level > 0)
            {
                string hStr = null;
                if (skill.PreBBSkill)
                {
                    if (sr.SkillH.Count >= skill.Level)
                    {
                        hStr = sr.SkillH[skill.Level - 1];
                    }
                }
                else
                {
                    if (sr.SkillH.Count > 0)
                    {
                        hStr = SummaryParser.GetSkillSummary(skill,skill.Level, sr, SummaryParams.Default);
                    }
                }

                picH += 4;
                GearGraphics.DrawString(g, "[现在等级 " + skill.Level + "]", GearGraphics.ItemDetailFont, 8, 272, ref picH, 16);
                GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont, 8, 272, ref picH, 16);
            }

            if (skill.Level < skill.MaxLevel)
            {
                string hStr = null;
                if (skill.PreBBSkill)
                {
                    if (sr.SkillH.Count >= skill.Level + 1)
                    {
                        hStr = sr.SkillH[skill.Level];
                    }
                }
                else
                {
                    if (sr.SkillH.Count > 0)
                    {
                        hStr = SummaryParser.GetSkillSummary(skill, skill.Level+1, sr, SummaryParams.Default); 
                    }
                }

                picH += 4;
                GearGraphics.DrawString(g, "[下次等级 " + (skill.Level + 1) + "]", GearGraphics.ItemDetailFont, 8, 272, ref picH, 16);
                GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont, 8, 272, ref picH, 16);
            }
            picH += 9;
            g.Dispose();
            return bitmap;
        }
    }
}

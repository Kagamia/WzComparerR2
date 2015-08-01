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

        Skill skill;
        bool showDelay;
        bool showReqSkill = true;

        public Skill Skill
        {
            get { return skill; }
            set { skill = value; }
        }

        public override object TargetItem
        {
            get { return this.skill; }
            set { this.skill = value as Skill; }
        }

        public bool ShowDelay
        {
            get { return showDelay; }
            set { showDelay = value; }
        }

        public bool ShowReqSkill
        {
            get { return showReqSkill; }
            set { showReqSkill = value; }
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

            //绘制背景区域
            GearGraphics.DrawNewTooltipBack(g, 0, 0, tooltip.Width, tooltip.Height);

            //复制图像
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, 290, picHeight), GraphicsUnit.Pixel);

            //左上角
            g.DrawImage(Resource.UIToolTip_img_Item_Frame2_cover, 3, 3);

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, skill.SkillID.ToString("d7"), true);
            }

            if (originBmp != null)
                originBmp.Dispose();

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderSkill(out int picH)
        {
            Bitmap bitmap = new Bitmap(290, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            picH = 0;

            //获取文字
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringSkill.TryGetValue(skill.SkillID, out sr))
            {
                sr = new StringResult(true);
                sr.Name = "(null)";
            }

            //绘制技能名称
            format.Alignment = StringAlignment.Center;
            g.DrawString(sr.Name, GearGraphics.ItemNameFont2, Brushes.White, 144, 10, format);

            //绘制图标
            picH = 33;
            g.FillRectangle(GearGraphics.GearIconBackBrush2, 14, picH, 68, 68);
            if (skill.Icon.Bitmap != null)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(skill.Icon.Bitmap),
                14 + (1 - skill.Icon.Origin.X) * 2,
                picH + (33 - skill.Icon.Bitmap.Height) * 2);
            }

            //绘制desc
            picH = 35;
            if (!skill.PreBBSkill)
                GearGraphics.DrawString(g, "[最高等级：" + skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, 92, 272, ref picH, 16);

            if (sr.Desc != null)
            {
                GearGraphics.DrawString(g, sr.Desc, GearGraphics.ItemDetailFont2, 92, 272, ref picH, 16);
            }
            if (skill.ReqLevel > 0)
            {
                GearGraphics.DrawString(g, "#c[要求等级：" + skill.ReqLevel.ToString() + "]#", GearGraphics.ItemDetailFont2, 92, 272, ref picH, 16);
            }
            if (skill.ReqAmount > 0)
            {
                GearGraphics.DrawString(g, "#c" + ItemStringHelper.GetSkillReqAmount(skill.SkillID, skill.ReqAmount) + "#", GearGraphics.ItemDetailFont2, 92, 272, ref picH, 16);
            }

            //分割线
            picH = Math.Max(picH, 114);
            g.DrawLine(Pens.White, 6, picH, 283, picH);
            picH += 9;

            if (skill.Level > 0)
            {
                string hStr = SummaryParser.GetSkillSummary(skill, skill.Level, sr, SummaryParams.Default);
                GearGraphics.DrawString(g, "[现在等级 " + skill.Level + "]", GearGraphics.ItemDetailFont, 10, 274, ref picH, 16);
                if (hStr != null)
                {
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, 10, 274, ref picH, 16);
                }
            }

            if (skill.Level < skill.MaxLevel)
            {
                string hStr = SummaryParser.GetSkillSummary(skill, skill.Level + 1, sr, SummaryParams.Default);
                GearGraphics.DrawString(g, "[下次等级 " + (skill.Level + 1) + "]", GearGraphics.ItemDetailFont, 10, 274, ref picH, 16);
                if (hStr != null)
                {
                    GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, 10, 274, ref picH, 16);
                }
            }
            picH += 9;

            List<string> skillDescEx = new List<string>();

            {
                List<string> attr = new List<string>();
                if (skill.Invisible)
                {
                    attr.Add("隐藏技能");
                }
                if (skill.Hyper != HyperSkillType.None)
                {
                    attr.Add("超级技能:" + skill.Hyper);
                }
                if (skill.CombatOrders)
                {
                    attr.Add("战斗命令加成");
                } 
                if (skill.NotRemoved)
                {
                    attr.Add("无法被移除");
                }
                if (skill.MasterLevel > 0 && skill.MasterLevel < skill.MaxLevel)
                {
                    attr.Add("初始掌握:Lv." + skill.MasterLevel);
                }

                if (attr.Count > 0)
                {
                    skillDescEx.Add("#c" + string.Join(", ", attr.ToArray()) + "#");
                }
            }

            if (showDelay && skill.Action.Count > 0)
            {
                foreach (string action in skill.Action)
                {
                    skillDescEx.Add("#c[技能延时] " + action + ": " + CharaSimLoader.GetActionDelay(action) + " ms#");
                }
            }

            if (showReqSkill && skill.ReqSkill.Count > 0)
            {
                foreach (var kv in skill.ReqSkill)
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
                g.DrawLine(Pens.White, 6, picH, 283, picH);
                picH += 9;
                foreach (var descEx in skillDescEx)
                {
                    GearGraphics.DrawString(g, descEx, GearGraphics.ItemDetailFont, 8, 266, ref picH, 16);
                }
                picH += 9;
            }

            format.Dispose();
            g.Dispose();
            return bitmap;
        }
    }
}

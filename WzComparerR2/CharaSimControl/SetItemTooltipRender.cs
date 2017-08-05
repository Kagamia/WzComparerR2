using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using Resource = CharaSimResource.Resource;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using System.Text.RegularExpressions;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class SetItemTooltipRender : TooltipRender
    {
        public SetItemTooltipRender()
        {
        }

        public SetItem SetItem { get; set; }

        public override object TargetItem
        {
            get { return this.SetItem; }
            set { this.SetItem = value as SetItem; }
        }

        public bool IsCombineProperties { get; set; } = true;

        public override Bitmap Render()
        {
            if (this.SetItem == null)
            {
                return null;
            }

            int width = 261;
            int picHeight1;
            Bitmap originBmp = RenderSetItem(out picHeight1);
            int picHeight2 = 0;
            Bitmap effectBmp = null;

            if (this.SetItem.ExpandToolTip)
            {
                effectBmp = RenderEffectPart(out picHeight2);
                width += 261;
            }

            Bitmap tooltip = new Bitmap(width, Math.Max(picHeight1, picHeight2));
            Graphics g = Graphics.FromImage(tooltip);

            //绘制左侧
            GearGraphics.DrawNewTooltipBack(g, 0, 0, originBmp.Width, picHeight1);
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, originBmp.Width, picHeight1), GraphicsUnit.Pixel);
            
            //绘制右侧
            if(effectBmp != null)
            {
                GearGraphics.DrawNewTooltipBack(g, originBmp.Width, 0, effectBmp.Width, picHeight2);
                g.DrawImage(effectBmp, originBmp.Width, 0, new Rectangle(0, 0, effectBmp.Width, picHeight2), GraphicsUnit.Pixel);
            }

            originBmp?.Dispose();
            effectBmp?.Dispose();
            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderSetItem(out int picHeight)
        {
            Bitmap setBitmap = new Bitmap(261, DefaultPicHeight);
            Graphics g = Graphics.FromImage(setBitmap);
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;

            picHeight = 10;
            g.DrawString(this.SetItem.setItemName, GearGraphics.ItemDetailFont2, GearGraphics.GreenBrush2, 130, 10, format);
            picHeight += 25;

            format.Alignment = StringAlignment.Far;

            foreach (var setItemPart in this.SetItem.itemIDs.Parts)
            {
                string itemName = setItemPart.Value.RepresentName;
                string typeName = setItemPart.Value.TypeName;

                if (string.IsNullOrEmpty(typeName) && SetItem.Parts)
                {
                    typeName = "装备";
                }

                if (string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(typeName))
                {
                    foreach (var itemID in setItemPart.Value.ItemIDs)
                    {
                        StringResult sr = null; ;
                        if (StringLinker != null)
                        {
                            if (StringLinker.StringEqp.TryGetValue(itemID.Key, out sr))
                            {
                                itemName = sr.Name;
                                if (typeName == null)
                                {
                                    typeName = ItemStringHelper.GetSetItemGearTypeString(Gear.GetGearType(itemID.Key));
                                }
                            }
                            else if (StringLinker.StringItem.TryGetValue(itemID.Key, out sr)) //兼容宠物
                            {
                                itemName = sr.Name;
                                if (typeName == null)
                                {
                                    if (itemID.Key / 10000 == 500)
                                    {
                                        typeName = "宠物";
                                    }
                                    else
                                    {
                                        typeName = "";
                                    }
                                }
                            }
                        }
                        if (sr == null)
                        {
                            itemName = "(null)";
                        }

                        break;
                    }
                }

                itemName = itemName ?? string.Empty;
                typeName = typeName ?? "装备";

                if (!Regex.IsMatch(typeName, @"^(\(.*\)|（.*）)$"))
                {
                    typeName = "(" + typeName + ")";
                }

                Brush brush = setItemPart.Value.Enabled ? Brushes.White : GearGraphics.GrayBrush2;
                g.DrawString(itemName, GearGraphics.ItemDetailFont2, brush, 8, picHeight);
                g.DrawString(typeName, GearGraphics.ItemDetailFont2, brush, 254, picHeight, format);
                picHeight += 18;
            }

            if (!this.SetItem.ExpandToolTip)
            {
                picHeight += 5;
                g.DrawLine(Pens.White, 6, picHeight, 254, picHeight);//分割线
                picHeight += 9;
                RenderEffect(g, ref picHeight);
            }
            picHeight += 11;

            format.Dispose();
            g.Dispose();
            return setBitmap;
        }

        private Bitmap RenderEffectPart(out int picHeight)
        {
            Bitmap effBitmap = new Bitmap(261, DefaultPicHeight);
            Graphics g = Graphics.FromImage(effBitmap);
            picHeight = 9;
            RenderEffect(g, ref picHeight);
            picHeight += 11;
            g.Dispose();
            return effBitmap;
        }

        /// <summary>
        /// 绘制套装属性。
        /// </summary>
        private void RenderEffect(Graphics g, ref int picHeight)
        {
            foreach (KeyValuePair<int, SetItemEffect> effect in this.SetItem.effects)
            {
                g.DrawString(effect.Key + "套装效果", GearGraphics.ItemDetailFont, GearGraphics.GreenBrush2, 8, picHeight);
                picHeight += 16;
                //Brush brush = effect.Value.Enabled ? Brushes.White : GearGraphics.GrayBrush2;
                var color = effect.Value.Enabled ? Color.White : GearGraphics.GrayColor2;

                //T116 合并套装
                var props = IsCombineProperties ? Gear.CombineProperties(effect.Value.PropsV5) : effect.Value.PropsV5;
                foreach (KeyValuePair<GearPropType, object> prop in props)
                {
                    if (prop.Key == GearPropType.Option)
                    {
                        List<Potential> ops = (List<Potential>)prop.Value;
                        foreach (Potential p in ops)
                        {
                            GearGraphics.DrawPlainText(g, p.ConvertSummary(), GearGraphics.ItemDetailFont2, color, 10, 244, ref picHeight, 16);
                        }
                    }
                    else if (prop.Key == GearPropType.OptionToMob)
                    {
                        List<SetItemOptionToMob> ops = (List<SetItemOptionToMob>)prop.Value;
                        foreach (SetItemOptionToMob p in ops)
                        {
                            GearGraphics.DrawPlainText(g, p.ConvertSummary(), GearGraphics.ItemDetailFont2, color, 10, 244, ref picHeight, 16);
                        }
                    }
                    else if (prop.Key == GearPropType.activeSkill)
                    {
                        List<SetItemActiveSkill> ops = (List<SetItemActiveSkill>)prop.Value;
                        foreach (SetItemActiveSkill p in ops)
                        {
                            StringResult sr;
                            if (StringLinker == null || !StringLinker.StringSkill.TryGetValue(p.SkillID, out sr))
                            {
                                sr = new StringResult();
                                sr.Name = p.SkillID.ToString();
                            }
                            string summary = "激活技能<" + sr.Name + "> Lv." + p.Level;
                            GearGraphics.DrawPlainText(g, summary, GearGraphics.ItemDetailFont2, color, 10, 244, ref picHeight, 16);
                        }
                    }
                    else
                    {
                        var summary = ItemStringHelper.GetGearPropString(prop.Key, Convert.ToInt32(prop.Value));
                        GearGraphics.DrawPlainText(g, summary, GearGraphics.ItemDetailFont2, color, 10, 244, ref picHeight, 16);
                    }
                }
            }
        }
    }
}

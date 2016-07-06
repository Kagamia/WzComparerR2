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

            int picHeight;
            Bitmap originBmp = RenderSetItem(out picHeight);
            Bitmap tooltip = new Bitmap(261, picHeight);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制背景区域
            GearGraphics.DrawNewTooltipBack(g, 0, 0, tooltip.Width, tooltip.Height);

            //复制图像
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, tooltip.Width, picHeight), GraphicsUnit.Pixel);

            if (originBmp != null)
                originBmp.Dispose();
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
                                typeName = ItemStringHelper.GetSetItemGearTypeString(Gear.GetGearType(itemID.Key));
                            }
                            else if (StringLinker.StringItem.TryGetValue(itemID.Key, out sr)) //兼容宠物
                            {
                                itemName = sr.Name;
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

            picHeight += 5;
            g.DrawLine(Pens.White, 6, picHeight, 254, picHeight);//分割线
            picHeight += 9;
            foreach (KeyValuePair<int, SetItemEffect> effect in this.SetItem.effects)
            {
                g.DrawString(effect.Key + "套装效果", GearGraphics.ItemDetailFont, GearGraphics.GreenBrush2, 8, picHeight);
                picHeight += 16;
                Brush brush = effect.Value.Enabled ? Brushes.White : GearGraphics.GrayBrush2;

                //T116 合并套装
                var props = IsCombineProperties ? CombineProperties(effect.Value.Props) : effect.Value.Props;
                foreach (KeyValuePair<GearPropType, object> prop in props)
                {
                    if (prop.Key == GearPropType.Option)
                    {
                        List<Potential> ops = (List<Potential>)prop.Value;
                        foreach (Potential p in ops)
                        {
                            g.DrawString(p.ConvertSummary(), GearGraphics.ItemDetailFont2, brush, 8, picHeight);
                            picHeight += 16;
                        }
                    }
                    else if (prop.Key == GearPropType.OptionToMob)
                    {
                        List<SetItemOptionToMob> ops = (List<SetItemOptionToMob>)prop.Value;
                        foreach (SetItemOptionToMob p in ops)
                        {
                            g.DrawString(p.ConvertSummary(), GearGraphics.ItemDetailFont2, brush, 8, picHeight);
                            picHeight += 16;
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
                            g.DrawString(summary, GearGraphics.ItemDetailFont2, brush, 8, picHeight);
                            picHeight += 16;
                        }
                    }
                    else
                    {
                        g.DrawString(ItemStringHelper.GetGearPropString(prop.Key, Convert.ToInt32(prop.Value)),
                            GearGraphics.SetItemPropFont, brush, 8, picHeight);
                        picHeight += 16;
                    }
                }
            }
            picHeight += 11;
            format.Dispose();
            g.Dispose();
            return setBitmap;
        }

        private SortedDictionary<GearPropType, object> CombineProperties(SortedDictionary<GearPropType, object> props)
        {
            var combinedProps = new SortedDictionary<GearPropType, object>();
            object obj;
            foreach (var prop in props)
            {
                switch (prop.Key)
                {
                    case GearPropType.incMHP:
                    case GearPropType.incMMP:
                        if (combinedProps.ContainsKey(GearPropType.incMHP_incMMP))
                        {
                            break;
                        }
                        else if (props.TryGetValue(prop.Key == GearPropType.incMHP? GearPropType.incMMP : GearPropType.incMHP, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incMHP_incMMP, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incMHPr:
                    case GearPropType.incMMPr:
                        if (combinedProps.ContainsKey(GearPropType.incMHPr_incMMPr))
                        {
                            break;
                        }
                        else if (props.TryGetValue(prop.Key == GearPropType.incMHPr ? GearPropType.incMMPr : GearPropType.incMHPr, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incMHPr_incMMPr, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incPAD:
                    case GearPropType.incMAD:
                        if (combinedProps.ContainsKey(GearPropType.incPAD_incMAD))
                        {
                            break;
                        }
                        else if (props.TryGetValue(prop.Key == GearPropType.incPAD ? GearPropType.incMAD : GearPropType.incPAD, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incPAD_incMAD, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incPDD:
                    case GearPropType.incMDD:
                        if (combinedProps.ContainsKey(GearPropType.incPDD_incMDD))
                        {
                            break;
                        }
                        else if (props.TryGetValue(prop.Key == GearPropType.incPDD ? GearPropType.incMDD : GearPropType.incPDD, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incPDD_incMDD, prop.Value);
                            break;
                        }
                        goto default;

                    case GearPropType.incACC:
                    case GearPropType.incEVA:
                        if (combinedProps.ContainsKey(GearPropType.incACC_incEVA))
                        {
                            break;
                        }
                        else if (props.TryGetValue(prop.Key == GearPropType.incACC ? GearPropType.incEVA : GearPropType.incACC, out obj)
                            && object.Equals(prop.Value, obj))
                        {
                            combinedProps.Add(GearPropType.incACC_incEVA, prop.Value);
                            break;
                        }
                        goto default;

                    default:
                        combinedProps.Add(prop.Key, prop.Value);
                        break;
                }
            }
            return combinedProps;
        }
    }
}

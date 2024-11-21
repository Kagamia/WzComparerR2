using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
            g.DrawString(this.SetItem.SetItemName, GearGraphics.ItemDetailFont2, GearGraphics.GreenBrush2, 130, 10, format);
            picHeight += 25;

            format.Alignment = StringAlignment.Far;
            Wz_Node characterWz = PluginManager.FindWz(Wz_Type.Character);

            foreach (var setItemPart in this.SetItem.ItemIDs.Parts)
            {
                string itemName = setItemPart.Value.RepresentName;
                string typeName = setItemPart.Value.TypeName;

                // for 'parts' setitem, detect typeName by the first itemID per part
                //if (string.IsNullOrEmpty(typeName) && SetItem.Parts)
                //{
                //    typeName = "特殊";
                //}

                ItemBase itemBase = null;
                bool cash = false;
                long wonderGrade = 0;

                if (setItemPart.Value.ItemIDs.Count > 0)
                {
                    var itemID = setItemPart.Value.ItemIDs.First().Key;

                    switch (itemID / 1000000)
                    {
                        case 0: //avatar
                        case 1: //gear
                            if (characterWz != null)
                            {
                                foreach (Wz_Node typeNode in characterWz.Nodes)
                                {
                                    Wz_Node itemNode = typeNode.FindNodeByPath(string.Format("{0:D8}.img", itemID), true);
                                    if (itemNode != null)
                                    {
                                        var gear = Gear.CreateFromNode(itemNode, PluginManager.FindWz);
                                        cash = gear.Cash;
                                        itemBase = gear;
                                        break;
                                    }
                                }
                            }
                            break;

                        case 5: //Pet
                            {
                                Wz_Node itemNode = PluginBase.PluginManager.FindWz(string.Format(@"Item\Pet\{0:D7}.img", itemID));
                                if (itemNode != null)
                                {
                                    var item = Item.CreateFromNode(itemNode, PluginManager.FindWz);
                                    cash = item.Cash;
                                    item.Props.TryGetValue(ItemPropType.wonderGrade, out wonderGrade);
                                    itemBase = item;
                                }
                            }
                            break;
                    }
                }

                if (string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(typeName))
                {
                    if (setItemPart.Value.ItemIDs.Count > 0)
                    {
                        var itemID = setItemPart.Value.ItemIDs.First().Key;
                        StringResult sr = null; ;
                        if (this.StringLinker != null)
                        {
                            if (this.StringLinker.StringEqp.TryGetValue(itemID, out sr))
                            {
                                itemName = sr.Name;
                                if (typeName == null)
                                {
                                    typeName = ItemStringHelper.GetSetItemGearTypeString(Gear.GetGearType(itemID));
                                }
                            }
                            else if (this.StringLinker.StringItem.TryGetValue(itemID, out sr)) //兼容宠物
                            {
                                itemName = sr.Name;
                                if (typeName == null)
                                {
                                    if (itemID / 10000 == 500)
                                    {
                                        typeName = "特殊";
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
                    }
                }

                itemName = itemName ?? string.Empty;
                typeName = typeName ?? "装备";

                if (!Regex.IsMatch(typeName, @"^(\(.*\)|（.*）)$"))
                {
                    typeName = "(" + typeName + ")";
                }

                Brush brush = setItemPart.Value.Enabled ? Brushes.White : GearGraphics.GrayBrush2;
                if (!cash)
                {
                    g.DrawString(itemName, GearGraphics.ItemDetailFont2, brush, 8, picHeight);
                    g.DrawString(typeName, GearGraphics.ItemDetailFont2, brush, 254, picHeight, format);
                    picHeight += 18;
                }
                else
                {
                    g.FillRectangle(GearGraphics.GearIconBackBrush2, 10, picHeight, 36, 36);
                    g.DrawImage(Resource.Item_shadow, 10 + 2 + 3, picHeight + 2 + 32 - 6);
                    if (itemBase?.IconRaw.Bitmap != null)
                    {
                        var icon = itemBase.IconRaw;
                        g.DrawImage(icon.Bitmap, 10 + 2 - icon.Origin.X, picHeight + 2 + 32 - icon.Origin.Y);
                    }

                    Bitmap cashImg = null;
                    if (wonderGrade > 0)
                    {
                        string resKey = $"CashShop_img_CashItem_label_{wonderGrade + 3}";
                        cashImg = Resource.ResourceManager.GetObject(resKey) as Bitmap;
                    }
                    if (cashImg == null) //default cashImg
                    {
                        cashImg = Resource.CashItem_0;
                    }
                    g.DrawImage(cashImg, 10 + 2 + 20, picHeight + 2 + 32 - 12);
                    g.DrawString(itemName, GearGraphics.ItemDetailFont2, brush, 50, picHeight);
                    g.DrawString(typeName, GearGraphics.ItemDetailFont2, brush, 254, picHeight, format);
                    picHeight += 40;
                }
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
            foreach (KeyValuePair<int, SetItemEffect> effect in this.SetItem.Effects)
            {
                string effTitle;
                if (this.SetItem.SetItemID < 0)
                {
                    effTitle = $"服务器内重复装备效果({effect.Key} / {this.SetItem.CompleteCount})";
                }
                else
                {
                    effTitle = effect.Key + "套装效果";
                }
                g.DrawString(effTitle, GearGraphics.ItemDetailFont, GearGraphics.GreenBrush2, 8, picHeight);
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
                            string summary = $"可以使用<{sr.Name}>技能";
                            GearGraphics.DrawPlainText(g, summary, GearGraphics.ItemDetailFont2, color, 10, 244, ref picHeight, 16);
                        }
                    }
                    else if (prop.Key == GearPropType.bonusByTime)
                    {
                        var ops = (List<SetItemBonusByTime>)prop.Value;
                        foreach (SetItemBonusByTime p in ops)
                        {
                            GearGraphics.DrawPlainText(g, $"{p.TermStart}小时后", GearGraphics.ItemDetailFont2, color, 10, 244, ref picHeight, 16);
                            foreach (var bonusProp in p.Props)
                            {
                                var summary = ItemStringHelper.GetGearPropString(bonusProp.Key, Convert.ToInt32(bonusProp.Value));
                                GearGraphics.DrawPlainText(g, summary, GearGraphics.ItemDetailFont2, color, 10, 244, ref picHeight, 16);
                            }
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

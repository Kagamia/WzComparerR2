using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;
using CharaSimResource;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class GearTooltipRender : TooltipRender
    {
        public GearTooltipRender()
        {
        }

        private Gear gear;
        private CharacterStatus charStat;

        public Gear Gear
        {
            get { return gear; }
            set { gear = value; }
        }

        public CharacterStatus CharacterStatus
        {
            get { return charStat; }
            set { charStat = value; }
        }

        public override Bitmap Render()
        {
            if (this.gear == null)
            {
                return null;
            }
            int picHeight, iconY, picHeight2, picHeight3;
            Bitmap left = renderBase(out picHeight, out iconY);
            Bitmap add = renderAddition(out picHeight2);
            Bitmap set = renderSetItem(out picHeight3);

            //整合图像
            int width = 252;
            if (add != null) width += 252;
            if (set != null) width += 252;
            Bitmap tooltip = new Bitmap(width, Math.Max(Math.Max(picHeight, picHeight2), picHeight3));
            Graphics g = Graphics.FromImage(tooltip);
            bool epic = gear.Epic;

            width = 0;
            //绘制主图
            if (left != null)
            {
                g.FillRectangle(epic ? GearGraphics.EpicGearBackBrush : GearGraphics.GearBackBrush, 2, 2, 248, picHeight - 4);
                g.CompositingMode = CompositingMode.SourceCopy;
                g.FillRectangle(epic ? GearGraphics.EpicGearIconBackBrush : GearGraphics.GearIconBackBrush, 14, iconY, 68, 68);
                g.CompositingMode = CompositingMode.SourceOver;
                g.DrawImage(left, 0, 0, new Rectangle(0, 0, 252, picHeight - 2), GraphicsUnit.Pixel);
                //绘制外边框
                g.DrawLines(epic ? GearGraphics.EpicGearBackPen : GearGraphics.GearBackPen, GearGraphics.GetBorderPath(0, 252, picHeight));
                //绘制等级边框
                Pen pen = GearGraphics.GetGearItemBorderPen(gear.Grade);
                if (pen != null)
                {
                    g.DrawLines(pen, getRankBorderPath(picHeight));
                }
                width += 252;
            }

            //绘制addition
            if (add != null)
            {
                //底色和边框
                g.FillRectangle(epic ? GearGraphics.EpicGearBackBrush : GearGraphics.GearBackBrush, width + 2, 2, 248, picHeight - 4);
                g.DrawLines(epic ? GearGraphics.EpicGearBackPen : GearGraphics.GearBackPen, GearGraphics.GetBorderPath(width, 252, picHeight));
                //复制原图
                g.DrawImage(add, width, 0, new Rectangle(0, 0, 252, picHeight2), GraphicsUnit.Pixel);
                add.Dispose();
                width += 252;
            }

            //绘制setitem
            if (set != null)
            {
                //底色和边框
                g.FillRectangle(GearGraphics.GearBackBrush, width + 2, 2, 248, picHeight3 - 4);
                g.DrawLines(GearGraphics.GearBackPen, GearGraphics.GetBorderPath(width, 252, picHeight3));
                //复制原图
                g.DrawImage(set, width, 0, new Rectangle(0, 0, 252, picHeight3), GraphicsUnit.Pixel);
                set.Dispose();
                width += 252;
            }

               // GearGraphics.DrawGearDetailNumber(g, 2, 2, gear.ItemID.ToString("d8"), true);

            g.Dispose();

            return tooltip;
        }

        private Bitmap renderBase(out int picHeight, out int iconY)
        {
            //绘制左侧部分
            Bitmap leftPart = new Bitmap(252, DefaultPicHeight);
            Graphics g = Graphics.FromImage(leftPart);
            StringFormat format = new StringFormat();
            int value;

            picHeight = 10;
            if (gear.Star > 0) //绘制星星
            {
                if (gear.Star < 5)
                {
                    for (int i = 0; i < gear.Star; i++)
                    {
                        g.DrawImage(Resource.ToolTip_Equip_Star_Star, 126 - gear.Star * 13 / 2 + 13 * i, picHeight);
                    }
                    picHeight += 18;
                }
                else
                {
                    int star = gear.Star % 5, star2 = gear.Star / 5;
                    int dx = 126 - (13 * star + 26 * star2) / 2;
                    for (int i = 0; i < 1; i++, dx += 26)
                    {
                        g.DrawImage(Resource.ToolTip_Equip_Star_Star2, dx, picHeight);
                    }
                    for (int i = 0; i < star; i++, dx += 13)
                    {
                        g.DrawImage(Resource.ToolTip_Equip_Star_Star, dx, picHeight + 5);
                    }
                    for (int i = 1; i < star2; i++, dx += 26)
                    {
                        g.DrawImage(Resource.ToolTip_Equip_Star_Star2, dx, picHeight);
                    }
                    picHeight += 28;
                }
            }

            //装备标题
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringEqp.TryGetValue(gear.ItemID, out sr))
            {
                sr = new StringResult();
                sr.Name = "(null)";
            }
            string gearName = sr.Name;
            string nameAdd = gear.ScrollUp > 0 ? ("+" + gear.ScrollUp) : null;
            switch (Gear.GetGender(gear.ItemID))
            {
                case 0: nameAdd += "男"; break;
                case 1: nameAdd += "女"; break;
            }
            if (!string.IsNullOrEmpty(nameAdd))
            {
                gearName += " (" + nameAdd + ")";
            }
            format.Alignment = StringAlignment.Center;
            g.DrawString(gearName, GearGraphics.ItemNameFont,
                GearGraphics.GetGearNameBrush(gear.diff, gear.ScrollUp > 0), 124, picHeight, format);//绘制装备名称
            picHeight += 19;

            //装备rank
            string rankStr;
            if (gear.GetBooleanValue(GearPropType.specialGrade))
                rankStr = ItemStringHelper.GetGearGradeString(GearGrade.Special);
            else
                rankStr = ItemStringHelper.GetGearGradeString(gear.Grade);
            g.DrawString(rankStr, GearGraphics.ItemDetailFont, Brushes.White, 127, picHeight, format);
            picHeight += 21;

            //额外属性
            for (int i = 0; i < 2; i++)
            {
                string attrStr = GetGearAttributeString(i);
                if (!string.IsNullOrEmpty(attrStr))
                {
                    g.DrawString(attrStr, GearGraphics.ItemDetailFont, GearGraphics.GearNameBrushC, 126, picHeight, format);
                    picHeight += 19;
                }
            }

            //装备限时
            if (gear.TimeLimited)
            {
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr = time.ToString("到yyyy年 M月 d日 H时 m分可以用");
                g.DrawString(expireStr, GearGraphics.ItemDetailFont, Brushes.White, 126, picHeight, format);
                picHeight += 16;
            }

            picHeight += 1;
            iconY = picHeight + 1;
            bool epic = gear.Epic;

            //绘制图标
            if (gear.Icon.Bitmap != null)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(gear.Icon.Bitmap),
                    14 + (1 - gear.Icon.Origin.X) * 2,
                    iconY + (33 - gear.Icon.Origin.Y) * 2);
            }
            if (gear.Cash)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(Resource.CashItem_0),
                    14 + 68 - 26,
                    iconY + 68 - 26);
            }

            //绘制属性要求
            drawGearReq(g, ref picHeight);

            //绘制装备等级
            if (gear.Props.TryGetValue(GearPropType.level, out value))
            {
                g.DrawImage(Resource.ToolTip_Equip_GrowthEnabled_itemLEV, 96, picHeight);
                GearGraphics.DrawGearGrowthNumber(g, 160, picHeight + 4, (value == -1) ? "m" : value.ToString(), true);
                picHeight += 12;
                g.DrawImage(Resource.ToolTip_Equip_GrowthEnabled_itemEXP, 96, picHeight);
                GearGraphics.DrawGearGrowthNumber(g, 160, picHeight + 4, (value == -1) ? "m" : "0%", true);
            }
            else
            {
                g.DrawImage(Resource.ToolTip_Equip_GrowthDisabled_itemLEV, 96, picHeight);
                g.DrawImage(Resource.ToolTip_Equip_GrowthDisabled_none, 160, picHeight + 4 + 3);
                picHeight += 12;
                g.DrawImage(Resource.ToolTip_Equip_GrowthDisabled_itemEXP, 96, picHeight);
                g.DrawImage(Resource.ToolTip_Equip_GrowthDisabled_none, 160, picHeight + 4 + 3);
            }
            picHeight += 12;
            if (gear.Props.TryGetValue(GearPropType.durability, out value))
            {
                if (value > 100) value = 100;
                g.DrawImage(value > 0 ? Resource.ToolTip_Equip_Can_durability : Resource.ToolTip_Equip_Cannot_durability, 96, picHeight);
                GearGraphics.DrawGearDetailNumber(g, 173, picHeight, value.ToString() + "%", value > 0);
            }
            picHeight += 13;

            //绘制职业要求
            int reqJob;
            gear.Props.TryGetValue(GearPropType.reqJob, out reqJob);
            g.DrawString("新手", GearGraphics.ItemDetailFont, reqJob > 0 ? Brushes.Red : Brushes.White, 10, picHeight);
            if (reqJob == 0) reqJob = 0x1f;//0001 1111
            if (reqJob == -1) reqJob = 0; //0000 0000
            g.DrawString("战士", GearGraphics.ItemDetailFont, (reqJob & 1) == 0 ? Brushes.Red : Brushes.White, 46, picHeight);
            g.DrawString("魔法师", GearGraphics.ItemDetailFont, (reqJob & 2) == 0 ? Brushes.Red : Brushes.White, 82, picHeight);
            g.DrawString("弓箭手", GearGraphics.ItemDetailFont, (reqJob & 4) == 0 ? Brushes.Red : Brushes.White, 130, picHeight);
            g.DrawString("飞侠", GearGraphics.ItemDetailFont, (reqJob & 8) == 0 ? Brushes.Red : Brushes.White, 178, picHeight);
            g.DrawString("海盗", GearGraphics.ItemDetailFont, (reqJob & 16) == 0 ? Brushes.Red : Brushes.White, 214, picHeight);
            picHeight += 19;

            //额外职业要求
            string extraReq = ItemStringHelper.GetExtraJobReqString(gear.type) ??
                (gear.Props.TryGetValue(GearPropType.reqSpecJob, out value) ? ItemStringHelper.GetExtraJobReqString(value) : null);
            if (!string.IsNullOrEmpty(extraReq))
            {
                g.DrawString(extraReq, GearGraphics.ItemDetailFont, GearGraphics.GearNameBrushC, 124, picHeight, format);
                picHeight += 18;
            }

            //分割线1号
            g.DrawLine(Pens.White, 6, picHeight, 245, picHeight);
            picHeight += 9;

            bool hasPart2 = false;
            //绘制属性
            if (gear.Props.TryGetValue(GearPropType.superiorEqp, out value) && value > 0)
            {
                g.DrawString("极真", GearGraphics.ItemNameFont, GearGraphics.SetItemNameBrush, 126, picHeight, format);
                picHeight += 18;
            }
            if (gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0)
            {
                g.DrawString("突破极限", GearGraphics.ItemNameFont, GearGraphics.SetItemNameBrush, 126, picHeight, format);
                picHeight += 18;
            }

            bool isWeapon = Gear.IsLeftWeapon(gear.type) || Gear.IsDoubleHandWeapon(gear.type);
            string typeStr = ItemStringHelper.GetGearTypeString(gear.type);
            if (!string.IsNullOrEmpty(typeStr))
            {
                g.DrawString("·", GearGraphics.ItemDetailFont, Brushes.White, 8, picHeight);
                g.DrawString((isWeapon ? "武器" : "装备") + "分类 : " + typeStr,
                    GearGraphics.ItemDetailFont, Brushes.White, 20, picHeight);
                picHeight += 16;
                hasPart2 = true;
            }
            if (gear.Props.TryGetValue(GearPropType.attackSpeed, out value))
            {
                g.DrawString("·", GearGraphics.ItemDetailFont, Brushes.White, 8, picHeight);
                g.DrawString("攻击速度 : " + ItemStringHelper.GetAttackSpeedString(value),
                    GearGraphics.ItemDetailFont, Brushes.White, 20, picHeight);
                picHeight += 16;
                hasPart2 = true;
            }
            List<GearPropType> props = new List<GearPropType>();
            foreach (KeyValuePair<GearPropType, int> p in gear.Props)
            {
                if ((int)p.Key < 100 && p.Value != 0)
                    props.Add(p.Key);
            }
            props.Sort();
            foreach (GearPropType type in props)
            {
                g.DrawString("·", GearGraphics.ItemDetailFont, Brushes.White, 8, picHeight);
                g.DrawString(ItemStringHelper.GetGearPropString(type, gear.Props[type]), (epic && Gear.IsEpicPropType(type)) ? GearGraphics.EpicGearDetailFont : GearGraphics.ItemDetailFont, Brushes.White, 20, picHeight);
                picHeight += 16;
                hasPart2 = true;
            }
            bool hasTuc = gear.HasTuc && gear.Props.TryGetValue(GearPropType.tuc, out value);
            if (hasTuc)
            {
                g.DrawString("·可升级次数 : " + value + "回", GearGraphics.ItemDetailFont, Brushes.White, 8, picHeight);
                picHeight += 16;
                hasPart2 = true;
            }
            if (gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0)
            {
                g.DrawString(ItemStringHelper.GetGearPropString(GearPropType.limitBreak, value), GearGraphics.ItemDetailFont, GearGraphics.SetItemNameBrush, 8, picHeight);
                picHeight += 16;
                hasPart2 = true;
            }

            if (hasTuc && gear.Hammer > -1)
            {
                if (gear.Hammer == 2)
                {
                    g.DrawString("黄金锤提炼完成", GearGraphics.ItemDetailFont, Brushes.White, 8, picHeight);
                    picHeight += 16;
                }
                if (gear.Props.TryGetValue(GearPropType.superiorEqp, out value) && value > 0)
                {
                    g.DrawString(ItemStringHelper.GetGearPropString(GearPropType.superiorEqp, value), GearGraphics.ItemDetailFont, GearGraphics.SetItemNameBrush, 8, picHeight);
                    picHeight += 16;
                }
                if (gear.Star > 0)
                {
                    g.DrawString("·应用" + gear.Star + "星强化", GearGraphics.ItemDetailFont, GearGraphics.OrangeBrush, 8, picHeight);
                    picHeight += 16;
                }
                picHeight += 2;
                g.DrawString("金锤子已提高的强化次数", GearGraphics.ItemDetailFont, GearGraphics.GoldHammerBrush, 8, picHeight);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.DrawString(": " + gear.Hammer.ToString() + (gear.Hammer == 2 ? "(MAX)" : null), GearGraphics.TahomaFont, GearGraphics.GoldHammerBrush, 140, picHeight - 2);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                picHeight += 14;
                hasPart2 = true;
            }
               
            
            //分割线2号
            if (hasPart2)
            {
                g.DrawLine(Pens.White, 6, picHeight, 245, picHeight);
                picHeight += 9;
            }

            //绘制潜能
            int optionCount = 0;
            foreach (Potential potential in gear.Options)
            {
                if (potential != null)
                {
                    g.DrawString("·", GearGraphics.ItemDetailFont, Brushes.White, 8, picHeight);
                    g.DrawString(potential.ConvertSummary(), GearGraphics.ItemDetailFont, Brushes.White, 20, picHeight);
                    picHeight += 16;
                    optionCount++;
                }
            }
            if (optionCount>0){
                picHeight += 4 * optionCount;
            }
            else if (gear.CanPotential)
            {
                GearGraphics.DrawString(g, " #c潜能卷轴# 可增加 #cC级物品# 潜力，但需鉴定。\n #c放大镜# 可解除 #c未鉴定物品# 潜能的封印。",
                    GearGraphics.ItemDetailFont, 8, 236, ref picHeight, 16);
                picHeight += 4;
            }

            //绘制附加潜能
            int adOptionCount = 0;
            foreach (Potential potential in gear.AdditionalOptions)
            {
                if (potential != null)
                {
                    adOptionCount++;
                }
            }
            if (adOptionCount > 0)
            {
                //分割线3号
                picHeight -= 3;
                g.DrawLine(Pens.White, 6, picHeight, 245, picHeight);
                g.DrawImage(GetAdditionalOptionIcon(gear.AdditionGrade), 8, picHeight+1);
                g.DrawString("附加潜能", GearGraphics.ItemDetailFont, GearGraphics.SetItemNameBrush, 26, picHeight+2);
                picHeight += 24;

                foreach (Potential potential in gear.AdditionalOptions)
                {
                    if (potential != null)
                    {
                        g.DrawString("+", GearGraphics.ItemDetailFont, Brushes.White, 8, picHeight);
                        g.DrawString(potential.ConvertSummary(), GearGraphics.ItemDetailFont, Brushes.White, 20, picHeight);
                        picHeight += 18;
                        adOptionCount++;
                    }
                }
                picHeight += 5;
            }

            //绘制desc
            if (!string.IsNullOrEmpty(sr.Desc))
            {
                if (optionCount > 0) picHeight -= 2;
                picHeight -= 3;
                GearGraphics.DrawString(g, sr.Desc, GearGraphics.ItemDetailFont, 8, 236, ref picHeight, 16);
                picHeight += 5;
            }
            if (gear.Props.TryGetValue(GearPropType.tradeAvailable, out value) && value != 0)
            {
                g.DrawString(ItemStringHelper.GetGearPropString(GearPropType.tradeAvailable, value),
                    GearGraphics.ItemDetailFont,
                    GearGraphics.OrangeBrush,
                    14, picHeight - 5);
                picHeight += 16;
            }

            if (gear.Props.TryGetValue(GearPropType.accountShareTag, out value) && value != 0)
            {
                GearGraphics.DrawString(g, " #c" + ItemStringHelper.GetGearPropString(GearPropType.accountShareTag, 1) + "#",
                     GearGraphics.ItemDetailFont, 8, 236, ref picHeight, 16);
                picHeight += 16;
            }

            //绘制倾向
            if (gear.State == GearState.itemList)
            {
                string incline = null;
                GearPropType[] inclineTypes = new GearPropType[]{
                    GearPropType.charismaEXP,
                    GearPropType.senseEXP,
                    GearPropType.insightEXP,
                    GearPropType.willEXP,
                    GearPropType.craftEXP,
                    GearPropType.charmEXP };

                string[] inclineString = new string[]{
                    "领导力","感性","洞察力","意志","手技","魅力"};

                for (int i = 0; i < inclineTypes.Length; i++)
                {
                    if (gear.Props.TryGetValue(inclineTypes[i], out value) && value > 0)
                    {
                        incline += "，" + inclineString[i] + value;
                    }
                }

                if (!string.IsNullOrEmpty(incline))
                {
                    picHeight -= 5;
                    GearGraphics.DrawString(g, "\n #c装备时可以获得" + incline.Substring(1) + "的经验值，仅限1次。#",
                        GearGraphics.ItemDetailFont, 8, 236, ref picHeight, 16);
                    picHeight += 8;
                }
            }
            format.Dispose();
            g.Dispose();
            return leftPart;
        }

        private Bitmap renderAddition(out int picHeight)
        {
            Bitmap addBitmap = null;
            picHeight = 0;
            if (gear.Additions.Count > 0)
            {
                addBitmap = new Bitmap(252, DefaultPicHeight);
                Graphics g = Graphics.FromImage(addBitmap);
                StringBuilder sb = new StringBuilder();
                foreach (Addition addition in gear.Additions)
                {
                    string conString = addition.GetConString(), propString = addition.GetPropString();
                    if (!string.IsNullOrEmpty(conString) || !string.IsNullOrEmpty(propString))
                    {
                        sb.Append("- ");
                        if (!string.IsNullOrEmpty(conString))
                            sb.AppendLine(conString);
                        if (!string.IsNullOrEmpty(propString))
                            sb.AppendLine(propString);
                        sb.AppendLine();
                    }
                }
                if (sb.Length > 0)
                {
                    picHeight = 10;
                    GearGraphics.DrawString(g, sb.ToString(), GearGraphics.ItemDetailFont, 8, 236, ref picHeight, 16);
                }
                g.Dispose();
            }
            return addBitmap;
        }

        private Bitmap renderSetItem(out int picHeight)
        {
            Bitmap setBitmap = null;
            int setID;
            picHeight = 0;
            if (gear.Props.TryGetValue(GearPropType.setItemID, out setID))
            {
                SetItem setItem;
                if (!CharaSimLoader.LoadedSetItems.TryGetValue(setID, out setItem))
                    return null;
                setBitmap = new Bitmap(252, DefaultPicHeight);
                Graphics g = Graphics.FromImage(setBitmap);
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;

                picHeight = 10;
                g.DrawString(setItem.SetItemName, GearGraphics.ItemDetailFont, GearGraphics.SetItemNameBrush, 126, 10, format);
                picHeight += 25;

                format.Alignment=StringAlignment.Far;

                foreach (var setItemPart in setItem.ItemIDs.Parts)
                {
                    string itemName = setItemPart.Value.RepresentName;
                    string typeName = setItemPart.Value.TypeName;

                    if (string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(typeName))
                    {
                        foreach (var itemID in setItemPart.Value.ItemIDs)
                        {
                            StringResult sr;
                            if (!StringLinker.StringEqp.TryGetValue(itemID.Key, out sr))
                            {
                                sr = new StringResult();
                                sr.Name = "(null)";
                            }
                            itemName = sr.Name;
                            typeName = ItemStringHelper.GetSetItemGearTypeString(Gear.GetGearType(itemID.Key));
                            break;
                        }
                    }

                    itemName = itemName ?? string.Empty;
                    typeName = typeName ?? "装备";

                    Brush brush = setItemPart.Value.Enabled ? Brushes.White : GearGraphics.SetItemGrayBrush;
                    g.DrawString(itemName, GearGraphics.ItemDetailFont, brush, 8, picHeight);
                    g.DrawString("(" + typeName + ")", GearGraphics.ItemDetailFont, brush, 246, picHeight, format);
                    picHeight += 18;
                }

                picHeight += 5;
                g.DrawLine(Pens.White, 6, picHeight, 245, picHeight);//分割线
                picHeight += 9;
                foreach (KeyValuePair<int, SetItemEffect> effect in setItem.Effects)
                {
                    g.DrawString(effect.Key + "套装效果", GearGraphics.ItemDetailFont, GearGraphics.SetItemNameBrush, 8, picHeight);
                    picHeight += 16;
                    Brush brush = effect.Value.Enabled ? Brushes.White : GearGraphics.SetItemGrayBrush;
                    foreach (KeyValuePair<GearPropType, object> prop in effect.Value.Props)
                    {
                        if (prop.Key == GearPropType.Option)
                        {
                            List<Potential> ops = (List<Potential>)prop.Value;
                            foreach (Potential p in ops)
                            {
                                g.DrawString(p.ConvertSummary(), GearGraphics.SetItemPropFont, brush, 8, picHeight);
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
            }
            return setBitmap;
        }

        private string GetGearAttributeString(int line)
        {
            int value;
            List<string> tags = new List<string>();
            switch (line)
            {
                case 0:
                    if (gear.Props.TryGetValue(GearPropType.only, out value) && value != 0)
                    {
                        tags.Add(ItemStringHelper.GetGearPropString(GearPropType.only, value));
                    }
                    if (gear.Props.TryGetValue(GearPropType.tradeBlock, out value) && value != 0)
                    {
                        tags.Add(ItemStringHelper.GetGearPropString(GearPropType.tradeBlock, value));
                    }
                    if (gear.Props.TryGetValue(GearPropType.accountSharable, out value) && value != 0)
                    {
                        tags.Add(ItemStringHelper.GetGearPropString(GearPropType.accountSharable, value));
                    }
                    if (gear.Props.TryGetValue(GearPropType.equipTradeBlock, out value) && value != 0)
                    {
                        if (gear.State == GearState.itemList)
                        {
                            tags.Add(ItemStringHelper.GetGearPropString(GearPropType.equipTradeBlock, value));
                        }
                        else
                        {
                            string tradeBlock = ItemStringHelper.GetGearPropString(GearPropType.tradeBlock, 1);
                            if (!tags.Contains(tradeBlock))
                                tags.Add(tradeBlock);
                        }
                    }
                    if (gear.Props.TryGetValue(GearPropType.noPotential, out value) && value != 0)
                    {
                        tags.Add(ItemStringHelper.GetGearPropString(GearPropType.noPotential, value));
                    }
                    if (gear.Props.TryGetValue(GearPropType.fixedPotential, out value) && value != 0)
                    {
                        tags.Add(ItemStringHelper.GetGearPropString(GearPropType.fixedPotential, value));
                    }
                    break;
                case 1:
                    if (gear.Props.TryGetValue(GearPropType.onlyEquip, out value) && value != 0)
                    {
                        tags.Add(ItemStringHelper.GetGearPropString(GearPropType.onlyEquip, value));
                    }
                    if (gear.Props.TryGetValue(GearPropType.notExtend, out value) && value != 0)
                    {
                        tags.Add(ItemStringHelper.GetGearPropString(GearPropType.notExtend, value));
                    }
                    break;
            }
            return tags.Count > 0 ? string.Join(", ", tags.ToArray()) : null;
        }

        private void drawGearReq(Graphics g, ref int picHeight)
        {
            int value;
            bool isGetProp;
            bool can;

            //等级要求
            isGetProp = gear.Props.TryGetValue(GearPropType.reqLevel, out value);
            can = (charStat == null || charStat.Level >= value);
            g.DrawImage(can ? Resource.ToolTip_Equip_Can_reqLEV : Resource.ToolTip_Equip_Cannot_reqLEV, 96, picHeight);
            GearGraphics.DrawGearDetailNumber(g, 156, picHeight + 4, isGetProp ? value.ToString() : "-", can);
            picHeight += 12;

            //力量要求
            isGetProp = gear.Props.TryGetValue(GearPropType.reqSTR, out value);
            can = (charStat == null || charStat.Strength.GetSum() >= value);
            g.DrawImage(can ? Resource.ToolTip_Equip_Can_reqSTR : Resource.ToolTip_Equip_Cannot_reqSTR, 96, picHeight);
            GearGraphics.DrawGearDetailNumber(g, 156, picHeight + 4, isGetProp ? value.ToString() : "-", can);
            picHeight += 12;

            //敏捷要求
            isGetProp = gear.Props.TryGetValue(GearPropType.reqDEX, out value);
            can = (charStat == null || charStat.Dexterity.GetSum() >= value);
            g.DrawImage(can ? Resource.ToolTip_Equip_Can_reqDEX : Resource.ToolTip_Equip_Cannot_reqDEX, 96, picHeight);
            GearGraphics.DrawGearDetailNumber(g, 156, picHeight + 4, isGetProp ? value.ToString() : "-", can);
            picHeight += 12;

            //智力要求
            isGetProp = gear.Props.TryGetValue(GearPropType.reqINT, out value);
            can = (charStat == null || charStat.Intelligence.GetSum() >= value);
            g.DrawImage(can ? Resource.ToolTip_Equip_Can_reqINT : Resource.ToolTip_Equip_Cannot_reqINT, 96, picHeight);
            GearGraphics.DrawGearDetailNumber(g, 156, picHeight + 4, isGetProp ? value.ToString() : "-", can);
            picHeight += 12;

            //运气要求
            isGetProp = gear.Props.TryGetValue(GearPropType.reqLUK, out value);
            can = (charStat == null || charStat.Luck.GetSum() >= value);
            g.DrawImage(can ? Resource.ToolTip_Equip_Can_reqLUK : Resource.ToolTip_Equip_Cannot_reqLUK, 96, picHeight);
            GearGraphics.DrawGearDetailNumber(g, 156, picHeight + 4, isGetProp ? value.ToString() : "-", can);
            picHeight += 12;

            //人气要求
            isGetProp = gear.Props.TryGetValue(GearPropType.reqPOP, out value);
            can = (charStat == null || charStat.Pop >= value);
            g.DrawImage(can ? Resource.ToolTip_Equip_Can_reqPOP : Resource.ToolTip_Equip_Cannot_reqPOP, 96, picHeight);
            GearGraphics.DrawGearDetailNumber(g, 156, picHeight + 4, isGetProp ? value.ToString() : "-", can);
            picHeight += 12;
        }

        private Image GetAdditionalOptionIcon(GearGrade grade)
        {
            switch (grade)
            {
                case GearGrade.B: return Resource.AdditionalOptionTooltip_rare;
                case GearGrade.A: return Resource.AdditionalOptionTooltip_epic;
                case GearGrade.S: return Resource.AdditionalOptionTooltip_unique;
                case GearGrade.SS: return Resource.AdditionalOptionTooltip_legendary;
            }
            return null;
        }

        private Point[] getRankBorderPath(int height)
        {
            List<Point> pointList = new List<Point>(5);
            pointList.Add(new Point(252 - 4, height - 5));
            pointList.Add(new Point(252 - 4, 4));
            pointList.Add(new Point(4, 4));
            pointList.Add(new Point(4, height - 4));
            pointList.Add(new Point(252 - 5, height - 4));
            return pointList.ToArray();
        }

    }
}

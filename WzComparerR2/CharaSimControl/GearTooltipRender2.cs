using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using Resource = CharaSimResource.Resource;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;
using WzComparerR2.WzLib;
using System.Windows.Forms;

namespace WzComparerR2.CharaSimControl
{
    public class GearTooltipRender2 : TooltipRender
    {
        static GearTooltipRender2()
        {
            res = new Dictionary<string, TextureBrush>();
            res["t"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_top, WrapMode.Clamp);
            res["line"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_line, WrapMode.Tile);
            res["dotline"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_dotline, WrapMode.Clamp);
            res["b"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_bottom, WrapMode.Clamp);
            res["cover"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_cover, WrapMode.Clamp);
        }

        private static Dictionary<string, TextureBrush> res;

        public GearTooltipRender2()
        {
        }

        private CharacterStatus charStat;

        public Gear Gear { get; set; }

        public override object TargetItem
        {
            get { return this.Gear; }
            set { this.Gear = value as Gear; }
        }

        public CharacterStatus CharacterStatus
        {
            get { return charStat; }
            set { charStat = value; }
        }

        public bool ShowSpeed { get; set; }
        public bool ShowLevelOrSealed { get; set; }
        public bool ShowMedalTag { get; set; } = true;
        public bool IsCombineProperties { get; set; } = true;

        public TooltipRender SetItemRender { get; set; }

        public override Bitmap Render()
        {
            if (this.Gear == null)
            {
                return null;
            }

            int[] picH = new int[4];
            Bitmap left = RenderBase(out picH[0]);
            Bitmap add = RenderAddition(out picH[1]);
            Bitmap set = RenderSetItem(out picH[2]);
            Bitmap levelOrSealed = null;
            if (this.ShowLevelOrSealed)
            {
                levelOrSealed = RenderLevelOrSealed(out picH[3]);
            }

            int width = 261;
            if (add != null) width += add.Width;
            if (set != null) width += set.Width;
            if (levelOrSealed != null) width += levelOrSealed.Width;
            int height = 0;
            for (int i = 0; i < picH.Length; i++)
            {
                height = Math.Max(height, picH[i]);
            }
            Bitmap tooltip = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制主图
            width = 0;
            if (left != null)
            {
                //绘制背景
                g.DrawImage(res["t"].Image, width, 0);
                FillRect(g, res["line"], width, 13, picH[0] - 13);
                g.DrawImage(res["b"].Image, width, picH[0] - 13);

                //复制图像
                g.DrawImage(left, width, 0, new Rectangle(0, 0, left.Width, picH[0]), GraphicsUnit.Pixel);

                //cover
                g.DrawImage(res["cover"].Image, 3, 3);

                width += left.Width;
                left.Dispose();
            }

            //绘制addition
            if (add != null)
            {
                //绘制背景
                g.DrawImage(res["t"].Image, width, 0);
                FillRect(g, res["line"], width, 13, tooltip.Height - 13);
                g.DrawImage(res["b"].Image, width, tooltip.Height - 13);

                //复制原图
                g.DrawImage(add, width, 0, new Rectangle(0, 0, add.Width, picH[1]), GraphicsUnit.Pixel);

                width += add.Width;
                add.Dispose();
            }

            //绘制setitem
            if (set != null)
            {
                //绘制背景
                //g.DrawImage(res["t"].Image, width, 0);
                //FillRect(g, res["line"], width, 13, picH[2] - 13);
                //g.DrawImage(res["b"].Image, width, picH[2] - 13);

                //复制原图
                g.DrawImage(set, width, 0, new Rectangle(0, 0, set.Width, picH[2]), GraphicsUnit.Pixel);
                width += set.Width;
                set.Dispose();
            }

            //绘制levelOrSealed
            if (levelOrSealed != null)
            {
                //绘制背景
                g.DrawImage(res["t"].Image, width, 0);
                FillRect(g, res["line"], width, 13, picH[3] - 13);
                g.DrawImage(res["b"].Image, width, picH[3] - 13);

                //复制原图
                g.DrawImage(levelOrSealed, width, 0, new Rectangle(0, 0, levelOrSealed.Width, picH[3]), GraphicsUnit.Pixel);
                width += levelOrSealed.Width;
                levelOrSealed.Dispose();
            }

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, Gear.ItemID.ToString("d8"), true);
            }

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderBase(out int picH)
        {
            Bitmap bitmap = new Bitmap(261, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone();
            int value;

            picH = 13;
            DrawStar2(g, ref picH); //绘制星星

            //绘制装备名称
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringEqp.TryGetValue(Gear.ItemID, out sr))
            {
                sr = new StringResult();
                sr.Name = "(null)";
            }
            string gearName = sr.Name;
            string nameAdd = Gear.ScrollUp > 0 ? ("+" + Gear.ScrollUp) : null;
            switch (Gear.GetGender(Gear.ItemID))
            {
                case 0: nameAdd += "Male"; break;
                case 1: nameAdd += "Female"; break;
            }
            if (!string.IsNullOrEmpty(nameAdd))
            {
                gearName += " (" + nameAdd + ")";
            }

            format.Alignment = StringAlignment.Center;
            TextRenderer.DrawText(g, gearName, GearGraphics.EquipNameFont2,
                new Point(261, picH), ((SolidBrush)GearGraphics.GetGearNameBrush(Gear.diff, Gear.ScrollUp > 0, Gear.Cash, Gear.ItemID / 10000 == 180)).Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            picH += 23;

            //装备rank
            string rankStr = null;
            if (Gear.GetBooleanValue(GearPropType.specialGrade))
            {
                rankStr = ItemStringHelper.GetGearGradeString(GearGrade.Special);
            }
            else if (!Gear.Cash) //T98后C级物品依然显示
            {
                rankStr = ItemStringHelper.GetGearGradeString(Gear.Grade);
            }
            if (rankStr != null)
            {
                TextRenderer.DrawText(g, rankStr, GearGraphics.EquipDetailFont, new Point(261, picH), Color.White, TextFormatFlags.HorizontalCenter);
                picH += 15;
            }

            //额外属性
            var attrList = GetGearAttributeString();
            if (attrList.Count > 0)
            {
                var font = GearGraphics.EquipDetailFont;
                string attrStr = null;
                for (int i = 0; i < attrList.Count; i++)
                {
                    var newStr = (attrStr != null ? (attrStr + ", ") : null) + attrList[i];
                    if (TextRenderer.MeasureText(g, newStr, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width > 261 - 7)
                    {
                        TextRenderer.DrawText(g, attrStr, GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.OrangeBrush2).Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                        picH += 15;
                        attrStr = attrList[i];
                    }
                    else
                    {
                        attrStr = newStr;
                    }
                }
                if (!string.IsNullOrEmpty(attrStr))
                {
                    TextRenderer.DrawText(g, attrStr, GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.OrangeBrush2).Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                    picH += 15;
                }
            }

            //装备限时
            if (Gear.TimeLimited)
            {
                DateTime time = DateTime.Now.ToUniversalTime().AddDays(7d);
                string timeLimited = "Available Until: ";
                string expireStr = timeLimited + time.ToString("M/d/yyyy HH:mm UTC");
                TextRenderer.DrawText(g, expireStr, GearGraphics.EquipDetailFont, new Point(261, picH), Color.White, TextFormatFlags.HorizontalCenter);
                picH += 15;
            }
            else if (Gear.GetBooleanValue(GearPropType.abilityTimeLimited))
            {
                string timeLimitedStats = "EFFECTS LAST UNTIL ";
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr = timeLimitedStats + time.ToString("M-d-yyyy AT HH:mm");
                TextRenderer.DrawText(g, expireStr, GearGraphics.EquipDetailFont, new Point(261, picH), Color.White, TextFormatFlags.HorizontalCenter);
                picH += 15;
            }


            //分割线1号
            picH += 7;
            g.DrawImage(res["dotline"].Image, 0, picH);

            //绘制装备图标
            if (Gear.Grade > 0 && (int)Gear.Grade <= 4) //绘制外框
            {
                Image border = Resource.ResourceManager.GetObject("UIToolTip_img_Item_ItemIcon_" + (int)Gear.Grade) as Image;
                if (border != null)
                {
                    g.DrawImage(border, 13, picH + 11);
                }
            }
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_base, 12, picH + 10); //绘制背景
            if (Gear.IconRaw.Bitmap != null) //绘制icon
            {
                var attr = new System.Drawing.Imaging.ImageAttributes();
                var matrix = new System.Drawing.Imaging.ColorMatrix(
                    new[] {
                        new float[] { 1, 0, 0, 0, 0 },
                        new float[] { 0, 1, 0, 0, 0 },
                        new float[] { 0, 0, 1, 0, 0 },
                        new float[] { 0, 0, 0, 0.5f, 0 },
                        new float[] { 0, 0, 0, 0, 1 },
                        });
                attr.SetColorMatrix(matrix);

                //绘制阴影
                var shade = Resource.UIToolTip_img_Item_ItemIcon_shade;
                g.DrawImage(shade,
                    new Rectangle(18 + 9, picH + 15 + 54, shade.Width, shade.Height),
                    0, 0, shade.Width, shade.Height,
                    GraphicsUnit.Pixel,
                    attr);
                //绘制图标
                g.DrawImage(GearGraphics.EnlargeBitmap(Gear.IconRaw.Bitmap),
                    18 + (1 - Gear.IconRaw.Origin.X) * 2,
                    picH + 15 + (33 - Gear.IconRaw.Origin.Y) * 2);

                attr.Dispose();
            }
            if (Gear.Cash) //绘制cash标识
            {
                /* not installed since CMST136
                 * 
                if (Gear.Props.TryGetValue(GearPropType.royalSpecial, out value) && value > 0)
                    g.DrawImage(GearGraphics.EnlargeBitmap(Resource.CashItem_label_0),
                        18 + 68 - 26,
                        picH + 15 + 68 - 26);
                else if (Gear.Props.TryGetValue(GearPropType.masterSpecial, out value) && value > 0)
                    g.DrawImage(GearGraphics.EnlargeBitmap(Resource.CashItem_label_3),
                        18 + 68 - 26,
                        picH + 15 + 68 - 26);
                else
                */
                g.DrawImage(GearGraphics.EnlargeBitmap(Resource.CashItem_0),
                    18 + 68 - 26,
                    picH + 15 + 68 - 26);
            }
            //检查星岩
            bool hasSocket = Gear.GetBooleanValue(GearPropType.nActivatedSocket);
            if (hasSocket)
            {
                Bitmap socketBmp = GetAlienStoneIcon();
                if (socketBmp != null)
                {
                    g.DrawImage(GearGraphics.EnlargeBitmap(socketBmp),
                        18 + 2,
                        picH + 15 + 3);
                }
            }

            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_cover, 16, picH + 14); //绘制左上角cover

            //绘制攻击力变化
            format.Alignment = StringAlignment.Far;
            TextRenderer.DrawText(g, "Attack Power", GearGraphics.EquipDetailFont, new Point(251 - TextRenderer.MeasureText(g, "Attack Power", GearGraphics.EquipDetailFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width, picH + 10), ((SolidBrush)GearGraphics.GrayBrush2).Color, TextFormatFlags.NoPadding);
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_incline_0, 249 - 19, picH + 27); //暂时画个0

            //绘制属性需求
            DrawGearReq(g, 97, picH + 58);
            picH += 93;

            //绘制属性变化
            DrawPropDiffEx(g, 12, picH);
            picH += 20;

            //绘制职业需求
            DrawJobReq(g, ref picH);

            //分割线2号
            g.DrawImage(res["dotline"].Image, 0, picH);
            picH += 8;

            bool hasPart2 = false;
            format.Alignment = StringAlignment.Center;

            //绘制属性
            if (Gear.Props.TryGetValue(GearPropType.superiorEqp, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Superior", GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
                picH += 15;
            }
            if (Gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Max Damage: ", GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
                picH += 15;
            }

            //绘制装备升级
            if (Gear.Props.TryGetValue(GearPropType.level, out value) && !Gear.FixLevel)
            {
                bool max = (Gear.Levels != null && value >= Gear.Levels.Count);
                TextRenderer.DrawText(g, "Growth Level: " + (max ? "MAX" : value.ToString()), GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
                TextRenderer.DrawText(g, "Growth EXP: " + (max ? "MAX" : "0%"), GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
            }
            else if (Gear.ItemID / 10000 == 171 && Gear.ItemID != 1712000)
            {
                TextRenderer.DrawText(g, "Growth Level: 1", GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
                TextRenderer.DrawText(g, "Item EXP: 1 / 0 ( 0% )", GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
            }

            if (Gear.Props.TryGetValue(GearPropType.@sealed, out value))
            {
                bool max = (Gear.Seals != null && value >= Gear.Seals.Count);
                TextRenderer.DrawText(g, "封印解除阶段 : " + (max ? "MAX" : value.ToString()), GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
                TextRenderer.DrawText(g, "封印解除经验值 : " + (max ? "MAX" : "0%"), GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
            }

            //绘制耐久度
            if (Gear.Props.TryGetValue(GearPropType.durability, out value))
            {
                TextRenderer.DrawText(g, "Durability: " + "100%", GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
                picH += 15;
            }

            //装备类型
            bool isWeapon = Gear.IsLeftWeapon(Gear.type) || Gear.IsDoubleHandWeapon(Gear.type);
            string typeStr = ItemStringHelper.GetGearTypeString(Gear.type);
            if (!string.IsNullOrEmpty(typeStr))
            {
                if (isWeapon)
                {
                    typeStr = "Type: " + typeStr;
                }
                else
                {
                    typeStr = "Type: " + typeStr;
                }

                if (Gear.IsLeftWeapon(Gear.type) || Gear.type == GearType.katara)
                {
                    typeStr += " (1H)";
                }
                else if (Gear.IsDoubleHandWeapon(Gear.type))
                {
                    typeStr += " (2H)";
                }
                TextRenderer.DrawText(g, typeStr, GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 15;
                hasPart2 = true;
            }

            if (!Gear.Props.TryGetValue(GearPropType.attackSpeed, out value)
                && (Gear.IsWeapon(Gear.type) || Gear.type == GearType.katara)) //找不到攻速的武器
            {
                value = 6; //给予默认速度
            }
            //  if (gear.Props.TryGetValue(GearPropType.attackSpeed, out value) && value > 0)
            if (!Gear.Cash && value > 0)
            {
                TextRenderer.DrawText(g, "Attack Speed: " + ItemStringHelper.GetAttackSpeedString(value) + (ShowSpeed ? (" (" + value + ")") : null),
                    GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 15;
                hasPart2 = true;
            }
            //机器人等级
            if (Gear.Props.TryGetValue(GearPropType.grade, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Rank: " + value, GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 15;
                hasPart2 = true;
            }


            //一般属性
            List<GearPropType> props = new List<GearPropType>();
            foreach (KeyValuePair<GearPropType, int> p in Gear.PropsV5) //5转过滤
            {
                if ((int)p.Key < 100 && p.Value != 0)
                    props.Add(p.Key);
            }
            props.Sort();
            //bool epic = Gear.Props.TryGetValue(GearPropType.epicItem, out value) && value > 0;
            foreach (GearPropType type in props)
            {
                //var font = (epic && Gear.IsEpicPropType(type)) ? GearGraphics.EpicGearDetailFont : GearGraphics.ItemDetailFont;
                //g.DrawString(ItemStringHelper.GetGearPropString(type, Gear.Props[type]), font, Brushes.White, 11, picH);
                //picH += 16;

                //绘制属性变化
                Gear.StandardProps.TryGetValue(type, out value); //standard value
                var propStr = ItemStringHelper.GetGearPropDiffString(type, Gear.Props[type], value);
                GearGraphics.DrawString(g, propStr, GearGraphics.EquipDetailFont, 13, 256, ref picH, 15);
                hasPart2 = true;
            }

            //戒指特殊潜能
            int ringOpt, ringOptLv;
            if (Gear.Props.TryGetValue(GearPropType.ringOptionSkill, out ringOpt)
                && Gear.Props.TryGetValue(GearPropType.ringOptionSkillLv, out ringOptLv))
            {
                var opt = Potential.LoadFromWz(ringOpt, ringOptLv, PluginBase.PluginManager.FindWz);
                if (opt != null)
                {
                    TextRenderer.DrawText(g, opt.ConvertSummary(), GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                    hasPart2 = true;
                }
            }

            bool hasReduce = Gear.Props.TryGetValue(GearPropType.reduceReq, out value);
            if (hasReduce && value > 0)
            {
                TextRenderer.DrawText(g, ItemStringHelper.GetGearPropString(GearPropType.reduceReq, value), GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 15;
                hasPart2 = true;
            }

            bool hasTuc = Gear.HasTuc && Gear.Props.TryGetValue(GearPropType.tuc, out value);
            if (Gear.GetBooleanValue(GearPropType.exceptUpgrade))
            {
                TextRenderer.DrawText(g, "Unable to enhance", GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 15;
            }
            else if (hasTuc)
            {
                GearGraphics.DrawString(g, "Remaining Enhancements: " + value + "\n#c(Available Recoveries: 0)#\nHammers Applied: 0", GearGraphics.EquipDetailFont, 13, 244, ref picH, 15, orangeColor: ((SolidBrush)GearGraphics.OrangeBrush3).Color);
                hasPart2 = true;
            }

            //星星锤子
            /*if (hasTuc && Gear.Hammer > -1 && Gear.GetMaxStar() > 0)
            {
                if (Gear.Hammer >= 1)
                {
                    TextRenderer.DrawText(g, "Hammers Applied: 0", GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                }
                if (Gear.Props.TryGetValue(GearPropType.superiorEqp, out value) && value > 0) //极真
                {
                    GearGraphics.DrawPlainText(g, ItemStringHelper.GetGearPropString(GearPropType.superiorEqp, value), GearGraphics.EquipDetailFont, ((SolidBrush)GearGraphics.GreenBrush2).Color, 13, 244, ref picH, 15);
                }

                if (!Gear.GetBooleanValue(GearPropType.exceptUpgrade))
                {
                    int maxStar = Gear.GetMaxStar();

                    if (Gear.Star > 0) //星星
                    {
                        g.DrawString("Star Force: " + Gear.Star + " / " + maxStar + " Stars Infused", GearGraphics.ItemDetailFont, Brushes.White, 11, picH);
                        picH += 16;
                    }
                    else
                    {
                        g.DrawString("Can be enhanced up to " + maxStar + " Star.", GearGraphics.ItemDetailFont, Brushes.White, 11, picH);
                        picH += 16;
                    }
                }
        }*/

            if (Gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0) //突破上限
            {
                TextRenderer.DrawText(g, ItemStringHelper.GetGearPropString(GearPropType.limitBreak, value), GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
                picH += 15;
                hasPart2 = true;
            }

            /*
            if (hasTuc && Gear.Hammer > -1)
            {
                picH += 2;
                TextRenderer.DrawText(g, "Hammers Applied", GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
                g.DrawString("Hammers Applied", GearGraphics.ItemDetailFont, GearGraphics.GoldHammerBrush, 11, picH);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.DrawString(": " + Gear.Hammer.ToString() + (Gear.Hammer == 2 ? "(MAX)" : null), GearGraphics.TahomaFont, GearGraphics.GoldHammerBrush, 145, picH - 2);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                picH += 16;
                hasPart2 = true;
            }
            */


            if (Gear.type == GearType.shovel || Gear.type == GearType.pickaxe)
            {
                string skillName = null;
                switch (Gear.type)
                {
                    case GearType.shovel: skillName = "Herbalism"; break;
                    case GearType.pickaxe: skillName = "Mining"; break;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_incSkillLevel, out value) && value > 0)
                {
                    TextRenderer.DrawText(g, skillName + " Skill: +" + value, GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                    hasPart2 = true;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_incSpeed, out value) && value > 0)
                {
                    TextRenderer.DrawText(g, skillName + " Speed: +" + value + "%", GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                    hasPart2 = true;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_incNum, out value) && value > 0)
                {
                    TextRenderer.DrawText(g, "Can get up to " + value + " item(s)", GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                    hasPart2 = true;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_reqSkillLevel, out value) && value > 0)
                {
                    TextRenderer.DrawText(g, "Requires " + value + " " + skillName + " Skill", GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                    hasPart2 = true;
                }
            }
            picH += 5;

            //绘制浮动属性
            if ((Gear.VariableStat != null && Gear.VariableStat.Count > 0) || hasReduce)
            {
                if (hasPart2) //分割线...
                {
                    picH -= 1;
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }

                if (Gear.VariableStat != null && Gear.VariableStat.Count > 0)
                {
                    int reqLvl;
                    Gear.Props.TryGetValue(GearPropType.reqLevel, out reqLvl);
                    TextRenderer.DrawText(g, "Add Stats per Character Level (Until Lv. " + reqLvl + ")", GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.HorizontalCenter);
                    picH += 20;

                    int reduceLvl;
                    Gear.Props.TryGetValue(GearPropType.reduceReq, out reduceLvl);

                    int curLevel = charStat == null ? reqLvl : Math.Min(charStat.Level, reqLvl);

                    foreach (var kv in Gear.VariableStat)
                    {
                        int dLevel = curLevel - reqLvl + reduceLvl;
                        //int addVal = (int)Math.Floor(kv.Value * dLevel);
                        //这里有一个计算上的错误 换方式执行
                        int addVal = (int)Math.Floor(new decimal(kv.Value) * dLevel);
                        string text = ItemStringHelper.GetGearPropString(kv.Key, addVal, 1);
                        text += string.Format(" ({0:f1} x {1})", kv.Value, dLevel);
                        TextRenderer.DrawText(g, text, GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                        picH += 20;
                    }

                    if (hasReduce)
                    {
                        TextRenderer.DrawText(g, "When upgrading or enhancing, it will be viewed as Lv. " + reqLvl, GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.GrayBrush2).Color, TextFormatFlags.NoPadding);
                        picH += 15;
                    }
                }
            }

            //绘制潜能
            int optionCount = 0;
            foreach (Potential potential in Gear.Options)
            {
                if (potential != null)
                {
                    optionCount++;
                }
            }

            if (optionCount > 0)
            {
                //分割线3号
                if (hasPart2)
                {
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }
                g.DrawImage(GetAdditionalOptionIcon(Gear.Grade), 9, picH - 1);
                TextRenderer.DrawText(g, "Potential", GearGraphics.EquipDetailFont, new Point(27, picH), ((SolidBrush)GearGraphics.GetPotentialTextBrush(Gear.Grade)).Color, TextFormatFlags.NoPadding);
                picH += 15;
                foreach (Potential potential in Gear.Options)
                {
                    if (potential != null)
                    {
                        GearGraphics.DrawString(g, potential.ConvertSummary(), GearGraphics.EquipDetailFont2, 13, 244, ref picH, 15);
                    }
                }
                picH += 5;
            }

            if (hasSocket)
            {
                g.DrawLine(Pens.White, 6, picH, 254, picH);
                picH += 8;
                GearGraphics.DrawString(g, ItemStringHelper.GetGearPropString(GearPropType.nActivatedSocket, 1),
                    GearGraphics.ItemDetailFont, 11, 247, ref picH, 15);
                picH += 3;
            }

            //绘制附加潜能
            int adOptionCount = 0;
            foreach (Potential potential in Gear.AdditionalOptions)
            {
                if (potential != null)
                {
                    adOptionCount++;
                }
            }
            if (adOptionCount > 0)
            {
                //分割线4号
                if (hasPart2)
                {
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }
                g.DrawImage(GetAdditionalOptionIcon(Gear.AdditionGrade), 8, picH - 1);
                TextRenderer.DrawText(g, "Potential", GearGraphics.EquipDetailFont, new Point(27, picH), ((SolidBrush)GearGraphics.GetPotentialTextBrush(Gear.AdditionGrade)).Color, TextFormatFlags.NoPadding);
                picH += 17;

                foreach (Potential potential in Gear.AdditionalOptions)
                {
                    if (potential != null)
                    {
                        GearGraphics.DrawString(g, "+ " + potential.ConvertSummary(), GearGraphics.EquipDetailFont2, 13, 244, ref picH, 15);
                    }
                }
                picH += 5;
            }

            //绘制desc
            List<string> desc = new List<string>();
            GearPropType[] descTypes = new GearPropType[]{
                GearPropType.tradeAvailable,
                GearPropType.accountShareTag,
                GearPropType.jokerToSetItem,
                GearPropType.colorvar,
            };
            foreach (GearPropType type in descTypes)
            {
                if (Gear.Props.TryGetValue(type, out value) && value != 0)
                {
                    desc.Add(ItemStringHelper.GetGearPropString(type, value));
                }
            }

            //绘制倾向
            if (Gear.State == GearState.itemList)
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
                    "Ambition ","Empathy ","Insight ","Willpower ","Diligence ","Charm "};

                for (int i = 0; i < inclineTypes.Length; i++)
                {
                    if (Gear.Props.TryGetValue(inclineTypes[i], out value) && value > 0)
                    {
                        incline += "，" + inclineString[i] + value;
                    }
                }

                if (!string.IsNullOrEmpty(incline))
                {
                    desc.Add("\n#c" + incline.Substring(1) + " EXP can be obtained once when equipped.#");
                }
            }

            //判断是否绘制徽章
            Wz_Node medalResNode = null;
            bool willDrawMedalTag = this.ShowMedalTag
                && this.Gear.Props.TryGetValue(GearPropType.medalTag, out value)
                && this.TryGetMedalResource(value, out medalResNode);

            //判断是否绘制技能desc
            string levelDesc = null;
            if (Gear.FixLevel && Gear.Props.TryGetValue(GearPropType.level, out value))
            {
                var levelInfo = Gear.Levels.FirstOrDefault(info => info.Level == value);
                if (levelInfo != null && levelInfo.Prob == levelInfo.ProbTotal && !string.IsNullOrEmpty(levelInfo.HS))
                {
                    levelDesc = sr[levelInfo.HS];
                }
            }

            if (!string.IsNullOrEmpty(sr.Desc) || !string.IsNullOrEmpty(levelDesc) || desc.Count > 0 || Gear.Sample.Bitmap != null || willDrawMedalTag)
            {
                //分割线4号
                if (hasPart2)
                {
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }
                if (Gear.Sample.Bitmap != null)
                {
                    g.DrawImage(Gear.Sample.Bitmap, (bitmap.Width - Gear.Sample.Bitmap.Width) / 2, picH);
                    picH += Gear.Sample.Bitmap.Height;
                    picH += 4;
                }
                if (medalResNode != null)
                {
                    GearGraphics.DrawNameTag(g, medalResNode, sr.Name, bitmap.Width, ref picH);
                    picH += 4;
                }
                if (!string.IsNullOrEmpty(sr.Desc))
                {
                    GearGraphics.DrawString(g, sr.Desc, GearGraphics.ItemDetailFont2, 11, 245, ref picH, 16);
                }
                if (!string.IsNullOrEmpty(levelDesc))
                {
                    GearGraphics.DrawString(g, " " + levelDesc, GearGraphics.ItemDetailFont2, 11, 245, ref picH, 16);
                }
                foreach (string str in desc)
                {
                    GearGraphics.DrawString(g, str, GearGraphics.ItemDetailFont, 11, 245, ref picH, 16);
                }
                picH += 5;
            }

            foreach (KeyValuePair<int, ExclusiveEquip> kv in CharaSimLoader.LoadedExclusiveEquips)
            {
                if (kv.Value.Items.Contains(Gear.ItemID))
                {
                    if (hasPart2)
                    {
                        g.DrawImage(res["dotline"].Image, 0, picH);
                        picH += 8;
                    }

                    string exclusiveEquip;
                    if (!string.IsNullOrEmpty(kv.Value.Info))
                    {
                        exclusiveEquip = "#cCannot equip multiple " + kv.Value.Info + " items.#";
                    }
                    else
                    {
                        List<string> itemNames = new List<string>();
                        foreach (int itemID in kv.Value.Items)
                        {
                            StringResult sr2;
                            if (this.StringLinker == null || !this.StringLinker.StringEqp.TryGetValue(itemID, out sr2))
                            {
                                sr2 = new StringResult();
                                sr2.Name = "(null)";
                            }
                            itemNames.Add(sr2.Name);
                        }
                        exclusiveEquip = "#cThe following items cannot be equipped\r\nsimultaneously.\r\n\r\n-------<Equipment>------\r\n" + string.Join(", ", itemNames) + ".#";
                    }
                    GearGraphics.DrawString(g, exclusiveEquip, GearGraphics.ItemDetailFont, 11, 246, ref picH, 16);
                    break;
                }
            }

            picH += 2;
            format.Dispose();
            g.Dispose();
            return bitmap;
        }

        private Bitmap RenderAddition(out int picHeight)
        {
            Bitmap addBitmap = null;
            picHeight = 0;
            if (Gear.Additions.Count > 0)
            {
                addBitmap = new Bitmap(261, DefaultPicHeight);
                Graphics g = Graphics.FromImage(addBitmap);
                StringBuilder sb = new StringBuilder();
                foreach (Addition addition in Gear.Additions)
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
                    GearGraphics.DrawString(g, sb.ToString(), GearGraphics.ItemDetailFont, 10, 250, ref picHeight, 16);
                }
                g.Dispose();
            }
            return addBitmap;
        }

        private Bitmap RenderSetItem(out int picHeight)
        {
            Bitmap setBitmap = null;
            int setID;
            picHeight = 0;
            if (Gear.Props.TryGetValue(GearPropType.setItemID, out setID))
            {
                SetItem setItem;
                if (!CharaSimLoader.LoadedSetItems.TryGetValue(setID, out setItem))
                    return null;

                TooltipRender renderer = this.SetItemRender;
                if (renderer == null)
                {
                    var defaultRenderer = new SetItemTooltipRender();
                    defaultRenderer.StringLinker = this.StringLinker;
                    defaultRenderer.ShowObjectID = false;
                    renderer = defaultRenderer;
                }

                renderer.TargetItem = setItem;
                setBitmap = renderer.Render();
                if (setBitmap != null)
                    picHeight = setBitmap.Height;
            }
            return setBitmap;
        }

        private Bitmap RenderLevelOrSealed(out int picHeight)
        {
            Bitmap levelOrSealed = null;
            Graphics g = null;
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            picHeight = 0;
            if (Gear.Levels != null)
            {
                if (levelOrSealed == null)
                {
                    levelOrSealed = new Bitmap(261, DefaultPicHeight);
                    g = Graphics.FromImage(levelOrSealed);
                }
                picHeight += 13;
                TextRenderer.DrawText(g, "Equipment Level Attributes", GearGraphics.EquipDetailFont, new Point(261, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
                picHeight += 15;
                if (Gear.FixLevel)
                {
                    TextRenderer.DrawText(g, "[Fixed Levels]", GearGraphics.EquipDetailFont, new Point(261, picHeight), ((SolidBrush)GearGraphics.OrangeBrush).Color, TextFormatFlags.HorizontalCenter);
                    picHeight += 15;
                }

                for (int i = 0; i < Gear.Levels.Count; i++)
                {
                    var info = Gear.Levels[i];
                    TextRenderer.DrawText(g, "Level " + info.Level + (i >= Gear.Levels.Count - 1 ? "(MAX)" : null), GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
                    picHeight += 15;
                    foreach (var kv in info.BonusProps)
                    {
                        GearLevelInfo.Range range = kv.Value;

                        string propString = ItemStringHelper.GetGearPropString(kv.Key, kv.Value.Min);
                        if (propString != null)
                        {
                            if (range.Max != range.Min)
                            {
                                propString += " ~ " + kv.Value.Max + (propString.EndsWith("%") ? "%" : null);
                            }
                            TextRenderer.DrawText(g, propString, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                            picHeight += 15;
                        }
                    }
                    if (info.Skills.Count > 0)
                    {
                        string title = string.Format("{2:P2}({0}/{1}) chance to gain :", info.Prob, info.ProbTotal, info.Prob * 1.0 / info.ProbTotal);
                        TextRenderer.DrawText(g, title, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                        foreach (var kv in info.Skills)
                        {
                            StringResult sr = null;
                            if (this.StringLinker != null)
                            {
                                this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr);
                            }
                            string text = string.Format("{0}({1}) +{2} Level", sr == null ? null : sr.Name, kv.Key, kv.Value);
                            TextRenderer.DrawText(g, text, GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.OrangeBrush).Color, TextFormatFlags.NoPadding);
                            picHeight += 15;
                        }
                    }
                    if (info.EquipmentSkills.Count > 0)
                    {
                        string title;
                        if (info.Prob < info.ProbTotal)
                        {
                            title = string.Format("{2:P2} chance to gain :", info.Prob, info.ProbTotal, info.Prob * 1.0 / info.ProbTotal);
                        }
                        else
                        {
                            title = "Acquired Skills When Equipped :";
                        }
                        g.DrawString(title, GearGraphics.ItemDetailFont, Brushes.White, 10, picHeight);
                        picHeight += 15;
                        foreach (var kv in info.EquipmentSkills)
                        {
                            StringResult sr = null;
                            if (this.StringLinker != null)
                            {
                                this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr);
                            }
                            string text = string.Format("{0}({1}) Lv.{2}", sr == null ? null : sr.Name, kv.Key, kv.Value);
                            TextRenderer.DrawText(g, title, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                            picHeight += 15;
                        }
                    }
                    if (info.Exp > 0)
                    {
                        TextRenderer.DrawText(g, "Growth EXP: " + info.Exp + "%", GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                    }

                    picHeight += 2;
                }
            }

            if (Gear.Seals != null)
            {
                if (levelOrSealed == null)
                {
                    levelOrSealed = new Bitmap(261, DefaultPicHeight);
                    g = Graphics.FromImage(levelOrSealed);
                }
                picHeight += 13;
                TextRenderer.DrawText(g, "Seal Release Properties", GearGraphics.EquipDetailFont, new Point(261, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
                picHeight += 15;
                for (int i = 0; i < Gear.Seals.Count; i++)
                {
                    var info = Gear.Seals[i];

                    TextRenderer.DrawText(g, "Level " + info.Level + (i >= Gear.Seals.Count - 1 ? "(MAX)" : null), GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
                    picHeight += 15;
                    var props = this.IsCombineProperties ? Gear.CombineProperties(info.BonusProps) : info.BonusProps;
                    foreach (var kv in props)
                    {
                        string propString = ItemStringHelper.GetGearPropString(kv.Key, kv.Value);
                        TextRenderer.DrawText(g, propString, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                    }
                    if (info.HasIcon)
                    {
                        Bitmap icon = info.Icon.Bitmap ?? info.IconRaw.Bitmap;
                        if (icon != null)
                        {
                            TextRenderer.DrawText(g, "Level: ", GearGraphics.EquipDetailFont, new Point(10, picHeight + icon.Height / 2 - 6), Color.White, TextFormatFlags.NoPadding);
                            g.DrawImage(icon, 52, picHeight);
                            picHeight += icon.Height;
                        }
                    }
                    if (info.Exp > 0)
                    {
                        TextRenderer.DrawText(g, "Growth EXP: " + info.Exp + "%", GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                    }
                    picHeight += 2;
                }
            }


            format.Dispose();
            if (g != null)
            {
                g.Dispose();
                picHeight += 13;
            }
            return levelOrSealed;
        }

        private void FillRect(Graphics g, TextureBrush brush, int x, int y0, int y1)
        {
            brush.ResetTransform();
            brush.TranslateTransform(x, y0);
            g.FillRectangle(brush, x, y0, brush.Image.Width, y1 - y0);
        }

        private List<string> GetGearAttributeString()
        {
            int value;
            List<string> tags = new List<string>();

            if (Gear.Props.TryGetValue(GearPropType.only, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.only, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.tradeBlock, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.tradeBlock, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.abilityTimeLimited, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.abilityTimeLimited, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.equipTradeBlock, out value) && value != 0)
            {
                if (Gear.State == GearState.itemList)
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
            if (Gear.Props.TryGetValue(GearPropType.accountSharable, out value) && value != 0)
            {
                int value2;
                if (Gear.Props.TryGetValue(GearPropType.sharableOnce, out value2) && value2 != 0)
                {
                    tags.Add(ItemStringHelper.GetGearPropString(GearPropType.sharableOnce, value2));
                }
                else
                {
                    tags.Add(ItemStringHelper.GetGearPropString(GearPropType.accountSharable, value));
                }
            }
            if (Gear.Props.TryGetValue(GearPropType.blockGoldHammer, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.blockGoldHammer, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.noPotential, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.noPotential, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.fixedPotential, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.fixedPotential, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.onlyEquip, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.onlyEquip, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.notExtend, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.notExtend, value));
            }

            return tags;
        }

        private Bitmap GetAlienStoneIcon()
        {
            if (Gear.AlienStoneSlot == null)
            {
                return Resource.ToolTip_Equip_AlienStone_Empty;
            }
            else
            {
                switch (Gear.AlienStoneSlot.Grade)
                {
                    case AlienStoneGrade.Normal:
                        return Resource.ToolTip_Equip_AlienStone_Normal;
                    case AlienStoneGrade.Rare:
                        return Resource.ToolTip_Equip_AlienStone_Rare;
                    case AlienStoneGrade.Epic:
                        return Resource.ToolTip_Equip_AlienStone_Epic;
                    case AlienStoneGrade.Unique:
                        return Resource.ToolTip_Equip_AlienStone_Unique;
                    case AlienStoneGrade.Legendary:
                        return Resource.ToolTip_Equip_AlienStone_Legendary;
                    default:
                        return null;
                }
            }
        }

        private void DrawGearReq(Graphics g, int x, int y)
        {
            int value;
            bool can;
            NumberType type;
            Size size;
            //需求等级
            this.Gear.Props.TryGetValue(GearPropType.reqLevel, out value);
            {
                int reduceReq;
                if (this.Gear.Props.TryGetValue(GearPropType.reduceReq, out reduceReq))
                {
                    value = Math.Max(0, value - reduceReq);
                }
            }
            can = this.charStat == null || this.charStat.Level >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqLEV", out size), x, y);
            DrawReqNum(g, value.ToString().PadLeft(3), (type == NumberType.Can ? NumberType.YellowNumber : type), x + 54, y, StringAlignment.Near);

            //需求人气
            this.Gear.Props.TryGetValue(GearPropType.reqPOP, out value);
            can = this.charStat == null || this.charStat.Pop >= value;
            type = GetReqType(can, value);
            if (value > 0)
            {
                g.DrawImage(FindReqImage(type, "reqPOP", out size), x + 80, y);
                DrawReqNum(g, value.ToString("D3"), type, x + 80 + 54, y, StringAlignment.Near);
            }

            y += 15;

            //需求力量
            this.Gear.Props.TryGetValue(GearPropType.reqSTR, out value);
            can = this.charStat == null || this.charStat.Strength.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqSTR", out size), x, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 54, y, StringAlignment.Near);


            //需求运气
            this.Gear.Props.TryGetValue(GearPropType.reqLUK, out value);
            can = this.charStat == null || this.charStat.Luck.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqLUK", out size), x + 80, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 80 + 54, y, StringAlignment.Near);

            y += 9;

            //需求敏捷
            this.Gear.Props.TryGetValue(GearPropType.reqDEX, out value);
            can = this.charStat == null || this.charStat.Dexterity.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqDEX", out size), x, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 54, y, StringAlignment.Near);

            //需求智力
            this.Gear.Props.TryGetValue(GearPropType.reqINT, out value);
            can = this.charStat == null || this.charStat.Intelligence.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqINT", out size), x + 80, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 80 + 54, y, StringAlignment.Near);
        }

        private void DrawPropDiffEx(Graphics g, int x, int y)
        {
            int value;
            string numValue;
            //防御
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_pdd, x, y);
            x += 62;
            DrawReqNum(g, "0", NumberType.LookAhead, x - 5, y + 6, StringAlignment.Far);

            ////魔防
            //g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_mdd, x, y);
            //x += 62;
            //DrawReqNum(g, "0", NumberType.LookAhead, x - 5, y + 6, StringAlignment.Far);

            //boss伤

            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_bdr, x, y);
            x += 62;
            this.Gear.Props.TryGetValue(GearPropType.bdR, out value);
            numValue = (value > 0 ? "+ " : null) + value + " % ";
            DrawReqNum(g, numValue, NumberType.LookAhead, x - 5 + 3, y + 6, StringAlignment.Far);

            //无视防御
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_igpddr, x, y);
            x += 62;
            this.Gear.Props.TryGetValue(GearPropType.imdR, out value);
            numValue = (value > 0 ? "+ " : null) + value + " % ";
            DrawReqNum(g, numValue, NumberType.LookAhead, x - 5 - 1, y + 6, StringAlignment.Far);
        }

        private void DrawJobReq(Graphics g, ref int picH)
        {
            int value;
            string extraReq = ItemStringHelper.GetExtraJobReqString(Gear.type) ??
                (Gear.Props.TryGetValue(GearPropType.reqSpecJob, out value) ? ItemStringHelper.GetExtraJobReqString(value) : null);
            Image jobImage = extraReq == null ? Resource.UIToolTip_img_Item_Equip_Job_normal : Resource.UIToolTip_img_Item_Equip_Job_expand;
            g.DrawImage(jobImage, 12, picH);

            int reqJob;
            Gear.Props.TryGetValue(GearPropType.reqJob, out reqJob);
            int[] origin = new int[] { 14, 7, 56, 7, 95, 7, 135, 7, 166, 10, 198, 10 };
            int[] origin2 = new int[] { 10, 6, 44, 6, 79, 6, 126, 6, 166, 6, 201, 6 };
            for (int i = 0; i <= 5; i++)
            {
                bool enable;
                if (i == 0)
                {
                    enable = reqJob <= 0;
                    if (reqJob == 0) reqJob = 0x1f;//0001 1111
                    if (reqJob == -1) reqJob = 0; //0000 0000
                }
                else
                {
                    enable = (reqJob & (1 << (i - 1))) != 0;
                }
                if (enable)
                {
                    enable = this.charStat == null || Character.CheckJobReq(this.charStat.Job, i);
                    Image jobImage2 = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_Job_" + (enable ? "enable" : "disable") + "_" + i.ToString()) as Image;
                    if (jobImage != null)
                    {
                        if (enable)
                            g.DrawImage(jobImage2, 12 + origin[i * 2], picH + origin[i * 2 + 1]);
                        else
                            g.DrawImage(jobImage2, 12 + origin2[i * 2], picH + origin2[i * 2 + 1]);
                    }
                }
            }
            if (extraReq != null)
            {
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                TextRenderer.DrawText(g, extraReq, GearGraphics.EquipDetailFont, new Point(261, picH + 24), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.HorizontalCenter);
                format.Dispose();
            }
            picH += jobImage.Height + 9;
        }

        private Image FindReqImage(NumberType type, string req, out Size size)
        {
            Image image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + req) as Image;
            if (image != null)
                size = image.Size;
            else
                size = Size.Empty;
            return image;
        }

        private void DrawStar(Graphics g, ref int picH)
        {
            if (Gear.Star > 0)
            {
                int totalWidth = Gear.Star * 10 + (Gear.Star / 5 - 1) * 6;
                int dx = 130 - totalWidth / 2;
                for (int i = 0; i < Gear.Star; i++)
                {
                    g.DrawImage(Resource.UIToolTip_img_Item_Equip_Star_Star, dx, picH);
                    dx += 10;
                    if (i > 0 && i % 5 == 4)
                    {
                        dx += 6;
                    }
                }
                picH += 18;
            }
        }

        private void DrawStar2(Graphics g, ref int picH)
        {
            int maxStar = Gear.GetMaxStar();
            if (maxStar > 0)
            {
                for (int i = 0; i < maxStar; i += 15)
                {
                    int starLine = Math.Min(maxStar - i, 15);
                    int totalWidth = starLine * 10 + (starLine / 5 - 1) * 6;
                    int dx = 130 - totalWidth / 2;
                    for (int j = 0; j < starLine; j++)
                    {
                        g.DrawImage((i + j < Gear.Star) ?
                            Resource.UIToolTip_img_Item_Equip_Star_Star : Resource.UIToolTip_img_Item_Equip_Star_Star0,
                            dx, picH);
                        dx += 10;
                        if (j > 0 && j % 5 == 4)
                        {
                            dx += 6;
                        }
                    }
                    picH += 18;
                }
                picH -= 1;
            }
        }

        private NumberType GetReqType(bool can, int reqValue)
        {
            if (reqValue <= 0)
                return NumberType.Disabled;
            if (can)
                return NumberType.Can;
            else
                return NumberType.Cannot;
        }

        private void DrawReqNum(Graphics g, string numString, NumberType type, int x, int y, StringAlignment align)
        {
            if (g == null || numString == null || align == StringAlignment.Center)
                return;
            int spaceWidth = type == NumberType.LookAhead ? 3 : 6;
            bool near = align == StringAlignment.Near;

            for (int i = 0; i < numString.Length; i++)
            {
                char c = near ? numString[i] : numString[numString.Length - i - 1];
                Image image = null;
                Point origin = Point.Empty;
                switch (c)
                {
                    case ' ':
                        break;
                    case '+':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "plus") as Image;
                        break;
                    case '-':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "minus") as Image;
                        origin.Y = 3;
                        break;
                    case '%':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "percent") as Image;
                        break;
                    default:
                        if ('0' <= c && c <= '9')
                        {
                            image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + c) as Image;
                            if (c == '1' && type == NumberType.LookAhead)
                            {
                                origin.X = 1;
                            }
                        }
                        break;
                }

                if (image != null)
                {
                    if (near)
                    {
                        g.DrawImage(image, x + origin.X, y + origin.Y);
                        x += image.Width + origin.X + 1;
                    }
                    else
                    {
                        x -= image.Width + origin.X;
                        g.DrawImage(image, x + origin.X, y + origin.Y);
                        x -= 1;
                    }
                }
                else //空格补位
                {
                    x += spaceWidth * (near ? 1 : -1);
                }
            }
        }

        private Image GetAdditionalOptionIcon(GearGrade grade)
        {
            switch (grade)
            {
                default:
                case GearGrade.B: return Resource.AdditionalOptionTooltip_rare;
                case GearGrade.A: return Resource.AdditionalOptionTooltip_epic;
                case GearGrade.S: return Resource.AdditionalOptionTooltip_unique;
                case GearGrade.SS: return Resource.AdditionalOptionTooltip_legendary;
            }
        }

        private bool TryGetMedalResource(int medalTag, out Wz_Node resNode)
        {
            resNode = PluginBase.PluginManager.FindWz("UI/NameTag.img/medal/" + medalTag);
            return resNode != null;
        }

        private enum NumberType
        {
            Can,
            Cannot,
            Disabled,
            LookAhead,
            YellowNumber,
        }
    }
}

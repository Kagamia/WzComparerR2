using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Drawing2D;
using Resource = CharaSimResource.Resource;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class ItemTooltipRender2 : TooltipRender
    {
        public ItemTooltipRender2()
        {
        }

        private Item item;

        public Item Item
        {
            get { return item; }
            set { item = value; }
        }

        public override object TargetItem
        {
            get
            {
                return this.item;
            }
            set
            {
                this.item = value as Item;
            }
        }


        public bool LinkRecipeInfo { get; set; }
        public bool LinkRecipeItem { get; set; }
        public bool ShowNickTag { get; set; }

        public TooltipRender LinkRecipeInfoRender { get; set; }
        public TooltipRender LinkRecipeGearRender { get; set; }
        public TooltipRender LinkRecipeItemRender { get; set; }
        public TooltipRender SetItemRender { get; set; }

        public override Bitmap Render()
        {
            if (this.item == null)
            {
                return null;
            }
            //绘制道具
            int picHeight;
            Bitmap itemBmp = RenderItem(out picHeight);
            Bitmap recipeInfoBmp = null;
            Bitmap recipeItemBmp = null;
            Bitmap setItemBmp = null;

            //图纸相关
            if (this.item.Specs.TryGetValue(ItemSpecType.recipe, out long recipeID))
            {
                long recipeSkillID = recipeID / 10000;
                Recipe recipe = null;
                //寻找配方
                Wz_Node recipeNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\Recipe_{0}.img\{1}", recipeSkillID, recipeID));
                if (recipeNode != null)
                {
                    recipe = Recipe.CreateFromNode(recipeNode);
                }
                //生成配方图像
                if (recipe != null)
                {
                    if (this.LinkRecipeInfo)
                    {
                        recipeInfoBmp = RenderLinkRecipeInfo(recipe);
                    }

                    if (this.LinkRecipeItem)
                    {
                        int itemID = recipe.MainTargetItemID;
                        int itemIDClass = itemID / 1000000;
                        if (itemIDClass == 1) //通过ID寻找装备
                        {
                            Wz_Node charaWz = PluginManager.FindWz(Wz_Type.Character);
                            if (charaWz != null)
                            {
                                string imgName = itemID.ToString("d8")+".img";
                                foreach (Wz_Node node0 in charaWz.Nodes)
                                {
                                    Wz_Node imgNode = node0.FindNodeByPath(imgName, true);
                                    if (imgNode != null)
                                    {
                                        Gear gear = Gear.CreateFromNode(imgNode, path=>PluginManager.FindWz(path));
                                        if (gear != null)
                                        {
                                            recipeItemBmp = RenderLinkRecipeGear(gear);
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                        else if (itemIDClass >= 2 && itemIDClass <= 5) //通过ID寻找道具
                        {
                            Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item);
                            if (itemWz != null)
                            {
                                string imgClass = (itemID / 10000).ToString("d4") + ".img\\"+itemID.ToString("d8");
                                foreach (Wz_Node node0 in itemWz.Nodes)
                                {
                                    Wz_Node imgNode = node0.FindNodeByPath(imgClass, true);
                                    if (imgNode != null)
                                    {
                                        Item item = Item.CreateFromNode(imgNode, PluginManager.FindWz);
                                        if (item != null)
                                        {
                                            recipeItemBmp = RenderLinkRecipeItem(item);
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (this.item.Props.TryGetValue(ItemPropType.setItemID, out long setID))
            {
                SetItem setItem;
                if (CharaSimLoader.LoadedSetItems.TryGetValue((int)setID, out setItem))
                {
                    setItemBmp = RenderSetItem(setItem);
                }
            }

            //计算布局
            Size totalSize = new Size(itemBmp.Width, picHeight);
            Point recipeInfoOrigin = Point.Empty;
            Point recipeItemOrigin = Point.Empty;
            Point setItemOrigin = Point.Empty;

            if (recipeItemBmp != null)
            {
                recipeItemOrigin.X = totalSize.Width;
                totalSize.Width += recipeItemBmp.Width;

                if (recipeInfoBmp != null)
                {
                    recipeInfoOrigin.X = itemBmp.Width - recipeInfoBmp.Width;
                    recipeInfoOrigin.Y = picHeight;
                    totalSize.Height = Math.Max(picHeight + recipeInfoBmp.Height, recipeItemBmp.Height);
                }
                else
                {
                    totalSize.Height = Math.Max(picHeight, recipeItemBmp.Height);
                }
            }
            else if (recipeInfoBmp != null)
            {
                totalSize.Width += recipeInfoBmp.Width;
                totalSize.Height = Math.Max(picHeight, recipeInfoBmp.Height);
                recipeInfoOrigin.X = itemBmp.Width;
            }
            if (setItemBmp != null)
            {
                setItemOrigin = new Point(totalSize.Width, 0);
                totalSize.Width += setItemBmp.Width;
                totalSize.Height = Math.Max(totalSize.Height, setItemBmp.Height);
            }

            //开始绘制
            Bitmap tooltip = new Bitmap(totalSize.Width, totalSize.Height);
            Graphics g = Graphics.FromImage(tooltip);

            if (itemBmp != null)
            {
                //绘制背景区域
                GearGraphics.DrawNewTooltipBack(g, 0, 0, itemBmp.Width, picHeight);
                //复制图像
                g.DrawImage(itemBmp, 0, 0, new Rectangle(0, 0, itemBmp.Width, picHeight), GraphicsUnit.Pixel);
                //左上角
                g.DrawImage(Resource.UIToolTip_img_Item_Frame2_cover, 3, 3);

                if (this.ShowObjectID)
                {
                    GearGraphics.DrawGearDetailNumber(g, 3, 3, item.ItemID.ToString("d8"), true);
                }
            }

            //绘制配方
            if (recipeInfoBmp != null)
            {
                g.DrawImage(recipeInfoBmp, recipeInfoOrigin.X, recipeInfoOrigin.Y,
                    new Rectangle(Point.Empty, recipeInfoBmp.Size), GraphicsUnit.Pixel);
            }

            //绘制产出道具
            if (recipeItemBmp != null)
            {
                g.DrawImage(recipeItemBmp, recipeItemOrigin.X, recipeItemOrigin.Y,
                    new Rectangle(Point.Empty, recipeItemBmp.Size), GraphicsUnit.Pixel);
            }

            //绘制套装
            if (setItemBmp != null)
            {
                g.DrawImage(setItemBmp, setItemOrigin.X, setItemOrigin.Y,
                    new Rectangle(Point.Empty, setItemBmp.Size), GraphicsUnit.Pixel);
            }

            if (itemBmp != null)
                itemBmp.Dispose();
            if (recipeInfoBmp != null)
                recipeInfoBmp.Dispose();
            if (recipeItemBmp != null)
                recipeItemBmp.Dispose();
            if (setItemBmp != null)
                setItemBmp.Dispose();

            g.Dispose();
            return tooltip;
        }


        private Bitmap RenderItem(out int picH)
        {
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            long value;

            //物品标题
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringItem.TryGetValue(item.ItemID, out sr))
            {
                sr = new StringResult();
                sr.Name = "(null)";
            }

            // calculate image width
            const int DefualtWidth = 290;
            int tooltipWidth = DefualtWidth;

            if (int.TryParse(sr["fixWidth"], out int fixWidth) && fixWidth > 0)
            {
                tooltipWidth = fixWidth;
            }
            else
            {
                using (Bitmap dummyImg = new Bitmap(1, 1))
                using (Graphics tempG = Graphics.FromImage(dummyImg))
                {
                    SizeF titleSize = tempG.MeasureString(sr.Name, GearGraphics.ItemNameFont2, short.MaxValue, format);
                    titleSize.Width += 12 * 2;
                    if (titleSize.Width > DefualtWidth)
                    {
                        tooltipWidth = (int)Math.Ceiling(titleSize.Width);
                    }
                }
            }

            Bitmap tooltip = new Bitmap(tooltipWidth, DefaultPicHeight);
            Graphics g = Graphics.FromImage(tooltip);
            picH = 10;

            //绘制标题
            bool hasPart2 = false;
            format.Alignment = StringAlignment.Center;
            g.DrawString(sr.Name, GearGraphics.ItemNameFont2, Brushes.White, tooltip.Width / 2, picH, format);
            picH += 22;

            //额外特性
            string attr = GetItemAttributeString();
            if (!string.IsNullOrEmpty(attr))
            {
                g.DrawString(attr, GearGraphics.ItemDetailFont, GearGraphics.GearNameBrushC, tooltip.Width / 2, picH, format);
                if (attr.Contains("\n")) {
                    picH += 31;
                }
                else
                {
                    picH += 19;
                }
                hasPart2 = true;
            }

            string expireTime = null;
            if (item.Props.TryGetValue(ItemPropType.permanent, out value) && value != 0)
            {
                expireTime = ItemStringHelper.GetItemPropString(ItemPropType.permanent, value);
            }
            else if (item.Props.TryGetValue(ItemPropType.life, out value) && value > 0)
            {
                DateTime time = DateTime.Now.AddDays(value);
                expireTime = time.ToString("魔法时间：到 yyyy年 M月 d日 H时");
            }
            if (!string.IsNullOrEmpty(expireTime))
            {
                g.DrawString(expireTime, GearGraphics.ItemDetailFont, Brushes.White, tooltip.Width / 2, picH, format);
                picH += 16;
                hasPart2 = true;
            }

            if (hasPart2)
            {
                picH += 1;
            }

            //装备限时
            if (item.TimeLimited)
            {
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr = time.ToString("到yyyy年 M月 d日 H时 m分可以用");
                g.DrawString(expireStr, GearGraphics.ItemDetailFont, Brushes.White, tooltip.Width / 2, picH, format);
                picH += 21;
            }

            //绘制图标
            int iconY = picH;
            int iconX = 14;
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_base, iconX, picH);
            if (item.Icon.Bitmap != null)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(item.Icon.Bitmap),
                iconX + 6 + (1 - item.Icon.Origin.X) * 2,
                picH + 6 + (33 - item.Icon.Origin.Y) * 2);
            }
            if (item.Cash)
            {
                Bitmap cashImg = null;

                if (item.Props.TryGetValue(ItemPropType.wonderGrade, out value) && value > 0)
                {
                    string resKey = $"CashShop_img_CashItem_label_{value + 3}";
                    cashImg = Resource.ResourceManager.GetObject(resKey) as Bitmap;
                }
                if (cashImg == null) //default cashImg
                {
                    cashImg = Resource.CashItem_0;
                }

                g.DrawImage(GearGraphics.EnlargeBitmap(cashImg),
                    iconX + 6 + 68 - 26,
                    picH + 6 + 68 - 26);
            }
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_cover, iconX + 4, picH + 4); //绘制左上角cover

            if (item.Props.TryGetValue(ItemPropType.reqLevel, out value))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawString("要求等级 :" + value, GearGraphics.ItemReqLevelFont, Brushes.White, 97, picH);
                picH += 15;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            }
            else
            {
                picH += 3;
            }

            int right = tooltip.Width - 18;

            string desc = null;
            if (item.Level > 0)
            {
                desc += $"[LV.{item.Level}] ";
            }
            desc += sr.Desc;
            if (!string.IsNullOrEmpty(desc))
            {
                GearGraphics.DrawString(g, desc, GearGraphics.ItemDetailFont2, 100, right, ref picH, 16);
            }
            if (!string.IsNullOrEmpty(sr.AutoDesc))
            {
                GearGraphics.DrawString(g, sr.AutoDesc, GearGraphics.ItemDetailFont2, 100, right, ref picH, 16);
            }
            if (item.Props.TryGetValue(ItemPropType.tradeAvailable, out value) && value > 0)
            {
                attr = ItemStringHelper.GetItemPropString(ItemPropType.tradeAvailable, value);
                if (!string.IsNullOrEmpty(attr))
                    GearGraphics.DrawString(g, "#c" + attr + "#", GearGraphics.ItemDetailFont2, 100, right, ref picH, 16);
            }
            if (item.Specs.TryGetValue(ItemSpecType.recipeValidDay, out value) && value > 0)
            {
                GearGraphics.DrawString(g, "(可制作时间：" + value + "天)", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }
            if (item.Specs.TryGetValue(ItemSpecType.recipeUseCount, out value) && value > 0)
            {
                GearGraphics.DrawString(g, "(可制作次数：" + value + "次)", GearGraphics.ItemDetailFont, 100, right, ref picH, 16);
            }

            picH += 3;

            Wz_Node nickResNode = null;
            bool willDrawNickTag = this.ShowNickTag
                && this.Item.Props.TryGetValue(ItemPropType.nickTag, out value)
                && this.TryGetNickResource(value, out nickResNode);
            string descLeftAlign = sr["desc_leftalign"];

            if (!string.IsNullOrEmpty(descLeftAlign) || item.Sample.Bitmap != null || willDrawNickTag)
            {
                if (picH < iconY + 84)
                {
                    picH = iconY + 84;
                }
                if (!string.IsNullOrEmpty(descLeftAlign))
                {
                    picH += 12;
                    GearGraphics.DrawString(g, descLeftAlign, GearGraphics.ItemDetailFont, 14, right, ref picH, 16);
                }
                if (item.Sample.Bitmap != null)
                {
                    g.DrawImage(item.Sample.Bitmap, (tooltip.Width - item.Sample.Bitmap.Width) / 2, picH);
                    picH += item.Sample.Bitmap.Height;
                    picH += 2;
                }
                if (nickResNode != null)
                {
                    //获取称号名称
                    string nickName;
                    string nickWithQR = sr["nickWithQR"];
                    if (nickWithQR != null)
                    {
                        string qrDefault = sr["qrDefault"] ?? string.Empty;
                        nickName = Regex.Replace(nickWithQR, "#qr.*?#", qrDefault);
                    }
                    else
                    {
                        nickName = sr.Name;
                    }
                    GearGraphics.DrawNameTag(g, nickResNode, nickName, tooltip.Width, ref picH);
                    picH += 4;
                }
            }


            //绘制配方需求
            if (item.Specs.TryGetValue(ItemSpecType.recipe, out value))
            {
                long reqSkill, reqSkillLevel;
                if (!item.Specs.TryGetValue(ItemSpecType.reqSkill, out reqSkill))
                {
                    reqSkill = value / 10000 * 10000;
                }

                if (!item.Specs.TryGetValue(ItemSpecType.reqSkillLevel, out reqSkillLevel))
                {
                    reqSkillLevel = 1;
                }

                picH = Math.Max(picH, iconY + 107);
                g.DrawLine(Pens.White, 6, picH, 283, picH);//分割线
                picH += 10;
                g.DrawString("<使用限制条件>", GearGraphics.ItemDetailFont, GearGraphics.SetItemNameBrush, 8, picH);
                picH += 17;

                //技能标题
                if (StringLinker == null || !StringLinker.StringSkill.TryGetValue((int)reqSkill, out sr))
                {
                    sr = new StringResult();
                    sr.Name = "(null)";
                }
                g.DrawString(string.Format("· {0}{1}级以上", sr.Name, reqSkillLevel), GearGraphics.ItemDetailFont, GearGraphics.SetItemNameBrush, 13, picH);
                picH += 16;
                picH += 6;
            }

            picH = Math.Max(iconY + 94, picH + 6);
            return tooltip;
        }

        private string GetItemAttributeString()
        {
            long value;
            List<string> tags = new List<string>();

            if (item.Props.TryGetValue(ItemPropType.quest, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.quest, value));
            }
            if (item.Props.TryGetValue(ItemPropType.pquest, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.pquest, value));
            }
            if (item.Props.TryGetValue(ItemPropType.only, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.only, value));
            }
            if (item.Props.TryGetValue(ItemPropType.tradeBlock, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.tradeBlock, value));
            }
            if (item.Props.TryGetValue(ItemPropType.accountSharable, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.accountSharable, value));
            }
            if (item.Props.TryGetValue(ItemPropType.accountSharableAfterExchange, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.accountSharableAfterExchange, value));
            }
            if (item.Props.TryGetValue(ItemPropType.mintable, out value))
            {
                tags.Add(ItemStringHelper.GetItemPropString(ItemPropType.mintable, value));
            }

            return tags.Count > 0 ? string.Join(", ", tags.ToArray()) : null;
        }

        private Bitmap RenderLinkRecipeInfo(Recipe recipe)
        {
            TooltipRender renderer = this.LinkRecipeInfoRender;
            if (renderer == null)
            {
                RecipeTooltipRender defaultRenderer = new RecipeTooltipRender();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = recipe;
            return renderer.Render();
        }

        private Bitmap RenderLinkRecipeGear(Gear gear)
        {
            TooltipRender renderer = this.LinkRecipeGearRender;
            if (renderer == null)
            {
                GearTooltipRender2 defaultRenderer = new GearTooltipRender2();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = gear;
            return renderer.Render();
        }

        private Bitmap RenderLinkRecipeItem(Item item)
        {
            TooltipRender renderer = this.LinkRecipeItemRender;
            if (renderer == null)
            {
                ItemTooltipRender2 defaultRenderer = new ItemTooltipRender2();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = item;
            return renderer.Render();
        }

        private Bitmap RenderSetItem(SetItem setItem)
        {
            TooltipRender renderer = this.SetItemRender;
            if (renderer == null)
            {
                var defaultRenderer = new SetItemTooltipRender();
                defaultRenderer.StringLinker = this.StringLinker;
                defaultRenderer.ShowObjectID = false;
                renderer = defaultRenderer;
            }

            renderer.TargetItem = setItem;
            return renderer.Render();
        }

        private bool TryGetNickResource(long nickTag, out Wz_Node resNode)
        {
            resNode = PluginBase.PluginManager.FindWz("UI/NameTag.img/nick/" + nickTag);
            return resNode != null;
        }
    }
}

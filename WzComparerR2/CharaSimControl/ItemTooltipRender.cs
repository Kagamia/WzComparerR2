using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using CharaSimResource;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class ItemTooltipRender:TooltipRender
    {
        public ItemTooltipRender()
        {
        }

        private Item item;

        public Item Item
        {
            get { return item; }
            set { item = value; }
        }

        public override Bitmap Render()
        {
            if (this.item == null)
            {
                return null;
            }
            int picHeight, iconY;
            Bitmap originBmp = renderItem(out picHeight, out iconY);
            Bitmap tooltip = new Bitmap(290, picHeight);
            Graphics g = Graphics.FromImage(tooltip);

            //复制图像
            g.FillRectangle(GearGraphics.GearBackBrush, 2, 2, 286, picHeight - 4);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.FillRectangle(GearGraphics.GearIconBackBrush, 14, iconY, 68, 68);
            g.CompositingMode = CompositingMode.SourceOver;
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, 290, picHeight - 2), GraphicsUnit.Pixel);
            //绘制外边框
            g.DrawLines(GearGraphics.GearBackPen, GearGraphics.GetBorderPath(0, 290, picHeight));

            //GearGraphics.DrawGearDetailNumber(g, 2, 2, item.ItemID.ToString("d8"), true);

            g.Dispose();
            return tooltip;
        }

        private Bitmap renderItem(out int picHeight, out int iconY)
        {
            Bitmap tooltip = new Bitmap(290, DefaultPicHeight);
            Graphics g = Graphics.FromImage(tooltip);
            StringFormat format = new StringFormat();
            int value;
            format.Alignment = StringAlignment.Center;
            picHeight = 10;
            iconY = 32;

            //物品标题
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringItem.TryGetValue(item.ItemID, out sr))
            {
                sr = new StringResult();
                sr.Name = "(null)";
            }
            string gearName = sr.Name;
            g.DrawString(gearName, GearGraphics.ItemNameFont, Brushes.White, 145, picHeight, format);//绘制装备名称
            picHeight += 21;
            string attr = GetItemAttributeString();
            if (!string.IsNullOrEmpty(attr))
            {
                g.DrawString(attr, GearGraphics.ItemDetailFont, GearGraphics.GearNameBrushC, 145, picHeight, format);
                iconY += 19;
            }

            //绘制图标
            if (item.Icon.Bitmap != null)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(item.Icon.Bitmap),
                    14 + (1 - item.Icon.Origin.X) * 2,
                    iconY + (33 - item.Icon.Origin.Y) * 2);
            }
            if (item.Cash)
            {
                g.DrawImage(GearGraphics.EnlargeBitmap(Resource.CashItem_0),
                    14 + 68 - 26,
                    iconY + 68 - 26);
            }

            picHeight = iconY;
            if (item.Props.TryGetValue(ItemPropType.reqLevel, out value))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawString("要求等级 : " + value, GearGraphics.ItemReqLevelFont, Brushes.White, 92, picHeight);
                picHeight += 15;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            }
            else
            {
                picHeight += 3;
            }
            if (!string.IsNullOrEmpty(sr.Desc))
            {
                GearGraphics.DrawString(g, sr.Desc + sr.AutoDesc, GearGraphics.ItemDetailFont, 92, 272, ref picHeight, 16);
            }
            if (item.Props.TryGetValue(ItemPropType.tradeAvailable, out value) && value > 0)
            {
                attr = ItemStringHelper.GetItemPropString(ItemPropType.tradeAvailable, value);
                if (!string.IsNullOrEmpty(attr))
                    GearGraphics.DrawString(g, "#c" + attr + "#", GearGraphics.ItemDetailFont, 92, 272, ref picHeight, 16);
            }
            picHeight = Math.Max(iconY + 84, iconY + 16 * (int)Math.Ceiling((picHeight - iconY) / 16.0));
            return tooltip;
        }

        private string GetItemAttributeString()
        {
            int value;
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

            return tags.Count > 0 ? string.Join(", ", tags.ToArray()) : null;
        }
    }
}

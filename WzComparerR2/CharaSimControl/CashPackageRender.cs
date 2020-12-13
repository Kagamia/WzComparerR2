using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Resource = CharaSimResource.Resource;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class CashPackageTooltipRender : TooltipRender
    {
        public CashPackageTooltipRender()
        {
        }

        public CashPackage CashPackage { get; set; }

        public override object TargetItem
        {
            get { return this.CashPackage; }
            set { this.CashPackage = value as CashPackage; }
        }

        public override Bitmap Render()
        {
            int picHeight;
            Bitmap originBmp = RenderCashPackage(out picHeight);
            Bitmap tooltip = new Bitmap(originBmp.Width, picHeight);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制背景区域
            GearGraphics.DrawNewTooltipBack(g, 0, 0, tooltip.Width, tooltip.Height);

            //复制图像
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, tooltip.Width, picHeight), GraphicsUnit.Pixel);

            if (originBmp != null)
                originBmp.Dispose();

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, CashPackage.ItemID.ToString("d8"), true);
            }

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderCashPackage(out int picH)
        {
            Bitmap cashBitmap = new Bitmap(220, DefaultPicHeight);
            Graphics g = Graphics.FromImage(cashBitmap);
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;

            int totalPrice = 0, totalOriginalPrice = 0;
            Commodity commodityPackage = new Commodity();
            if (CharaSimLoader.LoadedCommoditiesByItemId.ContainsKey(CashPackage.ItemID))
                commodityPackage = CharaSimLoader.LoadedCommoditiesByItemId[CashPackage.ItemID];

            SizeF titleSize = TextRenderer.MeasureText(g, CashPackage.name, GearGraphics.ItemNameFont2, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix);
            titleSize.Width += 12 * 2;

            if (titleSize.Width < 240)
                titleSize.Width = 240;
            titleSize.Height = 220;

            for (int i = 0; i < CashPackage.SN.Count; ++i)
            {
                Commodity commodity = CharaSimLoader.LoadedCommoditiesBySN[CashPackage.SN[i]];
                string name = null;

                StringResult sr = null;
                if (StringLinker != null)
                {
                    if (StringLinker.StringEqp.TryGetValue(commodity.ItemId, out sr))
                    {
                        name = sr.Name;
                    }
                    else if (StringLinker.StringItem.TryGetValue(commodity.ItemId, out sr))
                    {
                        name = sr.Name;
                    }
                    else
                    {
                        name = "(null)";
                    }
                }
                if (sr == null)
                {
                    name = "(null)";
                }

                SizeF nameSize = TextRenderer.MeasureText(g, name.Replace(Environment.NewLine, ""), GearGraphics.ItemDetailFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                if (commodity.Bonus == 0)
                    nameSize.Width += 55 + 8;
                else
                    nameSize.Width += 55 + 38 + 8 - 1;
                if (CashPackage.SN.Count < 8)
                {
                    if (nameSize.Width > titleSize.Width)
                        titleSize.Width = nameSize.Width;
                }
                else
                {
                    if (i < CashPackage.SN.Count / 2)
                    {
                        if (nameSize.Width > titleSize.Height)
                            titleSize.Height = nameSize.Width;
                    }
                    else
                    {
                        if (nameSize.Width > titleSize.Width)
                            titleSize.Width = nameSize.Width;
                    }
                }
            }

            if (CashPackage.SN.Count >= 8)
                titleSize.Width += titleSize.Height - 4;

            if (titleSize.Width > 220)
            {
                //重构大小
                g.Dispose();
                cashBitmap.Dispose();

                cashBitmap = new Bitmap((int)Math.Ceiling(titleSize.Width), DefaultPicHeight);
                g = Graphics.FromImage(cashBitmap);
            }

            picH = 10;
            g.DrawString(CashPackage.name, GearGraphics.ItemNameFont2, Brushes.White, cashBitmap.Width / 2, picH, format);
            picH += 14;
            if (commodityPackage.termStart > 0 || commodityPackage.termEnd > 0)
            {
                string term = "< 販賣期間:";
                if (commodityPackage.termStart > 0)
                    term += string.Format(" {0}年{1}月{2}日", commodityPackage.termStart / 1000000, (commodityPackage.termStart / 10000) % 100, (commodityPackage.termStart / 100) % 100);
                if (commodityPackage.termStart > 0 && commodityPackage.termEnd > 0)
                    term += "\n~";
                else
                    term += " ~";
                if (commodityPackage.termEnd > 0)
                    term += string.Format(" {0}年{1}月{2}日", commodityPackage.termEnd / 1000000, (commodityPackage.termEnd / 10000) % 100, (commodityPackage.termEnd / 100) % 100);
                term += " >";
                TextRenderer.DrawText(g, term, GearGraphics.ItemDetailFont2, new Point(cashBitmap.Width, picH), ((SolidBrush)GearGraphics.OrangeBrush4).Color, TextFormatFlags.HorizontalCenter);
                picH += 12 * term.Split('\n').Length;
            }
            if (commodityPackage.Limit > 0)
            {
                string limit = null;
                switch (commodityPackage.Limit)
                {
                    case 2:
                        //首次購買
                        break;
                    case 3:
                        limit = "beanfun帳號";
                        break;
                    case 4:
                        limit = "角色";
                        break;
                    default:
                        limit = commodityPackage.Limit.ToString();
                        break;
                }
                if (limit != null && limit.Length > 0)
                {
                    TextRenderer.DrawText(g, "< " + limit + "只能買一次>", GearGraphics.ItemDetailFont2, new Point(cashBitmap.Width, picH), ((SolidBrush)GearGraphics.OrangeBrush4).Color, TextFormatFlags.HorizontalCenter);
                    picH += 12;
                }
            }
            picH += 19;

            int right = cashBitmap.Width - 18;
            if (CashPackage.desc != null && CashPackage.desc.Length > 0)
                CashPackage.desc += "\n";
            if (CashPackage.onlyCash == 0)
                GearGraphics.DrawString(g, CashPackage.desc , GearGraphics.ItemDetailFont2, 11, right, ref picH, 16);
            
            bool hasLine = false;
            picH -= 4;

            int picH0 = picH, columnLeft = 0, columnRight;
            if (CashPackage.SN.Count < 8)
                columnRight = cashBitmap.Width;
            else
                columnRight = (int)Math.Ceiling(titleSize.Height);

            for(int i = 0; i < CashPackage.SN.Count; ++i)
            {
                if (CashPackage.SN.Count >= 8 && i == (CashPackage.SN.Count + 1) / 2)
                {
                    hasLine = false;
                    picH = picH0;
                    columnLeft = (int)Math.Ceiling(titleSize.Height) - 2;
                    columnRight = cashBitmap.Width;
                }

                if (hasLine)
                {
                    g.DrawImage(Resource.CSDiscount_Line, columnLeft + 13, picH);
                    picH += 1;
                }

                Commodity commodity = CharaSimLoader.LoadedCommoditiesBySN[CashPackage.SN[i]];
                string name = null, info = null, time = null;
                BitmapOrigin IconRaw = new BitmapOrigin();

                StringResult sr = null;
                if (StringLinker != null)
                {
                    Wz_Node iconNode = null;
                    if (StringLinker.StringEqp.TryGetValue(commodity.ItemId, out sr))
                    {
                        name = sr.Name;
                        string[] fullPaths = sr.FullPath.Split('\\');
                        iconNode = PluginBase.PluginManager.FindWz(string.Format(@"Character\{0}\{1:D8}.img\info\iconRaw", String.Join("\\", new List<string>(fullPaths).GetRange(2, fullPaths.Length - 3).ToArray()), commodity.ItemId));
                    }
                    else if (StringLinker.StringItem.TryGetValue(commodity.ItemId, out sr))
                    {
                        name = sr.Name;
                        if (Regex.IsMatch(sr.FullPath, @"^(Cash|Consume|Etc|Ins).img\\.+$"))
                        {
                            string itemType = null;
                            if (Regex.IsMatch(sr.FullPath, @"^Cash.img\\.+$"))
                                itemType = "Cash";
                            else if (Regex.IsMatch(sr.FullPath, @"^Consume.img\\.+$"))
                                itemType = "Consume";
                            else if (Regex.IsMatch(sr.FullPath, @"^Etc.img\\.+$"))
                                itemType = "Etc";
                            else if (Regex.IsMatch(sr.FullPath, @"^Ins.img\\.+$"))
                                itemType = "Install";
                            iconNode = PluginBase.PluginManager.FindWz(string.Format(@"Item\{0}\{1:D4}.img\{2:D8}\info\iconRaw", itemType, commodity.ItemId / 10000, commodity.ItemId));
                        }
                        else if (Regex.IsMatch(sr.FullPath, @"^Pet.img\\.+$"))
                        {
                            iconNode = PluginBase.PluginManager.FindWz(string.Format(@"Item\Pet\{0:D7}.img\info\iconRaw", commodity.ItemId));
                        }
                    }
                    else
                    {
                        name = "(null)";
                    }
                    if (iconNode != null)
                    {
                        IconRaw = BitmapOrigin.CreateFromNode(iconNode, PluginBase.PluginManager.FindWz);
                    }
                }
                if (sr == null)
                {
                    name = "(null)";
                }

                if (commodity.Bonus == 0)
                {
                    if (commodity.Count > 1)
                        info += commodity.Count + "個";
                    if (commodity.originalPrice == 0)
                    {
                        foreach (var commodity2 in CharaSimLoader.LoadedCommoditiesBySN.Values)
                        {
                            if (commodity2.ItemId == commodity.ItemId && commodity2.Count == commodity.Count && commodity2.Period == commodity.Period && commodity2.gameWorld == commodity.gameWorld && commodity2.Price > commodity.originalPrice)
                                commodity.originalPrice = commodity2.Price;
                        }
                        if (commodity.originalPrice == commodity.Price)
                            commodity.originalPrice = 0;
                    }
                    if (commodity.originalPrice > 0)
                    {
                        info += commodity.originalPrice + "樂豆點      ";
                        totalOriginalPrice += commodity.originalPrice;
                    }
                    else
                    {
                        totalOriginalPrice += commodity.Price;
                    }
                    info += commodity.Price + "樂豆點";
                    totalPrice += commodity.Price;
                }
                else
                {
                    info += commodity.Count + "個 ";
                    if (commodity.originalPrice > 0)
                    {
                        info += commodity.originalPrice + "樂豆點";
                        totalOriginalPrice += commodity.originalPrice;
                    }
                    else
                    {
                        info += commodity.Price + "樂豆點";
                        totalOriginalPrice += commodity.Price;
                    }
                }

                if (commodity.Period > 0)
                {
                    time ="使用期限: " + commodity.Period;
                }

                g.DrawImage(Resource.CSDiscount_backgrnd, columnLeft + 13, picH + 12);
                if (IconRaw.Bitmap != null)
                {
                    g.DrawImage(IconRaw.Bitmap, columnLeft + 13 + 1 - IconRaw.Origin.X, picH + 12 + 33 - IconRaw.Origin.Y);
                }
                if (time == null)
                {
                    TextRenderer.DrawText(g, name.TrimEnd(Environment.NewLine.ToCharArray()), GearGraphics.ItemDetailFont, new Point(columnLeft + 55, picH + 17), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    if (commodity.Bonus == 0)
                    {
                        TextRenderer.DrawText(g, info, GearGraphics.ItemDetailFont, new Point(columnLeft + 55, picH + 33), Color.White, TextFormatFlags.NoPadding);
                        if (commodity.originalPrice > 0)
                        {
                            int width = TextRenderer.MeasureText(g, info.Substring(0, info.IndexOf("      ")), GearGraphics.ItemDetailFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                            g.DrawLine(Pens.White, columnLeft + 54, picH + 37 + 4, columnLeft + 54 + width + 1, picH + 37 + 4);
                            g.DrawImage(Resource.CSDiscount_arrow, columnLeft + 50 + width + 10, picH + 36 + 1);
                            DrawDiscountNum(g, "-" + (int)(100 - 100.0 * commodity.Price / commodity.originalPrice) + "%", columnRight - 9, picH + 16, StringAlignment.Far);
                        }
                    }
                    else
                    {
                        TextRenderer.DrawText(g, info, GearGraphics.ItemDetailFont, new Point(columnLeft + 55, picH + 33), Color.Red, TextFormatFlags.NoPadding);
                        g.DrawImage(Resource.CSDiscount_bonus, columnRight - 47, picH + 29);
                    }
                }
                else
                {
                    TextRenderer.DrawText(g, name.Replace(Environment.NewLine, ""), GearGraphics.ItemDetailFont, new Point(columnLeft + 55, picH + 8), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    if (commodity.Bonus == 0)
                    {
                        TextRenderer.DrawText(g, info, GearGraphics.ItemDetailFont, new Point(columnLeft + 55, picH + 24), Color.White, TextFormatFlags.NoPadding);
                        if (commodity.originalPrice > 0)
                        {
                            int width = TextRenderer.MeasureText(g, info.Substring(0, info.IndexOf("      ")), GearGraphics.ItemDetailFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                            g.DrawLine(Pens.White, columnLeft + 55, picH + 24 + 4, columnLeft + 55 + width + 1, picH + 24 + 4);
                            g.DrawImage(Resource.CSDiscount_arrow, columnLeft + 55 + width + 10, picH + 24 + 1);
                            DrawDiscountNum(g, "-" + (int)(100 - 100.0 * commodity.Price / commodity.originalPrice) + "%", columnRight - 9, picH + 7, StringAlignment.Far);
                        }
                    }
                    else
                    {
                        TextRenderer.DrawText(g, info, GearGraphics.ItemDetailFont, new Point(columnLeft + 55, picH + 24), Color.Red, TextFormatFlags.NoPadding);
                        g.DrawImage(Resource.CSDiscount_bonus, columnRight - 47, picH + 20);
                    }
                    TextRenderer.DrawText(g, time, GearGraphics.ItemDetailFont, new Point(columnLeft + 55, picH + 39), Color.White, TextFormatFlags.NoPadding);
                }
                picH += 57;

                hasLine = true;
            }

            g.DrawLine(Pens.White, 13, picH, cashBitmap.Width - 8, picH);
            picH += 11;

            g.DrawImage(Resource.CSDiscount_total, 9, picH + 4);
            if (totalOriginalPrice == totalPrice)
            {
                TextRenderer.DrawText(g, totalPrice + "樂豆點", GearGraphics.ItemDetailFont, new Point(51, picH), Color.White, TextFormatFlags.NoPadding);
            }
            else
            {
                TextRenderer.DrawText(g, totalOriginalPrice + "樂豆點     " + totalPrice + "樂豆點", GearGraphics.ItemDetailFont, new Point(51, picH), Color.White, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, totalOriginalPrice + "樂豆點", GearGraphics.ItemDetailFont, new Point(51, picH), Color.Red, TextFormatFlags.NoPadding);
                g.DrawImage(Resource.CSDiscount_arrow, 50 + TextRenderer.MeasureText(g, totalOriginalPrice + "樂豆點", GearGraphics.ItemDetailFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width + 5, picH + 3);
                DrawDiscountNum(g, "-" + (int)((100 - 100.0 * totalPrice / totalOriginalPrice)) + "%", cashBitmap.Width - 9, picH + 2, StringAlignment.Far);
            }
            picH += 11;

            picH += 13;
            format.Dispose();
            g.Dispose();
            return cashBitmap;
        }

        private void DrawDiscountNum(Graphics g, string numString, int x, int y, StringAlignment align)
        {
            if (g == null || numString == null || align != StringAlignment.Far)
                return;
            bool near = align == StringAlignment.Near;

            for (int i = 0; i < numString.Length; i++)
            {
                char c = near ? numString[i] : numString[numString.Length - i - 1];
                Image image = null;
                Point origin = Point.Empty;
                switch (c)
                {
                    case '-':
                        image = Resource.ResourceManager.GetObject("CSDiscount_w") as Image;
                        break;
                    case '%':
                        image = Resource.ResourceManager.GetObject("CSDiscount_e") as Image;
                        break;
                    default:
                        if ('0' <= c && c <= '9')
                        {
                            image = Resource.ResourceManager.GetObject("CSDiscount_" + c) as Image;
                        }
                        break;
                }

                if (image != null)
                {
                    if (near)
                    {
                        g.DrawImage(image, x + origin.X, y + origin.Y);
                        x += image.Width + origin.X;
                    }
                    else
                    {
                        x -= image.Width + origin.X;
                        g.DrawImage(image, x + origin.X, y + origin.Y);
                    }
                }
            }
        }
    }
}

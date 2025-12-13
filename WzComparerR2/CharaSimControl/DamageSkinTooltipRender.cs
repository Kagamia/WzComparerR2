using System;
using System.Drawing;
using System.Linq;
using WzComparerR2.CharaSim;
using Resource = CharaSimResource.Resource;

namespace WzComparerR2.CharaSimControl
{
    public class DamageSkinTooltipRender : TooltipRender
    {
        public DamageSkinTooltipRender()
        {
        }

        private DamageSkin damageSkin;

        public DamageSkin DamageSkin
        {
            get { return damageSkin; }
            set { damageSkin = value; }
        }


        public override object TargetItem
        {
            get { return this.damageSkin; }
            set { this.damageSkin = value as DamageSkin; }
        }

        public bool UseMiniSize { get; set; }
        public bool AlwaysUseMseaFormat { get; set; }
        public bool DisplayUnitOnSingleLine { get; set; }
        public long DamageSkinNumber { get; set; }

        public override Bitmap Render()
        {
            if (this.damageSkin == null)
            {
                return null;
            }

            Bitmap customSampleNonCritical = GetCustomSample(DamageSkinNumber, UseMiniSize, false);
            Bitmap customSampleCritical = GetCustomSample(DamageSkinNumber, UseMiniSize, true);
            Bitmap extraBitmap = GetExtraEffect();
            Bitmap unitBitmap = null;

            int previewWidth = Math.Max(customSampleNonCritical.Width, customSampleCritical.Width);
            int previewHeight = customSampleNonCritical.Height + customSampleCritical.Height;

            if (extraBitmap != null)
            {
                previewWidth = Math.Max(previewWidth, extraBitmap.Width);
                previewHeight += extraBitmap.Height;
                if (DisplayUnitOnSingleLine)
                {
                    unitBitmap = GetUnit();
                    if (unitBitmap != null)
                    {
                        previewWidth = Math.Max(previewWidth, unitBitmap.Width);
                        previewHeight += unitBitmap.Height;
                    }
                }
            }

            int picH = 10;

            Bitmap tooltip = new Bitmap(previewWidth + 30, previewHeight + 30);
            Graphics g = Graphics.FromImage(tooltip);

            GearGraphics.DrawNewTooltipBack(g, 0, 0, tooltip.Width, tooltip.Height);

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, $"{this.damageSkin.DamageSkinID.ToString()}", true);
            }

            g.DrawImage(customSampleNonCritical, 10, picH, new Rectangle(0, 0, customSampleNonCritical.Width, customSampleNonCritical.Height), GraphicsUnit.Pixel);

            picH += customSampleNonCritical.Height + 5;

            g.DrawImage(customSampleCritical, 10, picH, new Rectangle(0, 0, customSampleCritical.Width, customSampleCritical.Height), GraphicsUnit.Pixel);

            picH += customSampleCritical.Height + 5;

            if (unitBitmap != null)
            {
                g.DrawImage(unitBitmap, 10, picH, new Rectangle(0, 0, unitBitmap.Width, unitBitmap.Height), GraphicsUnit.Pixel);
                picH += unitBitmap.Height + 5;
            }

            if (extraBitmap != null)
            {
                g.DrawImage(extraBitmap, 10, picH, new Rectangle(0, 0, extraBitmap.Width, extraBitmap.Height), GraphicsUnit.Pixel);
            }

            customSampleNonCritical.Dispose();
            customSampleCritical.Dispose();
            g.Dispose();
            return tooltip;
        }

        public Bitmap GetCustomSample(long inputNumber, bool useMiniSize, bool isCritical)
        {
            string numberStr = "";
            if (DisplayUnitOnSingleLine)
            {
                numberStr = inputNumber.ToString();
            }
            else
            {
                switch (damageSkin.CustomType)
                {
                    case "hangul": // CJK Detailed
                        numberStr = ItemStringHelper.ToChineseNumberExpr(inputNumber, true);
                        break;
                    case "hangulUnit": // CJK
                        numberStr = ItemStringHelper.ToChineseNumberExpr(inputNumber);
                        break;
                    case "glUnit": // GMS
                        numberStr = ItemStringHelper.ToThousandsNumberExpr(inputNumber, this.AlwaysUseMseaFormat);
                        break;
                    case "glUnit2": // MSEA
                        numberStr = ItemStringHelper.ToThousandsNumberExpr(inputNumber, true);
                        break;
                    default:
                        if (this.DamageSkin.MiniUnit.Count > 0) // Default to CJK format when units are available
                        {
                            numberStr = ItemStringHelper.ToChineseNumberExpr(inputNumber);
                        }
                        else
                        {
                            numberStr = inputNumber.ToString();
                        }
                        break;
                }
            }


            Bitmap criticalSign = null;
            if (this.damageSkin.BigCriticalDigit.ContainsKey("effect3"))
            {
                criticalSign = this.damageSkin.BigCriticalDigit["effect3"].Bitmap;
            }

            int totalWidth = 0;
            int maxHeight = 0;
            int digitSpacing = isCritical ?
                            (useMiniSize ? this.damageSkin.MiniCriticalDigitSpacing :
                            this.damageSkin.BigCriticalDigitSpacing) :
                            (useMiniSize ? this.damageSkin.MiniDigitSpacing :
                            this.damageSkin.BigDigitSpacing);
            int unitSpacing = isCritical ?
                            (useMiniSize ? this.damageSkin.MiniCriticalUnitSpacing :
                            this.damageSkin.BigCriticalUnitSpacing) :
                            (useMiniSize ? this.damageSkin.MiniUnitSpacing :
                            this.damageSkin.BigUnitSpacing);

            if (isCritical && criticalSign != null)
            {
                totalWidth += criticalSign.Width + unitSpacing;
                maxHeight = Math.Max(maxHeight, criticalSign.Height);
            }

            // Calculate total width and max height
            foreach (char c in numberStr)
            {
                string character = c.ToString();
                switch (character)
                {
                    case "0":
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                    case "8":
                    case "9":
                        totalWidth += isCritical ?
                            (useMiniSize ? this.damageSkin.MiniCriticalDigit[character].Bitmap.Width :
                            this.damageSkin.BigCriticalDigit[character].Bitmap.Width) :
                            (useMiniSize ? this.damageSkin.MiniDigit[character].Bitmap.Width :
                            this.damageSkin.BigDigit[character].Bitmap.Width);
                        totalWidth += digitSpacing;
                        maxHeight = Math.Max(maxHeight, isCritical ?
                            (useMiniSize ? this.damageSkin.MiniCriticalDigit[character].Bitmap.Height :
                            this.damageSkin.BigCriticalDigit[character].Bitmap.Height) :
                            (useMiniSize ? this.damageSkin.MiniDigit[character].Bitmap.Height :
                            this.damageSkin.BigDigit[character].Bitmap.Height));
                        break;

                    case "十":
                    case ".":
                        if (this.damageSkin.BigUnit.ContainsKey("0"))
                        {
                            totalWidth += isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["0"].Bitmap.Width :
                                this.damageSkin.BigCriticalUnit["0"].Bitmap.Width) :
                                (useMiniSize ? this.damageSkin.MiniUnit["0"].Bitmap.Width :
                                this.damageSkin.BigUnit["0"].Bitmap.Width);
                            totalWidth += unitSpacing;
                            maxHeight = Math.Max(maxHeight, isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["0"].Bitmap.Height :
                                this.damageSkin.BigCriticalUnit["0"].Bitmap.Height) :
                                (useMiniSize ? this.damageSkin.MiniUnit["0"].Bitmap.Height :
                                this.damageSkin.BigUnit["0"].Bitmap.Height));
                        }
                        break;

                    case "百":
                    case "K":
                        if (this.damageSkin.BigUnit.ContainsKey("1"))
                        {
                            totalWidth += isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["1"].Bitmap.Width :
                                this.damageSkin.BigCriticalUnit["1"].Bitmap.Width) :
                                (useMiniSize ? this.damageSkin.MiniUnit["1"].Bitmap.Width :
                                this.damageSkin.BigUnit["1"].Bitmap.Width);
                            totalWidth += unitSpacing;
                            maxHeight = Math.Max(maxHeight, isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["1"].Bitmap.Height :
                                this.damageSkin.BigCriticalUnit["1"].Bitmap.Height) :
                                (useMiniSize ? this.damageSkin.MiniUnit["1"].Bitmap.Height :
                                this.damageSkin.BigUnit["1"].Bitmap.Height));
                        }
                        break;

                    case "千":
                    case "M":
                        if (this.damageSkin.BigUnit.ContainsKey("2"))
                        {
                            totalWidth += isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["2"].Bitmap.Width :
                                this.damageSkin.BigCriticalUnit["2"].Bitmap.Width) :
                                (useMiniSize ? this.damageSkin.MiniUnit["2"].Bitmap.Width :
                                this.damageSkin.BigUnit["2"].Bitmap.Width);
                            totalWidth += unitSpacing;
                            maxHeight = Math.Max(maxHeight, isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["2"].Bitmap.Height :
                                this.damageSkin.BigCriticalUnit["2"].Bitmap.Height) :
                                (useMiniSize ? this.damageSkin.MiniUnit["2"].Bitmap.Height :
                                this.damageSkin.BigUnit["2"].Bitmap.Height));
                        }
                        break;

                    case "万":
                    case "B":
                        if (this.damageSkin.BigUnit.ContainsKey("3"))
                        {
                            totalWidth += isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["3"].Bitmap.Width :
                                this.damageSkin.BigCriticalUnit["3"].Bitmap.Width) :
                                (useMiniSize ? this.damageSkin.MiniUnit["3"].Bitmap.Width :
                                this.damageSkin.BigUnit["3"].Bitmap.Width);
                            totalWidth += unitSpacing;
                            maxHeight = Math.Max(maxHeight, isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["3"].Bitmap.Height :
                                this.damageSkin.BigCriticalUnit["3"].Bitmap.Height) :
                                (useMiniSize ? this.damageSkin.MiniUnit["3"].Bitmap.Height :
                                this.damageSkin.BigUnit["3"].Bitmap.Height));
                        }
                        break;

                    case "亿":
                    case "T":
                        if (this.damageSkin.BigUnit.ContainsKey("4"))
                        {
                            totalWidth += isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["4"].Bitmap.Width :
                                this.damageSkin.BigCriticalUnit["4"].Bitmap.Width) :
                                (useMiniSize ? this.damageSkin.MiniUnit["4"].Bitmap.Width :
                                this.damageSkin.BigUnit["4"].Bitmap.Width);
                            totalWidth += unitSpacing;
                            maxHeight = Math.Max(maxHeight, isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["4"].Bitmap.Height :
                                this.damageSkin.BigCriticalUnit["4"].Bitmap.Height) :
                                (useMiniSize ? this.damageSkin.MiniUnit["4"].Bitmap.Height :
                                this.damageSkin.BigUnit["4"].Bitmap.Height));
                        }
                        break;


                    case "兆":
                    case "Q":
                        if (this.damageSkin.BigUnit.ContainsKey("5"))
                        {
                            totalWidth += isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["5"].Bitmap.Width :
                                this.damageSkin.BigCriticalUnit["5"].Bitmap.Width) :
                                (useMiniSize ? this.damageSkin.MiniUnit["5"].Bitmap.Width :
                                this.damageSkin.BigUnit["5"].Bitmap.Width);
                            totalWidth += unitSpacing;
                            maxHeight = Math.Max(maxHeight, isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["5"].Bitmap.Height :
                                this.damageSkin.BigCriticalUnit["5"].Bitmap.Height) :
                                (useMiniSize ? this.damageSkin.MiniUnit["5"].Bitmap.Height :
                                this.damageSkin.BigUnit["5"].Bitmap.Height));
                        }
                        break;

                    case "京":
                        if (this.damageSkin.BigUnit.ContainsKey("6"))
                        {
                            totalWidth += isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["6"].Bitmap.Width :
                                this.damageSkin.BigCriticalUnit["6"].Bitmap.Width) :
                                (useMiniSize ? this.damageSkin.MiniUnit["6"].Bitmap.Width :
                                this.damageSkin.BigUnit["6"].Bitmap.Width);
                            totalWidth += unitSpacing;
                            maxHeight = Math.Max(maxHeight, isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalUnit["6"].Bitmap.Height :
                                this.damageSkin.BigCriticalUnit["6"].Bitmap.Height) :
                                (useMiniSize ? this.damageSkin.MiniUnit["6"].Bitmap.Height :
                                this.damageSkin.BigUnit["6"].Bitmap.Height));
                        }
                        break;
                }
            }

            Bitmap finalBitmap = new Bitmap(totalWidth, maxHeight);

            using (Graphics g = Graphics.FromImage(finalBitmap))
            {
                g.Clear(Color.Transparent);
                int offsetX = 0;
                if (isCritical && criticalSign != null)
                {
                    g.DrawImage(criticalSign, offsetX, 0);
                    offsetX += criticalSign.Width;
                }
                foreach (char c in numberStr)
                {
                    string character = c.ToString();
                    Bitmap charBitmap = null;
                    switch (character)
                    {
                        case "0":
                        case "1":
                        case "2":
                        case "3":
                        case "4":
                        case "5":
                        case "6":
                        case "7":
                        case "8":
                        case "9":
                            charBitmap = isCritical ?
                                (useMiniSize ? this.damageSkin.MiniCriticalDigit[character].Bitmap :
                                this.damageSkin.BigCriticalDigit[character].Bitmap) :
                                (useMiniSize ? this.damageSkin.MiniDigit[character].Bitmap :
                                this.damageSkin.BigDigit[character].Bitmap);
                            g.DrawImage(charBitmap, offsetX, maxHeight - charBitmap.Height);
                            offsetX += charBitmap.Width + digitSpacing;
                            break;

                        case "十":
                        case ".":
                            if (this.damageSkin.BigUnit.ContainsKey("0"))
                            {
                                charBitmap = isCritical ?
                                    (useMiniSize ? this.damageSkin.MiniCriticalUnit["0"].Bitmap :
                                    this.damageSkin.BigCriticalUnit["0"].Bitmap) :
                                    (useMiniSize ? this.damageSkin.MiniUnit["0"].Bitmap :
                                    this.damageSkin.BigUnit["0"].Bitmap);
                                g.DrawImage(charBitmap, offsetX, maxHeight - charBitmap.Height);
                                offsetX += charBitmap.Width + unitSpacing;
                            }
                            break;

                        case "百":
                        case "K":
                            if (this.damageSkin.BigUnit.ContainsKey("1"))
                            {
                                charBitmap = isCritical ?
                                    (useMiniSize ? this.damageSkin.MiniCriticalUnit["1"].Bitmap :
                                    this.damageSkin.BigCriticalUnit["1"].Bitmap) :
                                    (useMiniSize ? this.damageSkin.MiniUnit["1"].Bitmap :
                                    this.damageSkin.BigUnit["1"].Bitmap);
                                g.DrawImage(charBitmap, offsetX, maxHeight - charBitmap.Height);
                                offsetX += charBitmap.Width + unitSpacing;
                            }
                            break;

                        case "千":
                        case "M":
                            if (this.damageSkin.BigUnit.ContainsKey("2"))
                            {
                                charBitmap = isCritical ?
                                    (useMiniSize ? this.damageSkin.MiniCriticalUnit["2"].Bitmap :
                                    this.damageSkin.BigCriticalUnit["2"].Bitmap) :
                                    (useMiniSize ? this.damageSkin.MiniUnit["2"].Bitmap :
                                    this.damageSkin.BigUnit["2"].Bitmap);
                                g.DrawImage(charBitmap, offsetX, maxHeight - charBitmap.Height);
                                offsetX += charBitmap.Width + unitSpacing;
                            }
                            break;

                        case "万":
                        case "B":
                            if (this.damageSkin.BigUnit.ContainsKey("3"))
                            {
                                charBitmap = isCritical ?
                                    (useMiniSize ? this.damageSkin.MiniCriticalUnit["3"].Bitmap :
                                    this.damageSkin.BigCriticalUnit["3"].Bitmap) :
                                    (useMiniSize ? this.damageSkin.MiniUnit["3"].Bitmap :
                                    this.damageSkin.BigUnit["3"].Bitmap);
                                g.DrawImage(charBitmap, offsetX, maxHeight - charBitmap.Height);
                                offsetX += charBitmap.Width + unitSpacing;
                            }
                            break;

                        case "亿":
                        case "T":
                            if (this.damageSkin.BigUnit.ContainsKey("4"))
                            {
                                charBitmap = isCritical ?
                                    (useMiniSize ? this.damageSkin.MiniCriticalUnit["4"].Bitmap :
                                    this.damageSkin.BigCriticalUnit["4"].Bitmap) :
                                    (useMiniSize ? this.damageSkin.MiniUnit["4"].Bitmap :
                                    this.damageSkin.BigUnit["4"].Bitmap);
                                g.DrawImage(charBitmap, offsetX, maxHeight - charBitmap.Height);
                                offsetX += charBitmap.Width + unitSpacing;
                            }
                            break;


                        case "兆":
                        case "Q":
                            if (this.damageSkin.BigUnit.ContainsKey("5"))
                            {
                                charBitmap = isCritical ?
                                    (useMiniSize ? this.damageSkin.MiniCriticalUnit["5"].Bitmap :
                                    this.damageSkin.BigCriticalUnit["5"].Bitmap) :
                                    (useMiniSize ? this.damageSkin.MiniUnit["5"].Bitmap :
                                    this.damageSkin.BigUnit["5"].Bitmap);
                                g.DrawImage(charBitmap, offsetX, maxHeight - charBitmap.Height);
                                offsetX += charBitmap.Width + unitSpacing;
                            }
                            break;

                        case "京":
                            if (this.damageSkin.BigUnit.ContainsKey("6"))
                            {
                                charBitmap = isCritical ?
                                    (useMiniSize ? this.damageSkin.MiniCriticalUnit["6"].Bitmap :
                                    this.damageSkin.BigCriticalUnit["6"].Bitmap) :
                                    (useMiniSize ? this.damageSkin.MiniUnit["6"].Bitmap :
                                    this.damageSkin.BigUnit["6"].Bitmap);
                                g.DrawImage(charBitmap, offsetX, maxHeight - charBitmap.Height);
                                offsetX += charBitmap.Width + unitSpacing;
                            }
                            break;
                    }
                }
            }
            return finalBitmap;
        }

        public Bitmap GetUnit()
        {
            Bitmap unitBitmap = null;

            int width = 0;
            int height = 0;

            if (damageSkin.BigUnit.Count > 0)
            {
                if (UseMiniSize)
                {
                    foreach (var unit in damageSkin.MiniUnit.Values)
                    {
                        width += unit.Bitmap.Width;
                        height = Math.Max(height, unit.Bitmap.Height);
                        width += this.damageSkin.MiniUnitSpacing;
                    }
                    foreach (var unit in damageSkin.MiniCriticalUnit.Values)
                    {
                        width += unit.Bitmap.Width;
                        height = Math.Max(height, unit.Bitmap.Height);
                        width += this.damageSkin.MiniCriticalUnitSpacing;
                    }
                    unitBitmap = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage(unitBitmap))
                    {
                        g.Clear(Color.Transparent);
                        int offsetX = 0;
                        foreach (var unit in damageSkin.MiniUnit.Values)
                        {
                            g.DrawImage(unit.Bitmap, offsetX, height - unit.Bitmap.Height);
                            offsetX += unit.Bitmap.Width;
                            offsetX += this.damageSkin.MiniUnitSpacing;
                        }
                        foreach (var unit in damageSkin.MiniCriticalUnit.Values)
                        {
                            g.DrawImage(unit.Bitmap, offsetX, height - unit.Bitmap.Height);
                            offsetX += unit.Bitmap.Width;
                            offsetX += this.damageSkin.MiniCriticalUnitSpacing;
                        }
                    }
                }
                else
                {
                    foreach (var unit in damageSkin.BigUnit.Values)
                    {
                        width += unit.Bitmap.Width;
                        height = Math.Max(height, unit.Bitmap.Height);
                        width += this.damageSkin.BigUnitSpacing;
                    }
                    foreach (var unit in damageSkin.BigCriticalUnit.Values)
                    {
                        width += unit.Bitmap.Width;
                        height = Math.Max(height, unit.Bitmap.Height);
                        width += this.damageSkin.BigCriticalUnitSpacing;
                    }
                    unitBitmap = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage(unitBitmap))
                    {
                        g.Clear(Color.Transparent);
                        int offsetX = 0;
                        foreach (var unit in damageSkin.BigUnit.Values)
                        {
                            g.DrawImage(unit.Bitmap, offsetX, height - unit.Bitmap.Height);
                            offsetX += unit.Bitmap.Width;
                            offsetX += this.damageSkin.BigUnitSpacing;
                        }
                        foreach (var unit in damageSkin.BigCriticalUnit.Values)
                        {
                            g.DrawImage(unit.Bitmap, offsetX, height - unit.Bitmap.Height);
                            offsetX += unit.Bitmap.Width;
                            offsetX += this.damageSkin.BigCriticalUnitSpacing;
                        }
                    }
                }
            }
            return unitBitmap;
        }

        public Bitmap GetExtraEffect()
        {

            Bitmap[] originalBitmaps = new Bitmap[5]
            {
                this.damageSkin.MiniDigit.ContainsKey("Miss") ? this.damageSkin.MiniDigit?["Miss"].Bitmap : null,
                this.damageSkin.MiniDigit.ContainsKey("guard") ? this.damageSkin.MiniDigit?["guard"].Bitmap : null,
                this.damageSkin.MiniDigit.ContainsKey("resist") ? this.damageSkin.MiniDigit?["resist"].Bitmap : null,
                this.damageSkin.MiniDigit.ContainsKey("shot") ? this.damageSkin.MiniDigit?["shot"].Bitmap : null,
                this.damageSkin.MiniDigit.ContainsKey("counter") ? this.damageSkin.MiniDigit?["counter"].Bitmap : null
            };


            int width = 0;
            int height = 0;

            foreach (var bo in originalBitmaps)
            {
                if (bo == null) continue;
                width += bo.Width;
                height = Math.Max(height, bo.Height);
            }

            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                int offsetX = 0;
                for (int j = 0; j < originalBitmaps.Count(); j++)
                {
                    if (originalBitmaps[j] == null) continue;
                    g.DrawImage(originalBitmaps[j], offsetX, height - originalBitmaps[j].Height);
                    offsetX += originalBitmaps[j].Width;
                }
            }

            return bitmap;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using CharaSimResource;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    /// <summary>
    /// 提供一系列的静态Graphics工具，用来绘制物品tooltip。
    /// </summary>
    public static class GearGraphics
    {
        static GearGraphics()
        {
            TBrushes = new Dictionary<string, TextureBrush>();
            TBrushes["n"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_n, WrapMode.Tile);
            TBrushes["ne"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_ne, WrapMode.Clamp);
            TBrushes["e"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_e, WrapMode.Tile);
            TBrushes["se"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_se, WrapMode.Clamp);
            TBrushes["s"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_s, WrapMode.Tile);
            TBrushes["sw"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_sw, WrapMode.Clamp);
            TBrushes["w"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_w, WrapMode.Tile);
            TBrushes["nw"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_nw, WrapMode.Clamp);
            TBrushes["c"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame2_c, WrapMode.Tile);
            SetFontFamily("宋体");
        }

        public static readonly Dictionary<string, TextureBrush> TBrushes;
        public static readonly Font ItemNameFont = new Font("宋体", 14f, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font ItemDetailFont = new Font("宋体", 12f, GraphicsUnit.Pixel);
        public static readonly Font EpicGearDetailFont = new Font("宋体", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font TahomaFont = new Font("Tahoma", 12f, GraphicsUnit.Pixel);
        public static readonly Font SetItemPropFont = new Font("宋体", 12f, GraphicsUnit.Pixel);
        public static readonly Font ItemReqLevelFont = new Font("宋体", 11f, GraphicsUnit.Pixel);

        public static Font ItemNameFont2 { get; private set; }
        public static Font ItemDetailFont2 { get; private set; }

        public static void SetFontFamily(string fontName)
        {
            if (ItemNameFont2 != null)
            {
                ItemNameFont2.Dispose();
                ItemNameFont2 = null;
            }
            ItemNameFont2 = new Font(fontName, 14f, FontStyle.Bold, GraphicsUnit.Pixel);

            if (ItemDetailFont2 != null)
            {
                ItemDetailFont2.Dispose();
                ItemDetailFont2 = null;
            }
            ItemDetailFont2 = new Font(fontName, 12f, GraphicsUnit.Pixel);
        }

        public static readonly Color GearBackColor = Color.FromArgb(204, 0, 51, 85);
        public static readonly Color EpicGearBackColor = Color.FromArgb(170, 68, 0, 0);
        public static readonly Color GearIconBackColor = Color.FromArgb(238, 187, 204, 221);
        public static readonly Color EpicGearIconBackColor = Color.FromArgb(221, 204, 187, 187);

        public static readonly Brush GearBackBrush = new SolidBrush(GearBackColor);
        public static readonly Brush EpicGearBackBrush = new SolidBrush(EpicGearBackColor);
        public static readonly Pen GearBackPen = new Pen(GearBackColor);
        public static readonly Pen EpicGearBackPen = new Pen(EpicGearBackColor);
        public static readonly Brush GearIconBackBrush = new SolidBrush(GearIconBackColor);
        public static readonly Brush GearIconBackBrush2 = new SolidBrush(Color.FromArgb(187, 238, 238, 238));
        public static readonly Brush EpicGearIconBackBrush = new SolidBrush(EpicGearIconBackColor);
        public static readonly Brush StatDetailGrayBrush = new SolidBrush(Color.FromArgb(85, 85, 85));

        /// <summary>
        /// 表示物品说明中带有#c标识的橙色字体画刷。
        /// </summary>
        public static readonly Brush OrangeBrush = new SolidBrush(Color.FromArgb(255, 153, 0));
        /// <summary>
        /// 表示物品附加属性中橙色字体画刷。
        /// </summary>
        public static readonly Brush OrangeBrush2 = new SolidBrush(Color.FromArgb(255, 170, 0));
        /// <summary>
        /// 表示装备职业额外说明中使用的橙黄色画刷。
        /// </summary>
        public static readonly Brush OrangeBrush3 = new SolidBrush(Color.FromArgb(255, 204, 0));
        /// <summary>
        /// 表示装备属性额外说明中使用的绿色画刷。
        /// </summary>
        public static readonly Brush GreenBrush2 = new SolidBrush(Color.FromArgb(204, 255, 0));
        /// <summary>
        /// 表示用于绘制“攻击力提升”文字的灰色画刷。
        /// </summary>
        public static readonly Brush GrayBrush2 = new SolidBrush(Color.FromArgb(153, 153, 153));
        /// <summary>
        /// 表示套装名字的绿色画刷。
        /// </summary>
        public static readonly Brush SetItemNameBrush = new SolidBrush(Color.FromArgb(119, 255, 0));
        /// <summary>
        /// 表示套装属性不可用的灰色画刷。
        /// </summary>
        public static readonly Brush SetItemGrayBrush = new SolidBrush(Color.FromArgb(119, 136, 153));
        /// <summary>
        /// 表示装备tooltip中金锤子描述文字的颜色画刷。
        /// </summary>
        public static readonly Brush GoldHammerBrush = new SolidBrush(Color.FromArgb(255, 238, 204));
        /// <summary>
        /// 表示灰色品质的装备名字画刷，额外属性小于0。
        /// </summary>
        public static readonly Brush GearNameBrushA = new SolidBrush(Color.FromArgb(187, 187, 187));
        /// <summary>
        /// 表示白色品质的装备名字画刷，额外属性为0~5。
        /// </summary>
        public static readonly Brush GearNameBrushB = new SolidBrush(Color.FromArgb(255, 255, 255));
        /// <summary>
        /// 表示橙色品质的装备名字画刷，额外属性为0~5，并且已经附加卷轴。
        /// </summary>
        public static readonly Brush GearNameBrushC = new SolidBrush(Color.FromArgb(255, 136, 17));
        private static Color gearBlueColor = Color.FromArgb(85, 170, 255);
        /// <summary>
        /// 表示蓝色品质的装备名字画刷，额外属性为6~22。
        /// </summary>
        public static readonly Brush GearNameBrushD = new SolidBrush(gearBlueColor);
        private static Color gearPurpleColor = Color.FromArgb(204, 102, 255);
        /// <summary>
        /// 表示紫色品质的装备名字画刷，额外属性为23~39。
        /// </summary>
        public static readonly Brush GearNameBrushE = new SolidBrush(gearPurpleColor);
        private static Color gearGoldColor = Color.FromArgb(255, 255, 17);
        /// <summary>
        /// 表示金色品质的装备名字画刷，额外属性为40~54。
        /// </summary>
        public static readonly Brush GearNameBrushF = new SolidBrush(gearGoldColor);
        private static Color gearGreenColor = Color.FromArgb(51, 255, 0);
        /// <summary>
        /// 表示绿色品质的装备名字画刷，额外属性为55~69。
        /// </summary>
        public static readonly Brush GearNameBrushG = new SolidBrush(gearGreenColor);
        /// <summary>
        /// 表示红色品质的装备名字画刷，额外属性为70以上。
        /// </summary>
        public static readonly Brush GearNameBrushH = new SolidBrush(Color.FromArgb(255, 0, 119));

        public static Brush GetGearNameBrush(int diff, bool up)
        {
            if (diff < 0)
                return GearNameBrushA;
            if (diff < 6)
            {
                if (!up)
                    return GearNameBrushB;
                else
                    return GearNameBrushC;
            }
            if (diff < 23)
                return GearNameBrushD;
            if (diff < 40)
                return GearNameBrushE;
            if (diff < 55)
                return GearNameBrushF;
            if (diff < 70)
                return GearNameBrushG;
            return GearNameBrushH;
        }

        public static readonly Pen GearItemBorderPenC = new Pen(Color.FromArgb(255, 0, 102));
        public static readonly Pen GearItemBorderPenB = new Pen(gearBlueColor);
        public static readonly Pen GearItemBorderPenA = new Pen(gearPurpleColor);
        public static readonly Pen GearItemBorderPenS = new Pen(gearGoldColor);
        public static readonly Pen GearItemBorderPenSS = new Pen(gearGreenColor);
        public static Pen GetGearItemBorderPen(GearGrade grade)
        {
            switch (grade)
            {
                case GearGrade.B:
                    return GearItemBorderPenB;
                case GearGrade.A:
                    return GearItemBorderPenA;
                case GearGrade.S:
                    return GearItemBorderPenS;
                case GearGrade.SS:
                    return GearItemBorderPenSS;
                default:
                    return null;
            }
        }

        public static Brush GetPotentialTextBrush(GearGrade grade)
        {
            switch (grade)
            {
                case GearGrade.B: return GearNameBrushD;
                case GearGrade.A: return GearNameBrushE;
                case GearGrade.S: return GearNameBrushF;
                case GearGrade.SS: return GreenBrush2;
            }
            return null;
        }

        /// <summary>
        /// 在指定区域绘制包含宏代码的字符串。
        /// </summary>
        /// <param Name="g">绘图所关联的graphics。</param>
        /// <param Name="s">要绘制的string。</param>
        /// <param Name="font">要绘制string的字体。</param>
        /// <param Name="x">起始的x坐标。</param>
        /// <param Name="X1">每行终止的x坐标。</param>
        /// <param Name="y">起始行的y坐标。</param>
        public static void DrawString(Graphics g, string s, Font font, int x, int x1, ref int y, int height)
        {
            if (s == null)
                return;
            StringFormat fmt = StringFormat.GenericTypographic;
            Stack<Brush> brushStack = new Stack<Brush>();
            brushStack.Push(Brushes.White);
            Brush brush = brushStack.Peek();
            StringBuilder sb = new StringBuilder();
            int curX = x;
            int tempX = 0;
            int strPos = 0;
            char curChar;

            while (strPos < s.Length)
            {
                curChar = s[strPos++];
                if (curChar == '\\')
                {
                    if (strPos < s.Length)
                    {
                        curChar = s[strPos++];
                        switch (curChar)
                        {
                            case 'r': curChar = '\r'; break;
                            case 'n':
                                if (strPos <= 2)
                                    curChar = ' ';//替换文本第一个\n
                                else
                                    curChar = '\n';
                                break;
                        }
                    }
                    else //结束符处理
                    {
                        curChar = '#';
                    }
                }
                if (curChar == '#')
                {
                    if (strPos < s.Length && s[strPos] == 'c')//遇到#c 换橙刷子并flush
                    {
                        g.DrawString(sb.ToString(), font, brush, curX, y, fmt);
                        brushStack.Push(GearGraphics.OrangeBrush);
                        brush = brushStack.Peek();
                        strPos++;
                    }
                    else if (strPos < s.Length && s[strPos] == 'g')//遇到#c 换绿刷子并flush
                    {
                        g.DrawString(sb.ToString(), font, brush, curX, y, fmt);
                        brushStack.Push(GearGraphics.GearNameBrushG);
                        brush = brushStack.Peek();
                        strPos++;
                    }
                    else if (brushStack.Count == 1) //同#c
                    {
                        g.DrawString(sb.ToString(), font, brush, curX, y, fmt);
                        brushStack.Push(GearGraphics.OrangeBrush);
                        brush = brushStack.Peek();
                    }
                    else//遇到# 换白刷子并flush
                    {
                        g.DrawString(sb.ToString(), font, brush, curX, y, fmt);
                        brushStack.Pop();
                        brush = brushStack.Peek();
                    }
                    sb.Remove(0, sb.Length);
                    curX += tempX;
                    tempX = 0;
                }
                else if (curChar < 0x0100 && curChar != '\r' && curChar != '\n')
                {
                    sb.Append(curChar);
                    tempX += (int)font.Size / 2;
                }
                else if (curChar > 0xff)
                {
                    sb.Append(curChar);
                    tempX += (int)font.Size;
                }
                if (curX + tempX >= x1 || curChar == '\n' || strPos == s.Length)
                {
                    g.DrawString(sb.ToString(), font, brush, curX, y, fmt);
                    sb.Remove(0, sb.Length);
                    curX = x;
                    tempX = 0;
                    y += height;
                }
            }

            fmt.Dispose();
        }

        public static Bitmap EnlargeBitmap(Bitmap bitmap)
        {
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            IntPtr p = data.Scan0;
            byte[] origin = new byte[bitmap.Width * bitmap.Height * 4];
            Marshal.Copy(p, origin, 0, origin.Length);
            bitmap.UnlockBits(data);
            byte[] newByte = new byte[origin.Length * 4];
            byte[] buffer = new byte[4];
            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    Array.Copy(origin, getOffset(j, i, bitmap.Width), buffer, 0, 4);
                    Array.Copy(buffer, 0, newByte, getOffset(2 * j, 2 * i, bitmap.Width * 2), 4);
                    Array.Copy(buffer, 0, newByte, getOffset(2 * j + 1, 2 * i, bitmap.Width * 2), 4);
                }
                Array.Copy(newByte, getOffset(0, 2 * i, bitmap.Width * 2), newByte, getOffset(0, 2 * i + 1, bitmap.Width * 2), bitmap.Width * 8);
            }
            Bitmap newBitmap = new Bitmap(bitmap.Width * 2, bitmap.Height * 2);
            data = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(newByte, 0, data.Scan0, newByte.Length);
            newBitmap.UnlockBits(data);
            return newBitmap;
        }

        private static int getOffset(int x, int y, int width, int unit = 4)
        {
            return (y * width + x) * unit;
        }

        public static Point[] GetBorderPath(int dx, int width, int height)
        {
            List<Point> pointList = new List<Point>(13);
            pointList.Add(new Point(dx + 1, 0));
            pointList.Add(new Point(dx + 1, 1));
            pointList.Add(new Point(dx + 0, 1));
            pointList.Add(new Point(dx + 0, height - 2));
            pointList.Add(new Point(dx + 1, height - 2));
            pointList.Add(new Point(dx + 1, height - 1));
            pointList.Add(new Point(dx + width - 2, height - 1));
            pointList.Add(new Point(dx + width - 2, height - 2));
            pointList.Add(new Point(dx + width - 1, height - 2));
            pointList.Add(new Point(dx + width - 1, 1));
            pointList.Add(new Point(dx + width - 2, 1));
            pointList.Add(new Point(dx + width - 2, 0));
            pointList.Add(new Point(dx + 1, 0));
            return pointList.ToArray();
        }

        public static Point[] GetIconBorderPath(int x, int y)
        {
            Point[] pointList = new Point[5];
            pointList[0] = new Point(x + 32, y + 31);
            pointList[1] = new Point(x + 32, y);
            pointList[2] = new Point(x, y);
            pointList[3] = new Point(x, y + 32);
            pointList[4] = new Point(x + 31, y + 32);
            return pointList;
        }

        public static void DrawGearDetailNumber(Graphics g, int x, int y, string num, bool can)
        {
            Bitmap bitmap;
            for (int i = 0; i < num.Length; i++)
            {
                switch (num[i])
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        string resourceName = (can ? "ToolTip_Equip_Can_" : "ToolTip_Equip_Cannot_") + num[i];
                        bitmap = (Bitmap)Resource.ResourceManager.GetObject(resourceName);
                        g.DrawImage(bitmap, x, y);
                        x += bitmap.Width + 1;
                        break;
                    case '-':
                        bitmap = can ? Resource.ToolTip_Equip_Can_none : Resource.ToolTip_Equip_Cannot_none;
                        g.DrawImage(bitmap, x, y + 3);
                        x += bitmap.Width + 1;
                        break;
                    case '%':
                        bitmap = can ? Resource.ToolTip_Equip_Can_percent : Resource.ToolTip_Equip_Cannot_percent;
                        g.DrawImage(bitmap, x + 1, y);
                        x += bitmap.Width + 2;
                        break;
                }
            }
        }

        public static void DrawGearGrowthNumber(Graphics g, int x, int y, string num, bool can)
        {
            Bitmap bitmap;
            for (int i = 0; i < num.Length; i++)
            {
                switch (num[i])
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        string resourceName = (can ? "ToolTip_Equip_GrowthEnabled_" : "ToolTip_Equip_Cannot_") + num[i];
                        bitmap = (Bitmap)Resource.ResourceManager.GetObject(resourceName);
                        g.DrawImage(bitmap, x, y);
                        x += bitmap.Width + 1;
                        break;
                    case '-':
                        bitmap = can ? Resource.ToolTip_Equip_GrowthDisabled_none : Resource.ToolTip_Equip_GrowthDisabled_none;
                        g.DrawImage(bitmap, x, y);
                        x += bitmap.Width + 1;
                        break;
                    case '%':
                        bitmap = can ? Resource.ToolTip_Equip_GrowthEnabled_percent : Resource.ToolTip_Equip_GrowthEnabled_percent;
                        g.DrawImage(bitmap, x + 7, y - 4);
                        x += bitmap.Width + 1;
                        break;
                    case 'm':
                        bitmap = can ? Resource.ToolTip_Equip_GrowthEnabled_max : Resource.ToolTip_Equip_GrowthEnabled_max;
                        g.DrawImage(bitmap, x, y);
                        x += bitmap.Width + 1;
                        break;
                }
            }
        }

        public static void DrawNewTooltipBack(Graphics g, int x, int y, int width, int height)
        {
            Dictionary<string, TextureBrush> res = TBrushes;
            //测算准线
            int[] guideX = new int[4] { 0, res["w"].Image.Width, width - res["e"].Image.Width, width };
            int[] guideY = new int[4] { 0, res["n"].Image.Height, height - res["s"].Image.Height, height };
            for (int i = 0; i < guideX.Length; i++) guideX[i] += x;
            for (int i = 0; i < guideY.Length; i++) guideY[i] += y;
            //绘制四角
            FillRect(g, res["nw"], guideX, guideY, 0, 0, 1, 1);
            FillRect(g, res["ne"], guideX, guideY, 2, 0, 3, 1);
            FillRect(g, res["sw"], guideX, guideY, 0, 2, 1, 3);
            FillRect(g, res["se"], guideX, guideY, 2, 2, 3, 3);
            //填充上下区域
            if (guideX[2] > guideX[1])
            {
                FillRect(g, res["n"], guideX, guideY, 1, 0, 2, 1);
                FillRect(g, res["s"], guideX, guideY, 1, 2, 2, 3);
            }
            //填充左右区域
            if (guideY[2] > guideY[1])
            {
                FillRect(g, res["w"], guideX, guideY, 0, 1, 1, 2);
                FillRect(g, res["e"], guideX, guideY, 2, 1, 3, 2);
            }
            //填充中心
            if (guideX[2] > guideX[1] && guideY[2] > guideY[1])
            {
                FillRect(g, res["c"], guideX, guideY, 1, 1, 2, 2);
            }
        }

        private static void FillRect(Graphics g, TextureBrush brush, int[] guideX, int[] guideY, int x0, int y0, int x1, int y1)
        {
            brush.ResetTransform();
            brush.TranslateTransform(guideX[x0], guideY[y0]);
            g.FillRectangle(brush, guideX[x0], guideY[y0], guideX[x1] - guideX[x0], guideY[y1] - guideY[y0]);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, UInt32 wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0xB;

        public static void SetRedraw(System.Windows.Forms.Control control, bool enable)
        {
            if (control != null)
            {
                SendMessage(control.Handle, WM_SETREDRAW, new IntPtr(enable ? 1 : 0), IntPtr.Zero);
            }
        }
    }
}

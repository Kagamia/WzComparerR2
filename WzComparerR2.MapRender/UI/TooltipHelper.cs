using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using WzComparerR2.Common;
using WzComparerR2.Rendering;

namespace WzComparerR2.MapRender.UI
{
    public static class TooltipHelper
    {
        public static TextBlock PrepareTextBlock(IWcR2Font font, string text, ref Vector2 pos, Color color)
        {
            Vector2 size = font.MeasureString(text);

            TextBlock block = new TextBlock();
            block.Font = font;
            block.Text = text;
            block.Position = pos;
            block.ForeColor = color;

            pos.X += size.X;
            return block;
        }

        public static TextBlock PrepareTextLine(IWcR2Font font, string text, ref Vector2 pos, Color color, ref float maxWidth)
        {
            Vector2 size = font.MeasureString(text);

            TextBlock block = new TextBlock();
            block.Font = font;
            block.Text = text;
            block.Position = pos;
            block.ForeColor = color;

            maxWidth = Math.Max(pos.X + size.X, maxWidth);
            pos.X = 0;
            pos.Y += font.LineHeight;

            if (size.Y >= font.LineHeight)
            {
                pos.Y += size.Y - font.Size;
            }

            return block;
        }

        public static TextBlock[] PrepareFormatText(IWcR2Font font, string formatText, ref Vector2 pos, int width, ref float maxWidth)
        {
            var layouter = new TextLayouter();
            int y = (int)pos.Y;
            var blocks = layouter.LayoutFormatText(font, formatText, width, ref y);
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i].Position.X += pos.X;
                var blockWidth = blocks[i].Font.MeasureString(blocks[i].Text).X;
                maxWidth = Math.Max(maxWidth, blocks[i].Position.X + blockWidth);
            }
            pos.X = 0;
            pos.Y = y;
            return blocks;
        }

        public static TextBlock[] Prepare(LifeInfo info, MapRenderFonts fonts, out Vector2 size)
        {
            var blocks = new List<TextBlock>();
            var current = Vector2.Zero;
            size = Vector2.Zero;

            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "Level: " + info.level + (info.boss ? " (Boss)" : null), ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "HP/MP: " + info.maxHP + " / " + info.maxMP, ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "PAD/MAD: " + info.PADamage + " / " + info.MADamage, ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "PDr/MDr: " + info.PDRate + "% / " + info.MDRate + "%", ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "Acc/Eva: " + info.acc + " / " + info.eva, ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "KB: " + info.pushed, ref current, Color.White, ref size.X));
            blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "Exp: " + info.exp, ref current, Color.White, ref size.X));
            if (info.undead) blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "undead: 1", ref current, Color.White, ref size.X));
            StringBuilder sb;
            if ((sb = GetLifeElemAttrString(ref info.elemAttr)).Length > 0)
                blocks.Add(PrepareTextLine(fonts.TooltipContentFont, "elem: " + sb.ToString(), ref current, Color.White, ref size.X));
            size.Y = current.Y;

            return blocks.ToArray();
        }

        public static StringBuilder GetLifeElemAttrString(ref LifeInfo.ElemAttr elemAttr)
        {
            StringBuilder sb = new StringBuilder(14);
            sb.Append(GetElemResistanceString("冰", elemAttr.I));
            sb.Append(GetElemResistanceString("雷", elemAttr.L));
            sb.Append(GetElemResistanceString("火", elemAttr.F));
            sb.Append(GetElemResistanceString("毒", elemAttr.S));
            sb.Append(GetElemResistanceString("圣", elemAttr.H));
            sb.Append(GetElemResistanceString("暗", elemAttr.D));
            sb.Append(GetElemResistanceString("物", elemAttr.P));
            return sb;
        }

        public static string GetElemResistanceString(string elemName, LifeInfo.ElemResistance resist)
        {
            string e = null;
            switch (resist)
            {
                case LifeInfo.ElemResistance.Immune: e = "× "; break;
                case LifeInfo.ElemResistance.Resist: e = "△ "; break;
                case LifeInfo.ElemResistance.Normal: e = null; break;
                case LifeInfo.ElemResistance.Weak: e = "◎ "; break;
            }
            return e != null ? (elemName + e) : null;
        }

        public static string GetPortalTypeString(int pType)
        {
            switch (pType)
            {
                case 0: return "地图出生点";
                case 1: return "一般传送门(隐藏)";
                case 2: return "一般传送门";
                case 3: return "一般传送门(接触)";
                case 6: return "时空门入口点";
                case 7: return "脚本传送门";
                case 8: return "脚本传送门(隐藏)";
                case 9: return "脚本传送门(接触)";
                case 10: return "地图内传送门";
                case 12: return "弹力装置";
                default: return null;
            }
        }

        public struct TextBlock
        {
            public Vector2 Position;
            public Color ForeColor;
            public IWcR2Font Font;
            public string Text;
        }

        public class TextLayouter : WzComparerR2.Text.TextRenderer<IWcR2Font>
        {
            public TextLayouter() : base()
            {

            }

            List<TextBlock> blocks;

            public TextBlock[] LayoutFormatText(IWcR2Font font, string s, int width, ref int y)
            {
                this.blocks = new List<TextBlock>();
                base.DrawFormatString(s, font, width, ref y, (int)Math.Ceiling(font.LineHeight));
                return this.blocks.ToArray();
            }

            protected override void Flush(StringBuilder sb, int startIndex, int length, int x, int y, string colorID)
            {
                this.blocks.Add(new TextBlock()
                {
                    Position = new Vector2(x, y),
                    ForeColor = this.GetColor(colorID),
                    Font = this.font,
                    Text = sb.ToString(startIndex, length),
                });
            }

            protected override void MeasureRuns(List<WzComparerR2.Text.Run> runs)
            {
                int x = 0;
                foreach (var run in runs)
                {
                    if (run.IsBreakLine)
                    {
                        run.X = x;
                        run.Length = 0;
                    }
                    else
                    {
                        var size = base.font.MeasureString(this.sb.ToString(run.StartIndex, run.Length));
                        run.X = x;
                        run.Width = (int)size.X;
                        x += run.Width;
                    }
                }
            }

            protected override System.Drawing.Rectangle[] MeasureChars(int startIndex, int length)
            {
                var regions = new System.Drawing.Rectangle[length];
                int x = 0;
                for (int i = 0; i < length; i++)
                {
                    var text = this.sb[startIndex + i].ToString();
                    var size = this.font.MeasureString(text);
                    regions[i] = new System.Drawing.Rectangle(x, 0, (int)size.X, (int)size.Y);
                    x += (int)size.X;
                }
                return regions;
            }

            public virtual Color GetColor(string colorID)
            {
                switch (colorID)
                {
                    case "c":
                        return new Color(255, 153, 0);
                    default:
                        return Color.White;
                }
            }
        }
    }
}

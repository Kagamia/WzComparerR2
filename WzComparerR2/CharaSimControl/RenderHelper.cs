using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WzComparerR2.CharaSimControl
{
    class RenderHelper
    {
        private RenderHelper()
        {
        }

        public static TextBlock PrepareText(Graphics g, string text, Font font, Brush brush, int x, int y)
        {
            SizeF size = g.MeasureString(text, font, Int32.MaxValue, StringFormat.GenericTypographic);
            TextBlock block = new TextBlock()
            {
                Text = text,
                Font = font,
                Brush = brush,
                Position = new Point(x, y),
                Size = new Size((int)Math.Round(size.Width, MidpointRounding.AwayFromZero),
                    (int)Math.Round(size.Height, MidpointRounding.AwayFromZero))
            };
            return block;
        }

        public static void DrawText(Graphics g, TextBlock block, Point offset)
        {
            g.DrawString(block.Text, block.Font, block.Brush,
                block.Position.X + offset.X, block.Position.Y + offset.Y,
                StringFormat.GenericTypographic);
        }

        public static Rectangle Measure(IEnumerable<TextBlock> blocks)
        {
            Rectangle rect = Rectangle.Empty;

            foreach (var block in blocks)
            {
                var blockRect = block.Rectangle;
                if (!blockRect.IsEmpty)
                {
                    rect = Rectangle.Union(rect, blockRect);
                }
            }
            return rect;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using WzComparerR2.Text;

namespace WzComparerR2.Rendering
{
    public class XnaFontRenderer : TextRenderer<XnaFont>
    {
        public XnaFontRenderer(SpriteBatchEx spriteBatch)
        {
            this.SpriteBatch = spriteBatch;
        }

        public SpriteBatchEx SpriteBatch { get; set; }


        protected override void MeasureRuns(List<Run> runs)
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
                    var size = base.font.MeasureString(base.sb, run.StartIndex, run.Length);
                    run.X = x;
                    run.Width = (int)size.X;
                    x += run.Width;
                }
            }
        }

        protected override Rectangle[] MeasureChars(int startIndex, int length)
        {
            var regions = new Rectangle[length];
            int x = 0;
            for (int i = 0; i < length; i++)
            {
                var rect = this.font.TryGetRect(this.sb[startIndex + i]);
                regions[i] = new Rectangle(x, 0, rect.Width, rect.Height);
                x += rect.Width;
            }
            return regions;
        }

        protected override void Flush(StringBuilder sb, int startIndex, int length, int x, int y, string colorID)
        {
            var color = this.GetColor(colorID);
            var pos = new Microsoft.Xna.Framework.Vector2(x, y);
            this.SpriteBatch.DrawStringEx(this.font, sb, startIndex, length, pos, color);
        }

        public virtual Microsoft.Xna.Framework.Color GetColor(string colorID)
        {
            switch (colorID)
            {
                case "c":
                    return new Microsoft.Xna.Framework.Color(255, 153, 0);
                default:
                    return Microsoft.Xna.Framework.Color.White;
            }
        }
    }
}

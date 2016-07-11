using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Rendering;

namespace WzComparerR2.MapRender.UI
{
    public class UIGraphics
    {
        public static void DrawNineForm(RenderEnv env, NineFormResource res, Vector2 position, Vector2 size)
        {
            SpriteBatchEx sprite = env.Sprite;
            List<RenderBlock> blocks = new List<RenderBlock>(13);

            //计算框线
            int[] x = new int[4] { 0, res.NW.Width, (int)size.X - res.NE.Width, (int)size.X };
            int[] y = new int[4] { 0, res.NW.Height, (int)size.Y - res.SW.Height, (int)size.Y };

            //绘制左上
            blocks.Add(new RenderBlock(res.NW, new Rectangle(x[0], y[0], x[1] - x[0], y[1] - y[0])));

            //绘制上
            if (res.NW.Height == res.N.Height)
            {
                blocks.Add(new RenderBlock(res.N, new Rectangle(x[1], y[0], x[2] - x[1], y[1] - y[0])));
            }
            else if (res.NW.Height > res.N.Height)
            {
                int h1 = res.N.Height;
                blocks.Add(new RenderBlock(res.N, new Rectangle(x[1], y[0], x[2] - x[1], h1)));
                blocks.Add(new RenderBlock(res.C, new Rectangle(x[1], h1, x[2] - x[1], y[1] - h1)));
            }

            //绘制右上
            blocks.Add(new RenderBlock(res.NE, new Rectangle(x[2], y[0], x[3] - x[2], y[1] - y[0])));

            //绘制左
            if (res.NW.Width == res.W.Width)
            {
                blocks.Add(new RenderBlock(res.W, new Rectangle(x[0], y[1], x[1] - x[0], y[2] - y[1])));
            }
            else if (res.NW.Width > res.W.Width)
            {
                int w1 = res.W.Width;
                blocks.Add(new RenderBlock(res.W, new Rectangle(x[0], y[1], w1, y[2] - y[1])));
                blocks.Add(new RenderBlock(res.C, new Rectangle(w1, y[1], x[1] - w1, y[2] - y[1])));
            }

            //绘制中
            blocks.Add(new RenderBlock(res.C, new Rectangle(x[1], y[1], x[2] - x[1], y[2] - y[1])));

            //绘制右
            if (res.NE.Width == res.E.Width)
            {
                blocks.Add(new RenderBlock(res.E, new Rectangle(x[2], y[1], x[3] - x[2], y[2] - y[1])));
            }
            else if (res.NE.Width > res.E.Width)
            {
                int w1 = res.E.Width;
                blocks.Add(new RenderBlock(res.E, new Rectangle(x[3] - w1, y[1], w1, y[2] - y[1])));
                blocks.Add(new RenderBlock(res.C, new Rectangle(x[2], y[1], x[3] - x[2] - w1, y[2] - y[1])));
            }

            //绘制左下
            blocks.Add(new RenderBlock(res.SW, new Rectangle(x[0], y[2], x[1] - x[0], y[3] - y[2])));

            //绘制下
            if (res.SW.Height == res.S.Height)
            {
                blocks.Add(new RenderBlock(res.S, new Rectangle(x[1], y[2], x[2] - x[1], y[3] - y[2])));
            }
            else if (res.SW.Height > res.S.Height)
            {
                int h1 = res.S.Height;
                blocks.Add(new RenderBlock(res.S, new Rectangle(x[1], y[3] - h1, x[2] - x[1], h1)));
                blocks.Add(new RenderBlock(res.C, new Rectangle(x[1], y[2], x[2] - x[1], y[3] - y[2] - h1)));
            }

            //绘制右下
            blocks.Add(new RenderBlock(res.SE, new Rectangle(x[2], y[2], x[3] - x[2], y[3] - y[2])));


            //绘制全部
            foreach (var block in blocks)
            {
                if (block.Texture != null && block.Rectangle.Width > 0 && block.Rectangle.Height > 0)
                {
                    Rectangle rect = new Rectangle(block.Rectangle.X + (int)position.X,
                        block.Rectangle.Y + (int)position.Y,
                        block.Rectangle.Width,
                        block.Rectangle.Height);

                    sprite.Draw(block.Texture, rect, Color.White);
                }
            }
        }


        private struct RenderBlock
        {
            public RenderBlock(Texture2D texture, Rectangle rectangle)
            {
                this.Texture = texture;
                this.Rectangle = rectangle;
            }
            public Texture2D Texture;
            public Rectangle Rectangle;
        }
    }
}

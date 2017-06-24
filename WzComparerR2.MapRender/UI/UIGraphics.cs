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
        public static IList<RenderBlock<T>> LayoutNinePatch<T>(INinePatchResource<T> res, Point size)
        {
            var blocks = new List<RenderBlock<T>>(13);

            Point nw = res.GetSize(res.NW);
            Point n = res.GetSize(res.N);
            Point ne = res.GetSize(res.NE);
            Point w = res.GetSize(res.W);
            Point e = res.GetSize(res.E);
            Point sw = res.GetSize(res.SW);
            Point s = res.GetSize(res.S);
            Point se = res.GetSize(res.SE);
            //计算框线
            int[] x = new int[4] { 0, nw.X, size.X - ne.X, size.X };
            int[] y = new int[4] { 0, nw.Y, size.Y - sw.Y, size.Y };

            //绘制左上
            blocks.Add(new RenderBlock<T>(res.NW, new Rectangle(x[0], y[0], x[1] - x[0], y[1] - y[0])));

            //绘制上
            if (nw.Y == n.Y)
            {
                blocks.Add(new RenderBlock<T>(res.N, new Rectangle(x[1], y[0], x[2] - x[1], y[1] - y[0])));
            }
            else if (nw.Y > n.Y)
            {
                blocks.Add(new RenderBlock<T>(res.N, new Rectangle(x[1], y[0], x[2] - x[1], n.Y)));
                blocks.Add(new RenderBlock<T>(res.C, new Rectangle(x[1], n.Y, x[2] - x[1], y[1] - n.Y)));
            }

            //绘制右上
            blocks.Add(new RenderBlock<T>(res.NE, new Rectangle(x[2], y[0], x[3] - x[2], y[1] - y[0])));

            //绘制左
            if (nw.X == w.X)
            {
                blocks.Add(new RenderBlock<T>(res.W, new Rectangle(x[0], y[1], x[1] - x[0], y[2] - y[1])));
            }
            else if (nw.X > w.X)
            {
                blocks.Add(new RenderBlock<T>(res.W, new Rectangle(x[0], y[1], w.X, y[2] - y[1])));
                blocks.Add(new RenderBlock<T>(res.C, new Rectangle(w.X, y[1], x[1] - w.X, y[2] - y[1])));
            }

            //绘制中
            blocks.Add(new RenderBlock<T>(res.C, new Rectangle(x[1], y[1], x[2] - x[1], y[2] - y[1])));

            //绘制右
            if (ne.X == e.X)
            {
                blocks.Add(new RenderBlock<T>(res.E, new Rectangle(x[2], y[1], x[3] - x[2], y[2] - y[1])));
            }
            else if (ne.X > e.X)
            {
                blocks.Add(new RenderBlock<T>(res.E, new Rectangle(x[3] - e.X, y[1], e.X, y[2] - y[1])));
                blocks.Add(new RenderBlock<T>(res.C, new Rectangle(x[2], y[1], x[3] - x[2] - e.X, y[2] - y[1])));
            }

            //绘制左下
            blocks.Add(new RenderBlock<T>(res.SW, new Rectangle(x[0], y[2], x[1] - x[0], y[3] - y[2])));

            //绘制下
            if (sw.Y == s.Y)
            {
                blocks.Add(new RenderBlock<T>(res.S, new Rectangle(x[1], y[2], x[2] - x[1], y[3] - y[2])));
            }
            else if (sw.Y > s.Y)
            {
                blocks.Add(new RenderBlock<T>(res.S, new Rectangle(x[1], y[3] - s.Y, x[2] - x[1], s.Y)));
                blocks.Add(new RenderBlock<T>(res.C, new Rectangle(x[1], y[2], x[2] - x[1], y[3] - y[2] - s.Y)));
            }

            //绘制右下
            blocks.Add(new RenderBlock<T>(res.SE, new Rectangle(x[2], y[2], x[3] - x[2], y[3] - y[2])));

            return blocks;
        }

        public static IList<RenderBlock<T>> LayoutLCR<T>(INinePatchResource<T> res, Point size)
        {
            var blocks = new List<RenderBlock<T>>(3);
            Point w = res.GetSize(res.W);
            Point c = res.GetSize(res.C);
            Point e = res.GetSize(res.E);

            //计算框线
            int[] x = new int[4] { 0, w.X, size.X - e.X, size.X };

            //绘制左
            blocks.Add(new RenderBlock<T>(res.W, new Rectangle(x[0], 0, x[1] - x[0], size.Y)));
            //绘制中
            blocks.Add(new RenderBlock<T>(res.C, new Rectangle(x[1], 0, x[2] - x[1], size.Y)));
            //绘制右
            blocks.Add(new RenderBlock<T>(res.E, new Rectangle(x[2], 0, x[3] - x[2], size.Y)));

            return blocks;
        }

        public static IList<RenderBlock<T>> LayoutTCB<T>(INinePatchResource<T> res, Point size)
        {
            var blocks = new List<RenderBlock<T>>(3);
            Point n = res.GetSize(res.N);
            Point c = res.GetSize(res.C);
            Point s = res.GetSize(res.S);

            //计算框线
            int[] y = new int[4] { 0, n.Y, size.Y - s.Y, size.Y };

            //绘制上
            blocks.Add(new RenderBlock<T>(res.N, new Rectangle(0, y[0], size.X, y[1] - y[0])));
            //绘制中
            blocks.Add(new RenderBlock<T>(res.C, new Rectangle(0, y[1], size.X, y[2] - y[1])));
            //绘制下
            blocks.Add(new RenderBlock<T>(res.S, new Rectangle(0, y[2], size.X, y[3] - y[2])));

            return blocks;
        }

        public static void DrawNineForm(RenderEnv env, NineFormResource res, Vector2 position, Vector2 size)
        {
            SpriteBatchEx sprite = env.Sprite;
            var blocks = LayoutNinePatch(res, size.ToPoint());

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

        public struct RenderBlock<T>
        {
            public RenderBlock(T texture, Rectangle rectangle)
            {
                this.Texture = texture;
                this.Rectangle = rectangle;
            }
            public T Texture;
            public Rectangle Rectangle;
        }
    }

    public interface INinePatchResource<T>
    {
        T NW { get; }
        T N { get; }
        T NE { get; }
        T W { get; }
        T C { get; }
        T E { get; }
        T SW { get; }
        T S { get; }
        T SE { get; }

        Point GetSize(T texture);
    }
}

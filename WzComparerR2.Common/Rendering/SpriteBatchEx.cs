using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using GDIColor = System.Drawing.Color;
using GDIRect = System.Drawing.Rectangle;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace WzComparerR2.Rendering
{
    public class SpriteBatchEx : SpriteBatch
    {
        public SpriteBatchEx(GraphicsDevice graphicsDevice)
            : base(graphicsDevice)
        {
            this.singlePixel = CreateSinglePixel();
        }

        Texture2D singlePixel;

        private Texture2D CreateSinglePixel()
        {
            Texture2D pixel = new Texture2D(this.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            pixel.SetData(new Color[] { Color.White });
            return pixel;
        }

        public void DrawStringEx(XnaFont xnaFont, string text, Vector2 location, Color color)
        {
            DrawStringEx(xnaFont, text, 0, text == null ? 0 : text.Length, location, Vector2.Zero, color);
        }

        public void DrawStringEx(XnaFont xnaFont, string text, Vector2 location, Vector2 size, Color color)
        {
            DrawStringEx(xnaFont, text, 0, text == null ? 0 : text.Length, location, size, color);
        }

        private void DrawStringEx(XnaFont xnaFont, string text, int startIndex, int length, Vector2 location, Vector2 size, Color color)
        {
            IEnumerable<char> e = TextUtils.CreateCharEnumerator(text, startIndex, length);
            DrawStringEx(xnaFont, e, location, size, color, Vector2.Zero, 0);
        }

        public void DrawStringEx(XnaFont xnaFont, StringBuilder stringBuilder, Vector2 location, Color color)
        {
            DrawStringEx(xnaFont, stringBuilder, 0, stringBuilder == null ? 0 : stringBuilder.Length, location, color, Vector2.Zero);
        }

        public void DrawStringEx(XnaFont xnaFont, StringBuilder stringBuilder, Vector2 location, Color color, Vector2 origin)
        {
            DrawStringEx(xnaFont, stringBuilder, 0, stringBuilder == null ? 0 : stringBuilder.Length, location, color, origin);
        }

        public void DrawStringEx(XnaFont xnaFont, StringBuilder stringBuilder, int startIndex, int length, Vector2 location, Color color)
        {
            DrawStringEx(xnaFont, stringBuilder, startIndex, length, location, color, Vector2.Zero);
        }

        public void DrawStringEx(XnaFont xnaFont, StringBuilder stringBuilder, int startIndex, int length, Vector2 location, Color color, Vector2 origin)
        {
            IEnumerable<char> e = TextUtils.CreateCharEnumerator(stringBuilder, startIndex, length);
            DrawStringEx(xnaFont, e, location, Vector2.Zero, color, origin, 0);
        }

        private void DrawStringEx(XnaFont font, IEnumerable<char> text, Vector2 location, Vector2 size, Color color, Vector2 origin, float layerDepth)
        {
            if (font == null || text == null)
            {
                return;
            }

            float dx = location.X, dy = location.Y;

            foreach (char c in text)
            {
                if (c == '\r')
                {
                    continue;
                }
                else if (c == '\n') //换行符
                {
                    dy += font.Height;
                    dx = location.X;
                    continue;
                }
                else
                {
                    Rectangle rect = font.TryGetRect(c);
                    if (size.X > 0 && dx > location.X && dx + rect.Width > location.X + size.X) //强制换行
                    {
                        dy += font.Height;
                        dx = location.X;
                    }
                    base.Draw(font.TextureBuffer, new Vector2(dx, dy), rect, color, 0f, origin, 1f, SpriteEffects.None, layerDepth);
                    dx += rect.Width;
                }
            }
            location.X = dx;
            location.Y = dy;
        }

        public void DrawPath(Point[] path, Color color)
        {
            Rectangle[] rectPath = new Rectangle[path.Length];
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (path[i].X == path[i + 1].X)
                {
                    int dy = path[i + 1].Y - path[i].Y;
                    if (dy > 0)
                    {
                        rectPath[i] = new Rectangle(path[i].X, path[i].Y, 1, dy);
                    }
                    else if (dy < 0)
                    {
                        rectPath[i] = new Rectangle(path[i].X, path[i + 1].Y + 1, 1, -dy);
                    }
                }
                else if (path[i].Y == path[i + 1].Y)
                {
                    int dx = path[i + 1].X - path[i].X;
                    if (dx > 0)
                    {
                        rectPath[i] = new Rectangle(path[i].X, path[i].Y, dx, 1);
                    }
                    else if (dx < 0)
                    {
                        rectPath[i] = new Rectangle(path[i + 1].X + 1, path[i].Y, -dx, 1);
                    }
                }
            }
            if (path[0] != path[path.Length - 1] || path.Length == 1)
            {
                rectPath[path.Length - 1] = new Rectangle(
                    path[path.Length - 1].X,
                    path[path.Length - 1].Y,
                    1,
                    1);
            }
            for (int i = 0; i < rectPath.Length; i++)
            {
                if (rectPath[i].Width > 0 && rectPath[i].Height > 0)
                {
                    this.FillRectangle(rectPath[i], color);
                }
            }
        }

        public void FillRectangle(Rectangle rectangle, Color color)
        {
            base.Draw(singlePixel, rectangle, color);
        }

        public void FillRectangle(Rectangle rectangle, Color color, Vector2 origin)
        {
            rectangle.X -= (int)origin.X;
            rectangle.Y -= (int)origin.Y;
            base.Draw(singlePixel, rectangle, color);
        }

        public void FillRoundedRectangle(Rectangle rectangle, Color color)
        {
            if (rectangle.Width > 2 && rectangle.Height > 2)
            {
                base.Draw(singlePixel, new Rectangle(rectangle.X + 1, rectangle.Y, rectangle.Width - 2, 1), color);
                base.Draw(singlePixel, new Rectangle(rectangle.X, rectangle.Y + 1, rectangle.Width, rectangle.Height - 2), color);
                base.Draw(singlePixel, new Rectangle(rectangle.X + 1, rectangle.Bottom - 1, rectangle.Width - 2, 1), color);
            }
            else
            {
                base.Draw(singlePixel, rectangle, color);
            }
        }

        public void DrawRectangle(Rectangle rectangle, Color color)
        {
            if (!rectangle.IsEmpty)
            {
                Point[] path = new Point[5];
                path[0] = new Point(rectangle.X, rectangle.Y);
                path[1] = new Point(path[0].X, rectangle.Y + rectangle.Height);
                path[2] = new Point(rectangle.X + rectangle.Width, path[1].Y);
                path[3] = new Point(path[2].X, path[0].Y);
                path[4] = path[0];
                this.DrawPath(path, color);
            }
        }

        public void DrawLine(Point point1, Point point2, int width, Color color)
        {
            if (point1 != point2)
            {
                float length = Vector2.Distance(new Vector2(point1.X, point1.Y), new Vector2(point2.X, point2.Y));
                Rectangle dest = new Rectangle(point1.X, point1.Y, (int)length, width);
                Vector2 origin = new Vector2(0, 0.5f);
                float rot = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
                this.Draw(this.singlePixel, dest, null, color, rot, origin, SpriteEffects.None, 0);
            }
        }

        public void Flush()
        {
            this.End();
            this.GetType().BaseType.GetField("_beginCalled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(this, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.singlePixel != null)
                {
                    this.singlePixel.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}

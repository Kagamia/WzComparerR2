using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GDIColor = System.Drawing.Color;
using GDIRect = System.Drawing.Rectangle;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace WzComparerR2.Rendering
{
    public class XnaFont : IDisposable
    {
        public XnaFont(GraphicsDevice graphicsDevice, FontFamily fontFamily, float size)
            : this(graphicsDevice, new Font(fontFamily, size, GraphicsUnit.Pixel))
        {
        }

        public XnaFont(GraphicsDevice graphicsDevice, string familyName, float size)
            : this(graphicsDevice, new Font(familyName, size, GraphicsUnit.Pixel))
        {
        }

        public XnaFont(GraphicsDevice graphicsDevice, Font baseFont)
        {
            if (graphicsDevice == null || baseFont == null)
            {
                throw new ArgumentNullException();
            }
            this.baseFont = baseFont;
            this.Height = baseFont.Height;
            this.textureBuffer = new Texture2D(graphicsDevice, 2048, 2048, false, SurfaceFormat.Bgra32);
            RebuildGdiBuffer(this.baseFont.Height);
            this.charLocation = new Dictionary<char, Rectangle>();
        }

        Font baseFont;
        Texture2D textureBuffer;
        Bitmap gdiBuffer;
        Graphics g;
        Dictionary<char, Rectangle> charLocation;
        int textureSpaceX;
        int textureSpaceY;
        int textureCurLineHeight;
        int gdiBufferX;

        public Texture2D TextureBuffer
        {
            get { return textureBuffer; }
        }

        public Font BaseFont
        {
            get { return baseFont; }
        }

        public int Height { get; set; }

        public Vector2 MeasureString(string text)
        {
            return MeasureString(text, Vector2.Zero);
        }

        public Vector2 MeasureString(StringBuilder stringBuilder)
        {
            return MeasureString(stringBuilder, Vector2.Zero);
        }

        public Vector2 MeasureString(string text, Vector2 size)
        {
            var ie = TextUtils.CreateCharEnumerator(text, 0, text.Length);
            return MeasureString(ie, size);
        }

        public Vector2 MeasureString(StringBuilder stringBuilder, Vector2 size)
        {
            var ie = TextUtils.CreateCharEnumerator(stringBuilder, 0, stringBuilder.Length);
            return MeasureString(ie, size);
        }

        public Vector2 MeasureString(string text, int startIndex, int length)
        {
            return MeasureString(TextUtils.CreateCharEnumerator(text, startIndex, length), Vector2.Zero);
        }

        public Vector2 MeasureString(StringBuilder stringBuilder, int startIndex, int length)
        {
            return MeasureString(TextUtils.CreateCharEnumerator(stringBuilder, startIndex, length), Vector2.Zero);
        }

        public Vector2 MeasureString(IEnumerable<char> text, Vector2 layoutSize)
        {
            if (text == null)
                return Vector2.Zero;

            Size size = new Size();
            int maxWidth = 0;
            int lineHeight = 0;
            foreach (char c in text)
            {
                if (c == '\r')
                {
                    continue;
                }
                if (c == '\n')
                {
                    if (lineHeight <= 0)
                    {
                        //lineHeight = this.baseFont.Height;
                    }
                    size.Height += this.baseFont.Height;
                    maxWidth = Math.Max(maxWidth, size.Width);
                    size.Width = 0;
                    lineHeight = 0;
                }
                else
                {
                    Rectangle rect = TryGetRect(c);
                    if (layoutSize.X > 0 && size.Width > 0 && size.Width + rect.Width > layoutSize.X) //强制换行
                    {
                        size.Height += this.baseFont.Height;
                        maxWidth = Math.Max(maxWidth, size.Width);
                        size.Width = 0;
                        lineHeight = 0;
                    }
                    size.Width += rect.Width;
                    lineHeight = Math.Max(rect.Height, lineHeight);
                }
            }
            size.Width = Math.Max(maxWidth, size.Width);
            size.Height += lineHeight;
            if (size.Width <= 0)
                return Vector2.Zero;
            return new Vector2(size.Width, size.Height);
        }

        public Rectangle TryGetRect(char c)
        {
            Rectangle rect;
            if (!this.charLocation.TryGetValue(c, out rect))
            {
                rect = this.CreateCharBuffer(c);
                this.charLocation[c] = rect;
            }
            return rect;
        }

        private Rectangle CreateCharBuffer(char c)
        {
            string text = c.ToString();
            SizeF size;

            size = g.MeasureString(text, this.baseFont, byte.MaxValue, StringFormat.GenericTypographic);
            GDIRect originRect = new GDIRect(gdiBufferX, 0, (int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
            if (originRect.Width == 0)
            {
                originRect.Width = (int)(this.baseFont.Size / 2);
            }
            if (gdiBuffer.Height < originRect.Height)
            {
                RebuildGdiBuffer(originRect.Height);
                originRect.X = 0; //2012-10-3
            }
            if (gdiBufferX + originRect.Width > gdiBuffer.Width)
            {
                g.Clear(GDIColor.Transparent);
                gdiBufferX = 0;
                originRect.X = 0;
            }
            g.DrawString(text, baseFont, Brushes.White, originRect.Location, StringFormat.GenericTypographic);

            //计算范围并且复制图像数据到数组
            byte[] b = new byte[4 * originRect.Width * originRect.Height];
            BitmapData data = gdiBuffer.LockBits(originRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            for (int i = 0; i < originRect.Height; i++)
            {
                IntPtr source = IntPtr.Add(data.Scan0, data.Stride * i);
                System.Runtime.InteropServices.Marshal.Copy(source, b, i * 4 * originRect.Width, 4 * originRect.Width);
            }
            gdiBuffer.UnlockBits(data);
            gdiBufferX += originRect.Width;

            //调整xnaTexture的大小并粘贴图像
            textureBuffer.GraphicsDevice.Textures[0] = null;

            if (textureSpaceX + originRect.Width > textureBuffer.Width)
            {
                textureSpaceX = 0;
                textureSpaceY += textureCurLineHeight;
                textureCurLineHeight = 0;
            }
            textureCurLineHeight = Math.Max(textureCurLineHeight, originRect.Height);
            if (textureSpaceY + textureCurLineHeight > textureBuffer.Height)
            {
                ClearTextureBuffer();
                textureSpaceX = 0;
                textureSpaceY = 0;
                charLocation.Clear();
            }

            Rectangle rect = new Rectangle(textureSpaceX, textureSpaceY, originRect.Width, originRect.Height);

            textureBuffer.SetData(0, rect, b, 0, b.Length);
            textureSpaceX += rect.Width;
            return rect;
        }

        private void RebuildGdiBuffer(int height)
        {
            if (gdiBuffer != null)
            {
                g.Dispose();
                gdiBuffer.Dispose();
            }
            gdiBuffer = new Bitmap(textureBuffer.Width, height, PixelFormat.Format32bppArgb);
            gdiBufferX = 0;
            g = Graphics.FromImage(gdiBuffer);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.HighQuality;
            // g.CompositingMode = CompositingMode.SourceCopy; 乱用这句出事故...
        }

        private void ClearTextureBuffer()
        {
            int[] ary = new int[textureBuffer.Width * textureBuffer.Height];
            textureBuffer.SetData(ary);
            ary = null;
        }

        ~XnaFont()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.textureBuffer.Dispose();
                this.g.Dispose();
                this.baseFont.Dispose();
                this.gdiBuffer.Dispose();
            }
        }

        public static FontFamily GdiLoadFontFile(string fontFileName)
        {
            try
            {
                PrivateFontCollection font = new PrivateFontCollection();
                font.AddFontFile(fontFileName);
                gdiFontCache.Add(font);
                return font.Families[0];
            }
            catch
            {
                return null;
            }
        }

        private static List<PrivateFontCollection> gdiFontCache = new List<PrivateFontCollection>();
    }
}

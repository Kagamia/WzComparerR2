using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WzComparerR2.Common
{
    public class GifFrame
    {
        public GifFrame()
        {
        }

        public GifFrame(Bitmap bitmap)
            : this(bitmap, new Point(), 0)
        {
        }

        public GifFrame(Bitmap bitmap, int delay)
            : this(bitmap, new Point(), 0)
        {
        }

        public GifFrame(Bitmap bitmap, Point origin, int delay)
        {
            this.bitmap = bitmap;
            this.origin = origin;
            this.delay = delay;
        }

        Bitmap bitmap;
        Point origin;
        int delay;

        public Bitmap Bitmap
        {
            get { return bitmap; }
            set { bitmap = value; }
        }

        public Point Origin
        {
            get { return origin; }
            set { origin = value; }
        }

        public int Delay
        {
            get { return delay; }
            set { delay = value; }
        }

        public static GifFrame Combine(params GifFrame[] frames)
        {
            //计算新生成图大小
            int left = 0, top = 0, right = 0, bottom = 0, delay = 0;
            foreach (GifFrame frame in frames)
            {
                if (frame == null || frame.bitmap == null)
                {
                    continue;
                }

                left = Math.Min(left, -frame.origin.X);
                top = Math.Min(top, -frame.origin.Y);
                right = Math.Max(right, frame.bitmap.Width - frame.origin.X);
                bottom = Math.Max(bottom, frame.bitmap.Height - frame.origin.Y);
                delay = Math.Max(delay, frame.delay);
            }
            Rectangle rect = new Rectangle(left, top, right - left, bottom - top);
            if (rect.Width == 0 || rect.Height == 0)
                return null;

            //重绘图片
            Bitmap bitmap = new Bitmap(rect.Width, rect.Height);
            Graphics g = Graphics.FromImage(bitmap);
            foreach (GifFrame frame in frames)
            {
                if (frame == null || frame.bitmap == null)
                {
                    continue;
                }
                g.DrawImage(frame.bitmap, frame.origin.X + rect.X, frame.origin.Y + rect.Y);
            }
            g.Dispose();
            GifFrame newFrame = new GifFrame(bitmap, new Point(-rect.X, -rect.Y), delay);
            return newFrame;
        }
    }
}

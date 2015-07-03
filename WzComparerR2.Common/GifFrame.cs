using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace WzComparerR2.Common
{
    public class GifFrame : IGifFrame
    {
        public GifFrame()
        {
            this.A0 = 255;
            this.A1 = 255;
        }

        public GifFrame(Bitmap bitmap)
            : this(bitmap, Point.Empty, 0)
        {
        }

        public GifFrame(Bitmap bitmap, int delay)
            : this(bitmap, Point.Empty, delay)
        {
        }

        public GifFrame(Bitmap bitmap, Point origin, int delay)
            : this()
        {
            this.Bitmap = bitmap;
            this.Origin = origin;
            this.Delay = delay;
        }

        public Bitmap Bitmap { get; set; }
        public Point Origin { get; set; }
        public int Delay { get; set; }
        public int A0 { get; set; }
        public int A1 { get; set; }

        int IGifFrame.Delay
        {
            get { return this.Delay; }
        }

        Rectangle IGifFrame.Region
        {
            get
            {
                var size = this.Bitmap == null ? Size.Empty : this.Bitmap.Size;
                return new Rectangle(-this.Origin.X, -this.Origin.Y, size.Width, size.Height);
            }
        }

        void IGifFrame.Draw(Graphics g, Rectangle canvasRect)
        {
            if (this.Bitmap == null)
            {
                return;
            }

            Point pos = new Point(-this.Origin.X - canvasRect.X, -this.Origin.Y - canvasRect.Y);

            if (A0 >= 255)
            {
                g.DrawImage(this.Bitmap, pos);
            }
            else if (A0 > 0)
            {
                var imageAttr = new ImageAttributes();
                float a = A0 / 255f;
                var mt = new ColorMatrix(new[]{
                        new float[]{1,0,0,0,0},
                        new float[]{0,1,0,0,0},
                        new float[]{0,0,1,0,0},
                        new float[]{0,0,0,a,0},
                        new float[]{0,0,0,0,1},
                    });
                imageAttr.SetColorMatrix(mt);
                g.DrawImage(this.Bitmap,
                    new Rectangle(pos, this.Bitmap.Size),
                    0, 0, this.Bitmap.Width, this.Bitmap.Height, GraphicsUnit.Pixel,
                    imageAttr);
                imageAttr.Dispose();
            }
        }
    }
}

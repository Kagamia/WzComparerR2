using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace WzComparerR2.Common
{
    public abstract class GifEncoder : IDisposable
    {
        protected GifEncoder(string fileName, int width, int height)
        {
            this.FileName = fileName;
            this.Width = width;
            this.Height = height;
        }

        public string FileName { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public virtual void AppendFrame(Bitmap image, int delay)
        {
            BitmapData data = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            this.AppendFrame(data.Scan0, delay);
            image.UnlockBits(data);
        }

        public abstract void AppendFrame(IntPtr pBuffer, int delay);


        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {

        }

        ~GifEncoder()
        {
            this.Dispose(false);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace WzComparerR2.Encoders
{
    public abstract class GifEncoder : IDisposable
    {
        protected GifEncoder()
        {
        }

        public string FileName { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public virtual string Name
        {
            get
            {
                return GetType().Name;
            }
        }
        public abstract GifEncoderCompatibility Compatibility { get; }

        public virtual void Init(string fileName, int width, int height)
        {
            FileName = fileName;
            Width = width;
            Height = height;
        }

        public virtual void AppendFrame(Bitmap image, int delay)
        {
            BitmapData data = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            AppendFrame(data.Scan0, delay);
            image.UnlockBits(data);
        }

        public abstract void AppendFrame(IntPtr pBuffer, int delay);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {

        }

        ~GifEncoder()
        {
            Dispose(false);
        }
    }
}

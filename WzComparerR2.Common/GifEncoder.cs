using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WzComparerR2.Common
{
    public class GifEncoder : IDisposable
    {
        public GifEncoder(string location, int width, int height, int max_color, Color back_color)
        {
            encoder_pointer = construct(location, width, height, max_color, back_color.ToArgb());
        }

        private IntPtr encoder_pointer;

        public void AppendFrame(Bitmap image, int delay)
        {
            BitmapData data = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            encoder.append_frame(data.Scan0, delay, encoder_pointer);

            image.UnlockBits(data);
        }

        public void Dispose()
        {
            encoder.destruct(encoder_pointer);
        }

        private gif_encoder_structure encoder
        {
            get
            {
                return (gif_encoder_structure)Marshal.PtrToStructure(encoder_pointer, typeof(gif_encoder_structure));
            }
        }

        [DllImport("libgif.dll", EntryPoint = "#1", CharSet = CharSet.Unicode)]
        private extern static IntPtr construct(string location, int width, int height, int maxColor, int backColor);

        public delegate void gif_encoder_destruct(IntPtr encoder_pointer);
        public delegate void gif_encoder_append_frame(IntPtr pixels, int delay, IntPtr encoder_pointer);


        [StructLayout(LayoutKind.Sequential)]
        public struct gif_encoder_structure
        {
            public gif_encoder_destruct destruct;
            public gif_encoder_append_frame append_frame;
        }
    }
}

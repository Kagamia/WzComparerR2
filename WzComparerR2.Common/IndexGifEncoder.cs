using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WzComparerR2.Common
{
    public class IndexGifEncoder : GifEncoder
    {
        public IndexGifEncoder(string location, int width, int height)
             : this(location, width, height, 255, Color.FromArgb(0))
        {

        }
        public IndexGifEncoder(string location, int width, int height, int max_color, Color back_color)
            :base(location, width, height)
        {
            encoder_pointer = construct(location, width, height, max_color, back_color.ToArgb());
        }

        private IntPtr encoder_pointer;


        public override void AppendFrame(IntPtr pBuffer, int delay)
        {
            encoder.append_frame(pBuffer, delay, encoder_pointer);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                encoder.destruct(encoder_pointer);
            }
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

        private delegate void gif_encoder_destruct(IntPtr encoder_pointer);
        private delegate void gif_encoder_append_frame(IntPtr pixels, int delay, IntPtr encoder_pointer);

        [StructLayout(LayoutKind.Sequential)]
        private struct gif_encoder_structure
        {
            public gif_encoder_destruct destruct;
            public gif_encoder_append_frame append_frame;
        }
    }
}

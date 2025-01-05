using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WzComparerR2.Encoders
{
    public class IndexGifEncoder : GifEncoder
    {
        public IndexGifEncoder()
        {
        }

        private IntPtr encoder_pointer;

        public override GifEncoderCompatibility Compatibility => new GifEncoderCompatibility()
        {
            IsFixedFrameRate = false,
            MinFrameDelay = 10,
            MaxFrameDelay = 655350,
            FrameDelayStep = 10,
            AlphaSupportMode = AlphaSupportMode.OneBitAlpha,
            DefaultExtension = ".gif",
            SupportedExtensions = new[] { ".gif" },
        };

        public override void Init(string fileName, int width, int height)
        {
            base.Init(fileName, width, height);

            encoder_pointer = construct(fileName, width, height, 255, 0);
        }

        public override void AppendFrame(IntPtr pBuffer, int delay)
        {
            encoder.append_frame(pBuffer, delay, encoder_pointer);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (encoder_pointer != IntPtr.Zero)
                {
                    encoder.destruct(encoder_pointer);
                    encoder_pointer = IntPtr.Zero;
                }
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

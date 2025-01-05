using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WzComparerR2.Encoders
{
    public class BuildInApngEncoder : GifEncoder
    {
        public BuildInApngEncoder()
        {
        }

        public bool OptimizeEnabled { get; set; }

        private IntPtr handle;

        public override GifEncoderCompatibility Compatibility => new GifEncoderCompatibility()
        {
            IsFixedFrameRate = false,
            MinFrameDelay = 1,
            MaxFrameDelay = 655350,
            FrameDelayStep = 1,
            AlphaSupportMode = AlphaSupportMode.FullAlpha,
            DefaultExtension = ".png",
            SupportedExtensions = new[] { ".png" },
        };

        public override void Init(string fileName, int width, int height)
        {
            base.Init(fileName, width, height);

            var err = apng_init(fileName, width, height, out handle);
            if (err != ApngError.Success)
            {
                throw new Exception($"Apng error: {err}.");
            }
        }

        public override void AppendFrame(IntPtr pBuffer, int delay)
        {
            var err = apng_append_frame(handle, pBuffer, 0, 0, Width, Height, Width * 4, delay, OptimizeEnabled);
            if (err != ApngError.Success)
            {
                throw new Exception($"Apng error: {err}.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (handle != IntPtr.Zero)
                {
                    apng_write_end(handle);
                    apng_destroy(ref handle);
                    handle = IntPtr.Zero;
                }
            }
            base.Dispose(disposing);
        }

        enum ApngError : int
        {
            Success = 0,
            ContextCreateFailed = 1,
            FileError = 2,
            ArgumentError = 3,
            MemoryError = 4,
        };

        [DllImport("libapng.dll")]
        static extern ApngError apng_init([MarshalAs(UnmanagedType.LPWStr)] string fileName, int width, int height, out IntPtr ppEnc);
        [DllImport("libapng.dll")]
        static extern ApngError apng_append_frame(IntPtr pEnc, IntPtr pData, int x, int y, int width, int height, int stride, int delay_ms, bool optimize);
        [DllImport("libapng.dll")]
        static extern void apng_write_end(IntPtr pEnc);
        [DllImport("libapng.dll")]
        static extern void apng_destroy(ref IntPtr ppEnc);
    }
}

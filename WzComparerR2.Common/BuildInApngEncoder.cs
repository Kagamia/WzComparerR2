using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WzComparerR2.Common
{
    public class BuildInApngEncoder : GifEncoder
    {
        public BuildInApngEncoder(string fileName, int width, int height)
            : base(fileName, width, height)
        {
            var err = apng_init(fileName, width, height, out this.handle);
            if (err != ApngError.Success)
            {
                throw new Exception($"Apng error: {err}.");
            }
        }

        private IntPtr handle;

        public bool OptimizeEnabled { get; set; }

        public override void AppendFrame(IntPtr pBuffer, int delay)
        {
            var err = apng_append_frame(this.handle, pBuffer, 0, 0, this.Width, this.Height, this.Width * 4, delay, this.OptimizeEnabled);
            if (err != ApngError.Success)
            {
                throw new Exception($"Apng error: {err}.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                apng_write_end(this.handle);
                apng_destroy(ref this.handle);
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
        static extern ApngError apng_init([MarshalAs(UnmanagedType.LPWStr)]string fileName, int width, int height, out IntPtr ppEnc);
        [DllImport("libapng.dll")]
        static extern ApngError apng_append_frame(IntPtr pEnc, IntPtr pData, int x, int y, int width, int height, int stride, int delay_ms, bool optimize);
        [DllImport("libapng.dll")]
        static extern void apng_write_end(IntPtr pEnc);
        [DllImport("libapng.dll")]
        static extern void apng_destroy(ref IntPtr ppEnc);
    }
}

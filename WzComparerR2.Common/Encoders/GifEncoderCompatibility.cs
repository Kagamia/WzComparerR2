using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.Encoders
{
    public class GifEncoderCompatibility
    {
        public GifEncoderCompatibility()
        {
        }

        public bool IsFixedFrameRate { get; set; }
        public int MinFrameDelay { get; set; }
        public int MaxFrameDelay { get; set; }
        public int FrameDelayStep { get; set; }
        public AlphaSupportMode AlphaSupportMode { get; set; }
        public string DefaultExtension { get; set; }
        public IReadOnlyList<string> SupportedExtensions { get; set; }
    }

    public enum AlphaSupportMode
    {
        NoAlpha,
        OneBitAlpha,
        FullAlpha
    }
}

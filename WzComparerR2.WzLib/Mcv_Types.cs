using System;

namespace WzComparerR2.WzLib
{
    public class McvHeader
    {
        public string Signature { get; set; }
        public int HeaderLength { get; set; }
        public uint FourCC { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int FrameCount { get; set; }
        public McvDataFlags DataFlag { get; set; }
        public McvFrameInfo[] Frames { get; set; }
    }

    [Flags]
    public enum McvDataFlags
    {
        Default = 0,
        AlphaMap = 1,
        PerFrameDelay = 2,
        PerFrameTimeline = 4,
    }

    public class McvFrameInfo
    {
        public McvFrameInfo()
        {
            this.DataOffset = -1;
            this.AlphaDataOffset = -1;
        }

        public long DataOffset { get; set; }
        public int DataCount { get; set; }
        public long AlphaDataOffset { get; set; }
        public int AlphaDataCount { get; set; }
        public long DelayInNanoseconds { get; set; }
        public long StartTimeInNanoseconds { get; set; }

        public TimeSpan Delay => TimeSpan.FromTicks(this.DelayInNanoseconds / 100);
        public TimeSpan StartTime => TimeSpan.FromTicks(this.StartTimeInNanoseconds / 100);
    }
}

using System;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib
{
    public class Wz_Video : IMapleStoryBlob
    {
        public Wz_Video(uint offset, int length, Wz_Image wz_Image)
        {
            this.Offset = offset;
            this.Length = length;
            this.WzImage = wz_Image;
        }

        public uint Offset { get; set; }
        public int Length { get; set; }
        public Wz_Image WzImage { get; set; }
        public IMapleStoryFile WzFile => this.WzImage?.WzFile;

        public void CopyTo(byte[] buffer, int offset)
        {
            if (buffer.Length - offset < this.Length)
            {
                throw new ArgumentException("Insufficient buffer size");
            }
            lock (this.WzFile.ReadLock)
            {
                var s = this.WzImage.OpenRead();
                s.Position = this.Offset;
                s.ReadExactly(buffer, offset, this.Length);
            }
        }

        public void CopyTo(Span<byte> span)
        {
            if (span.Length < this.Length)
            {
                throw new ArgumentException("Insufficient buffer size");
            }
            lock (this.WzFile.ReadLock)
            {
                var s = this.WzImage.OpenRead();
                s.Position = this.Offset;
                s.ReadExactly(span.Slice(0, this.Length));
            }
        }
    }
}

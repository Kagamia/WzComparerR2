using System;

namespace WzComparerR2.WzLib
{
    public interface IMapleStoryBlob
    {
        int Length { get; }
        void CopyTo(byte[] buffer, int offset);
        void CopyTo(Span<byte> span);
    }
}

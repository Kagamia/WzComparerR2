using System;

namespace WzComparerR2.WzLib.Utilities
{
    public interface IWzDecrypter
    {
        byte this[int index] { get; }
        void Decrypt(byte[] buffer, int startIndex, int length);
        void Decrypt(byte[] buffer, int startIndex, int length, int keyOffset);
        void Decrypt(Span<byte> data);
        void Decrypt(Span<byte> data, int keyOffset);
    }
}

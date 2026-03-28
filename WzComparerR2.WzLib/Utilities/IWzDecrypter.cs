using System;

namespace WzComparerR2.WzLib.Utilities
{
    public interface IWzDecrypter
    {
        byte this[int index] { get; }
        void Decrypt(Span<byte> data);
        void Decrypt(Span<byte> data, int keyOffset);
        void Decrypt(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);
        void Decrypt(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer, int keyOffset);
    }
}

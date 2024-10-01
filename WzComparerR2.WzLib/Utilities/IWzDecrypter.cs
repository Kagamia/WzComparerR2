using System;

namespace WzComparerR2.WzLib.Utilities
{
    public interface IWzDecrypter
    {
        void Decrypt(byte[] buffer, int startIndex, int length);
    }
}

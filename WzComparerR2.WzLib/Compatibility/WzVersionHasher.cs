//"v410_260106_1_A1F3C9E2"
using System;
using System.Runtime.InteropServices;

namespace WzComparerR2.WzLib.Compatibility
{
    public static class WzVersionHasher
    {
        public static uint ComputePkg1HashVersion(int wzVersion)
        {
            uint sum = 0;
#if NET6_0_OR_GREATER
            Span<char> versionStr = stackalloc char[11];
            wzVersion.TryFormat(versionStr, out int charsWritten, provider: System.Globalization.CultureInfo.InvariantCulture);
            versionStr = versionStr.Slice(0, charsWritten);
#else
            string versionStr = wzVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
#endif
            for (int j = 0; j < versionStr.Length; j++)
            {
                sum <<= 5;
                sum += (uint)versionStr[j] + 1;
            }
            
            return sum;
        }

        public static uint ComputePkg2HashVersion(string wzVersion)
        {
            ReadOnlySpan<byte> strBytes = MemoryMarshal.Cast<char, byte>(wzVersion.AsSpan());
            uint hash = 0x811C9DC5;
            foreach (var c in strBytes)
            {
                hash = (hash ^ c) * 0x1000193;
            }
            hash = 0x85EBCA6B * (hash ^ (hash >> 13));
            return hash ^ (hash >> 16);
        }
    }
}
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace WzComparerR2.Patcher.Builder
{
    public class CheckSum
    {
        static CheckSum()
        {
            if (sbox == null)
            {
                sbox = new uint[SizeSBox * 8];
                Generatesbox(sbox);
            }
        }

        private CheckSum()
        {

        }

        private const int PolyNomial = 0x04C11DB7;
        private const uint TopBit = 0x80000000;
        private const int SizeSBox = 256;
        private const int TableNum = 8;
        private static uint[] sbox;

        private static void Generatesbox(uint[] sbox)
        {
            uint remain;
            uint i;
            int bit;

            for (i = 0; i < SizeSBox; i++)
            {
                remain = i << 0x18;
                for (bit = 0; bit < 8; bit++)
                {
                    if ((remain & TopBit) != 0)
                    {
                        remain = (remain << 1) ^ PolyNomial;
                    }
                    else
                    {
                        remain = (remain << 1);
                    }
                }
                sbox[i] = remain;
            }

            for (; i < sbox.Length; i++)
            {
                uint r = sbox[i - 256];
                sbox[i] = sbox[r >> 24] ^ (r << 8);
            }
        }

        public static uint ComputeHash(Stream stream, long length, CancellationToken cancellationToken = default)
        {
            return ComputeHash(stream, length, 0, cancellationToken);
        }

        public static uint ComputeHash(Stream stream, long length, uint crc, CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(0x4000);
            try
            {
                while (length > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int count = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, length));
                    if (count == 0)
                    {
                        break;
                    }
                    crc = ComputeHash(buffer, 0, count, crc);
                    length -= count;
                }
                return crc;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static uint ComputeHash(byte[] data, int startIndex, int count, uint crc)
        {
            return ComputeHash(data.AsSpan(startIndex, count), crc);
        }

        public static uint ComputeHash(ReadOnlySpan<byte> data, uint crc)
        {
#if NET6_0_OR_GREATER
            // reference paper: Fast CRC Computation for Generic Polynomials Using PCLMULQDQ Instruction
            if (data.Length >= 32 && Sse42.IsSupported && Pclmulqdq.IsSupported)
            {
                unsafe
                {
                    Vector128<long> k4k3 = Vector128.Create(0xE8A45605, 0xC5B9CD4C);
                    Vector128<byte> reverseMask = Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
                    Vector128<byte> x0, x1, x2;

                    fixed (byte* pData = data)
                        x0 = Sse2.LoadVector128(pData);
                    x0 = Ssse3.Shuffle(x0, reverseMask);
                    x0 = Sse2.Xor(x0, Vector128.Create(0, 0, 0, crc).AsByte());
                    data = data.Slice(16);

                    while (data.Length >= 16)
                    {
                        x1 = Pclmulqdq.CarrylessMultiply(x0.AsInt64(), k4k3, 0x00).AsByte();
                        x0 = Pclmulqdq.CarrylessMultiply(x0.AsInt64(), k4k3, 0x11).AsByte();
                        fixed (byte* pData = data)
                            x2 = Sse2.LoadVector128(pData);
                        x2 = Ssse3.Shuffle(x2, reverseMask);
                        x0 = Sse2.Xor(x0, x1);
                        x0 = Sse2.Xor(x0, x2);
                        data = data.Slice(16);
                    }
                    x0 = Ssse3.Shuffle(x0, reverseMask);

                    Span<byte> rollingData = stackalloc byte[16];
                    fixed (byte* pData = rollingData)
                        Sse2.Store(pData, x0);
                    crc = 0;
                    foreach (var b in rollingData)
                        crc = (crc << 8) ^ CheckSum.sbox[(crc >> 24) ^ b];
                }
            }
#endif

            if (data.Length >= 8)
            {
                Span<uint> pcrc = stackalloc uint[1] { crc };
                Span<byte> crcBytes = MemoryMarshal.AsBytes(pcrc);
                ref uint crcRef = ref pcrc[0];
                ReadOnlySpan<uint> table = sbox.AsSpan();

                while (data.Length >= 8)
                {
                    crcRef ^= (uint)((data[0] << 24) + (data[1] << 16) + (data[2] << 8) + data[3]);
                    crcRef = table[crcBytes[3] + 0x700]
                            ^ table[crcBytes[2] + 0x600]
                            ^ table[crcBytes[1] + 0x500]
                            ^ table[crcBytes[0] + 0x400]
                            ^ table[data[4] + 0x300]
                            ^ table[data[5] + 0x200]
                            ^ table[data[6] + 0x100]
                            ^ table[data[7] + 0x000];
                    data = data.Slice(8);
                }
                crc = crcRef;
            }

            foreach (var b in data)
                crc = (crc << 8) ^ CheckSum.sbox[(crc >> 24) ^ b];

            return crc;
        }
    }
}

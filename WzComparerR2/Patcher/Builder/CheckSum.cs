using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

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

        public static uint ComputeHash2(byte[] data, int startIndex, int count, uint crc)
        {
            for (int i = startIndex, i0 = startIndex + count; i < i0; i++)
            {
                uint IndexLookup = ((crc >> 0x18) ^ data[i]);
                crc = (uint)((crc << 0x08) ^ sbox[IndexLookup]);
            }
            return crc;
        }

        public static uint ComputeHash(Stream stream, long length, CancellationToken cancellationToken = default)
        {
            return ComputeHash(stream, length, 0, cancellationToken);
        }

        public static uint ComputeHash(Stream stream, long length, uint crc, CancellationToken cancellationToken = default)
        {
            byte[] buffer = new byte[0x8000];
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

        public static uint ComputeHash(byte[] data, int startIndex, int count, uint crc)
        {
            return ComputeHash(data.AsSpan(startIndex, count), crc);
        }

        private static uint ComputeHash(ReadOnlySpan<byte> data, uint crc)
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

            for (int i = 0; i < data.Length; i++)
            {
                uint indexLookup = (crcRef >> 0x18) ^ data[i];
                crcRef = (crcRef << 8) ^ table[(int)indexLookup];
            }

            return crcRef;
        }
    }
}

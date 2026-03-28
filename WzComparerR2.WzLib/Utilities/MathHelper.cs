using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace WzComparerR2.WzLib.Utilities
{
    internal static class MathHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Mix(uint v)
        {
            v ^= v >> 16;
            v *= 0x7FEB352D;
            v ^= v >> 15;
            v *= 0x846CA68B;
            v ^= v >> 16;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ROL(uint v, int n) => (v << n) | (v >> (32 - n));

        const byte M8 = 0xAA;
        const ushort M16 = 0xAAAA;

        /// <summary>
        /// XOR each byte with (byte)(0xAA + i), widening to char.
        /// </summary>
        public static unsafe void XorWidenToChar(ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            int length = bytes.Length;
            int i = 0;

#if NET6_0_OR_GREATER
            if (Avx2.IsSupported && length >= 32)
            {
                var increment = Vector256.Create((byte)32);
                var mask = Vector256.Create(
                    (byte)(M8 + 0),  (byte)(M8 + 1),  (byte)(M8 + 2),  (byte)(M8 + 3),
                    (byte)(M8 + 4),  (byte)(M8 + 5),  (byte)(M8 + 6),  (byte)(M8 + 7),
                    (byte)(M8 + 8),  (byte)(M8 + 9),  (byte)(M8 + 10), (byte)(M8 + 11),
                    (byte)(M8 + 12), (byte)(M8 + 13), (byte)(M8 + 14), (byte)(M8 + 15),
                    (byte)(M8 + 16), (byte)(M8 + 17), (byte)(M8 + 18), (byte)(M8 + 19),
                    (byte)(M8 + 20), (byte)(M8 + 21), (byte)(M8 + 22), (byte)(M8 + 23),
                    (byte)(M8 + 24), (byte)(M8 + 25), (byte)(M8 + 26), (byte)(M8 + 27),
                    (byte)(M8 + 28), (byte)(M8 + 29), (byte)(M8 + 30), (byte)(M8 + 31));

                fixed (byte* pIn = bytes)
                fixed (char* pOut = chars)
                {
                    var output = (ushort*)pOut;
                    for (; i + 32 <= length; i += 32)
                    {
                        var data = Avx.LoadVector256(pIn + i);
                        var xored = Avx2.Xor(data, mask);

                        var lo = Avx2.ConvertToVector256Int16(xored.GetLower()).AsUInt16();
                        var hi = Avx2.ConvertToVector256Int16(xored.GetUpper()).AsUInt16();

                        Avx.Store(output + i, lo);
                        Avx.Store(output + i + 16, hi);

                        mask = Avx2.Add(mask, increment);
                    }
                }
            }
            else if (Sse2.IsSupported && length >= 16)
            {
                var increment = Vector128.Create((byte)16);
                var mask = Vector128.Create(
                    (byte)(M8 + 0), (byte)(M8 + 1), (byte)(M8 + 2), (byte)(M8 + 3),
                    (byte)(M8 + 4), (byte)(M8 + 5), (byte)(M8 + 6), (byte)(M8 + 7),
                    (byte)(M8 + 8), (byte)(M8 + 9), (byte)(M8 + 10), (byte)(M8 + 11),
                    (byte)(M8 + 12), (byte)(M8 + 13), (byte)(M8 + 14), (byte)(M8 + 15));

                fixed (byte* pIn = bytes)
                fixed (char* pOut = chars)
                {
                    var output = (ushort*)pOut;
                    for (; i + 16 <= length; i += 16)
                    {
                        var data = Sse2.LoadVector128(pIn + i);
                        var xored = Sse2.Xor(data, mask);

                        var lo = Sse2.UnpackLow(xored, Vector128<byte>.Zero);
                        var hi = Sse2.UnpackHigh(xored, Vector128<byte>.Zero);

                        Sse2.Store(output + i, lo.AsUInt16());
                        Sse2.Store(output + i + 8, hi.AsUInt16());

                        mask = Sse2.Add(mask, increment);
                    }
                }
            }
#endif
            for (; i < length; i++)
            {
                chars[i] = (char)(bytes[i] ^ (byte)(M8 + i));
            }
        }

        /// <summary>
        /// XOR each char with (ushort)(0xAAAA + i).
        /// </summary>
        public static unsafe void XorChars(ReadOnlySpan<char> input, Span<char> output)
        {
            int length = input.Length;
            int i = 0;

#if NET6_0_OR_GREATER
            if (Avx2.IsSupported && length >= 16)
            {
                var increment = Vector256.Create((ushort)16);
                var mask = Vector256.Create(
                    (ushort)(M16 + 0),  (ushort)(M16 + 1),  (ushort)(M16 + 2),  (ushort)(M16 + 3),
                    (ushort)(M16 + 4),  (ushort)(M16 + 5),  (ushort)(M16 + 6),  (ushort)(M16 + 7),
                    (ushort)(M16 + 8),  (ushort)(M16 + 9),  (ushort)(M16 + 10), (ushort)(M16 + 11),
                    (ushort)(M16 + 12), (ushort)(M16 + 13), (ushort)(M16 + 14), (ushort)(M16 + 15));

                fixed (char* pIn = input, pOut = output)
                {
                    for (; i + 16 <= length; i += 16)
                    {
                        var vec = Avx.LoadVector256((ushort*)(pIn + i));
                        var result = Avx2.Xor(vec, mask);
                        Avx.Store((ushort*)(pOut + i), result);
                        mask = Avx2.Add(mask, increment);
                    }
                }
            }
            else if (Sse2.IsSupported && length >= 8)
            {
                var increment = Vector128.Create((ushort)8);
                var mask = Vector128.Create(
                    (ushort)(M16 + 0), (ushort)(M16 + 1), (ushort)(M16 + 2), (ushort)(M16 + 3),
                    (ushort)(M16 + 4), (ushort)(M16 + 5), (ushort)(M16 + 6), (ushort)(M16 + 7));

                fixed (char* pIn = input, pOut = output)
                {
                    for (; i + 8 <= length; i += 8)
                    {
                        var vec = Sse2.LoadVector128((ushort*)(pIn + i));
                        var result = Sse2.Xor(vec, mask);
                        Sse2.Store((ushort*)(pOut + i), result);
                        mask = Sse2.Add(mask, increment);
                    }
                }
            }
#endif
            for (; i < length; i++)
            {
                output[i] = (char)(input[i] ^ (char)(M16 + i));
            }
        }

        /// <summary>
        /// XOR each byte with (byte)(0xAA + i), writing to output.
        /// </summary>
        public static unsafe void XorBytes(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int length = input.Length;
            int i = 0;

#if NET6_0_OR_GREATER
            if (Avx2.IsSupported && length >= 32)
            {
                var increment = Vector256.Create((byte)32);
                var mask = Vector256.Create(
                    (byte)(M8 + 0),  (byte)(M8 + 1),  (byte)(M8 + 2),  (byte)(M8 + 3),
                    (byte)(M8 + 4),  (byte)(M8 + 5),  (byte)(M8 + 6),  (byte)(M8 + 7),
                    (byte)(M8 + 8),  (byte)(M8 + 9),  (byte)(M8 + 10), (byte)(M8 + 11),
                    (byte)(M8 + 12), (byte)(M8 + 13), (byte)(M8 + 14), (byte)(M8 + 15),
                    (byte)(M8 + 16), (byte)(M8 + 17), (byte)(M8 + 18), (byte)(M8 + 19),
                    (byte)(M8 + 20), (byte)(M8 + 21), (byte)(M8 + 22), (byte)(M8 + 23),
                    (byte)(M8 + 24), (byte)(M8 + 25), (byte)(M8 + 26), (byte)(M8 + 27),
                    (byte)(M8 + 28), (byte)(M8 + 29), (byte)(M8 + 30), (byte)(M8 + 31));

                fixed (byte* pIn = input, pOut = output)
                {
                    for (; i + 32 <= length; i += 32)
                    {
                        var data = Avx.LoadVector256(pIn + i);
                        var result = Avx2.Xor(data, mask);
                        Avx.Store(pOut + i, result);
                        mask = Avx2.Add(mask, increment);
                    }
                }
            }
            else if (Sse2.IsSupported && length >= 16)
            {
                var increment = Vector128.Create((byte)16);
                var mask = Vector128.Create(
                    (byte)(M8 + 0), (byte)(M8 + 1), (byte)(M8 + 2), (byte)(M8 + 3),
                    (byte)(M8 + 4), (byte)(M8 + 5), (byte)(M8 + 6), (byte)(M8 + 7),
                    (byte)(M8 + 8), (byte)(M8 + 9), (byte)(M8 + 10), (byte)(M8 + 11),
                    (byte)(M8 + 12), (byte)(M8 + 13), (byte)(M8 + 14), (byte)(M8 + 15));

                fixed (byte* pIn = input, pOut = output)
                {
                    for (; i + 16 <= length; i += 16)
                    {
                        var data = Sse2.LoadVector128(pIn + i);
                        var result = Sse2.Xor(data, mask);
                        Sse2.Store(pOut + i, result);
                        mask = Sse2.Add(mask, increment);
                    }
                }
            }
#endif
            for (; i < length; i++)
            {
                output[i] = (byte)(input[i] ^ (byte)(M8 + i));
            }
        }

        public static unsafe int SumBytes(ReadOnlySpan<byte> data)
        {
            int cs = 0;
            int i = 0;
            int count = data.Length;

            fixed (byte* pBuffer = data)
            {
#if NET6_0_OR_GREATER
                if (Avx2.IsSupported)
                {
                    var sum = Vector256<long>.Zero;
                    var zero = Vector256<byte>.Zero;
                    for (; i + 32 <= count; i += 32)
                    {
                        var v = Avx.LoadVector256(pBuffer + i);
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(v, zero).AsInt64());
                    }
                    var lo = sum.GetLower();
                    var hi = sum.GetUpper();
                    var total = Sse2.Add(lo, hi);
                    cs = (int)(total.GetElement(0) + total.GetElement(1));
                }
                else if (Sse2.IsSupported)
                {
                    var sum = Vector128<long>.Zero;
                    var zero = Vector128<byte>.Zero;
                    for (; i + 16 <= count; i += 16)
                    {
                        var v = Sse2.LoadVector128(pBuffer + i);
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(v, zero).AsInt64());
                    }
                    cs = (int)(sum.GetElement(0) + sum.GetElement(1));
                }
#endif
                for (; i < count; i++)
                {
                    cs += pBuffer[i];
                }
            }

            return cs;
        }
    }
}
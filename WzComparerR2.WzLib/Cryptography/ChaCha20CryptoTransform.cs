// port from https://github.com/mcraiha/CSharp-ChaCha20-NetStandard/blob/master/src/CSChaCha20.cs

/*
 * Copyright (c) 2015, 2018 Scott Bennett
 *           (c) 2018-2023 Kaarlo Räihä
 *
 * Permission to use, copy, modify, and distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 * WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 * ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 * WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 * ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 * OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace WzComparerR2.WzLib.Cryptography
{
    public class ChaCha20CryptoTransform : ICryptoTransform, IDisposable
    {
        /// <summary>
        /// Only allowed key lenght in bytes
        /// </summary>
        public const int AllowedKeyLength = 32;

        /// <summary>
        /// Only allowed nonce lenght in bytes
        /// </summary>
        public const int AllowedNonceLength = 12;

        /// <summary>
        /// How many bytes are processed per loop
        /// </summary>
        public const int ProcessBytesAtTime = 64;

        public const int StateLength = 16;

        public int InputBlockSize => ProcessBytesAtTime;

        public int OutputBlockSize => ProcessBytesAtTime;

        public bool CanTransformMultipleBlocks => true;

        public bool CanReuseTransform => false;

        /// <summary>
        /// The ChaCha20 state (aka "context")
        /// </summary>
        private readonly uint[] state = new uint[StateLength];

        /// <summary>
        /// Determines if the objects in this class have been disposed of. Set to true by the Dispose() method.
        /// </summary>
        private bool isDisposed = false;

        public ChaCha20CryptoTransform(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, uint counter)
        {
            this.KeySetup(key);
            this.IvSetup(nonce, counter);
        }

        /// <summary>
        /// The ChaCha20 state (aka "context"). Read-Only.
        /// </summary>
        public uint[] State => this.state;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            this.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
            int inputBlocks = Math.DivRem(inputCount, this.InputBlockSize, out int rem);
            int outputCount = inputBlocks * this.OutputBlockSize;
            if (inputBlocks == 0 || rem != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputCount));
            }
            if (outputBuffer == null)
            {
                throw new ArgumentNullException(nameof(outputBuffer));
            }
            if (outputCount > outputBuffer.Length - outputOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(outputBuffer));
            }
            this.TransformBlock(inputBuffer.AsSpan(inputOffset, inputCount - rem), outputBuffer.AsSpan(outputOffset, outputCount), out int byteWritten);
            return byteWritten;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            this.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
            if (inputCount == 0)
            {
                return Array.Empty<byte>();
            }

            int inputBlocks = Math.DivRem(inputCount, this.InputBlockSize, out int rem);
            int outputCount = inputBlocks * this.OutputBlockSize;
            byte[] outputBuffer = new byte[outputCount];
            this.TransformBlock(inputBuffer.AsSpan(inputOffset, inputCount - rem), outputBuffer.AsSpan(), out _);
            return outputBuffer;
        }

        /// <summary>
        /// Set up the ChaCha state with the given key. A 32-byte key is required and enforced.
        /// </summary>
        /// <param name="key">
        /// A 32-byte (256-bit) key, treated as a concatenation of eight 32-bit little-endian integers
        /// </param>
        private void KeySetup(ReadOnlySpan<byte> key)
        {
            if (key.Length != AllowedKeyLength)
            {
                throw new ArgumentException($"Key length must be {AllowedKeyLength}. Actual: {key.Length}");
            }

            state[4] = Util.U8To32Little(key, 0);
            state[5] = Util.U8To32Little(key, 4);
            state[6] = Util.U8To32Little(key, 8);
            state[7] = Util.U8To32Little(key, 12);

            // These are the same constants defined in the reference implementation.
            // http://cr.yp.to/streamciphers/timings/estreambench/submissions/salsa20/chacha8/ref/chacha.c
            ReadOnlySpan<byte> constants = (key.Length == AllowedKeyLength) ? "expand 32-byte k"u8 : "expand 16-byte k"u8;
            int keyIndex = key.Length - 16;

            state[8] = Util.U8To32Little(key, keyIndex + 0);
            state[9] = Util.U8To32Little(key, keyIndex + 4);
            state[10] = Util.U8To32Little(key, keyIndex + 8);
            state[11] = Util.U8To32Little(key, keyIndex + 12);

            state[0] = Util.U8To32Little(constants, 0);
            state[1] = Util.U8To32Little(constants, 4);
            state[2] = Util.U8To32Little(constants, 8);
            state[3] = Util.U8To32Little(constants, 12);
        }

        /// <summary>
        /// Set up the ChaCha state with the given nonce (aka Initialization Vector or IV) and block counter. A 12-byte nonce and a 4-byte counter are required.
        /// </summary>
        /// <param name="nonce">
        /// A 12-byte (96-bit) nonce, treated as a concatenation of three 32-bit little-endian integers
        /// </param>
        /// <param name="counter">
        /// A 4-byte (32-bit) block counter, treated as a 32-bit little-endian integer
        /// </param>
        private void IvSetup(ReadOnlySpan<byte> nonce, uint counter)
        {
            if (nonce.Length != AllowedNonceLength)
            {
                // There has already been some state set up. Clear it before exiting.
                this.Dispose();
                throw new ArgumentException($"Nonce length must be {AllowedNonceLength}. Actual: {nonce.Length}");
            }

            state[12] = counter;
            state[13] = Util.U8To32Little(nonce, 0);
            state[14] = Util.U8To32Little(nonce, 4);
            state[15] = Util.U8To32Little(nonce, 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null)
            {
                throw new ArgumentNullException(nameof(inputBuffer));
            }
            if ((uint)inputCount > inputBuffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(inputCount));
            }
            if (inputOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            }
            if (inputBuffer.Length - inputCount < inputOffset)
            {
                throw new ArgumentException("Offset and length were out of bounds.");
            }
        }

        /// <summary>
        /// Encrypt or decrypt an arbitrary-length byte array (input), writing the resulting byte array to the output buffer. The number of bytes to read from the input buffer is determined by numBytes.
        /// </summary>
        /// <param name="output">Output byte array</param>
        /// <param name="input">Input byte array</param>
        /// <param name="numBytes">How many bytes to process</param>
        /// <param name="simdMode">Chosen SIMD mode (default is auto-detect)</param>
        private unsafe void TransformBlock(ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("state", "The ChaCha state has been disposed");
            }

            Span<uint> x = stackalloc uint[StateLength];    // Working buffer
            Span<byte> tmp = stackalloc byte[ProcessBytesAtTime];  // Temporary buffer

            bytesWritten = 0;
            while (input.Length >= ProcessBytesAtTime)
            {
                UpdateStateAndGenerateTemporaryBuffer(this.state, x, tmp);

#if NET8_0_OR_GREATER
                if (Vector512.IsHardwareAccelerated)
                {
                    // 1 x 64 bytes
                    Vector512<byte> inputV = Vector512.Create<byte>(input);
                    Vector512<byte> tmpV = Vector512.Create<byte>(tmp);
                    Vector512<byte> outputV = inputV ^ tmpV;
                    outputV.CopyTo(output);
                }
                else
#endif
#if NET6_0_OR_GREATER
                if (Avx2.IsSupported)
                {
                    // 2 x 32 bytes
                    Vector256<byte> inputV, tmpV;
                    fixed (byte* pInput = input)
                    fixed (byte* pTmp = tmp)
                    fixed (byte* pOutput = output)
                    {
                        inputV = Avx2.LoadVector256(pInput);
                        tmpV = Avx2.LoadVector256(pTmp);
                        Avx2.Store(pOutput, Avx2.Xor(inputV, tmpV));

                        inputV = Avx2.LoadVector256(pInput + 32);
                        tmpV = Avx2.LoadVector256(pTmp + 32);
                        Avx2.Store(pOutput + 32, Avx2.Xor(inputV, tmpV));
                    }
                }
                else if (Sse2.IsSupported)
                {
                    // 4 x 16 bytes
                    Vector128<byte> inputV, tmpV;
                    fixed (byte* pInput = input)
                    fixed (byte* pTmp = tmp)
                    fixed (byte* pOutput = output)
                    {
                        inputV = Sse2.LoadVector128(pInput);
                        tmpV = Sse2.LoadVector128(pTmp);
                        Sse2.Store(pOutput, Sse2.Xor(inputV, tmpV));

                        inputV = Sse2.LoadVector128(pInput + 16);
                        tmpV = Sse2.LoadVector128(pTmp + 16);
                        Sse2.Store(pOutput + 16, Sse2.Xor(inputV, tmpV));

                        inputV = Sse2.LoadVector128(pInput + 32);
                        tmpV = Sse2.LoadVector128(pTmp + 32);
                        Sse2.Store(pOutput + 32, Sse2.Xor(inputV, tmpV));

                        inputV = Sse2.LoadVector128(pInput + 48);
                        tmpV = Sse2.LoadVector128(pTmp + 48);
                        Sse2.Store(pOutput + 48, Sse2.Xor(inputV, tmpV));
                    }
                }
                else
#endif
                {
                    // Small unroll
                    ReadOnlySpan<uint> inputV = MemoryMarshal.Cast<byte, uint>(input);
                    ReadOnlySpan<uint> tmpV = MemoryMarshal.Cast<byte, uint>(tmp);
                    Span<uint> outputV = MemoryMarshal.Cast<byte, uint>(output);
                    for (int i = 0, j = ProcessBytesAtTime / 4; i < j; i++)
                    {
                        outputV[i] = inputV[i] ^ tmpV[i];
                    }
                }

                input = input.Slice(ProcessBytesAtTime);
                output = output.Slice(ProcessBytesAtTime);
                bytesWritten += ProcessBytesAtTime;
            }
        }

        #region Destructor and Disposer
        /// <summary>
        /// Clear and dispose of the internal state. The finalizer is only called if Dispose() was never called on this cipher.
        /// </summary>
        ~ChaCha20CryptoTransform()
        {
            Dispose(false);
        }

        /// <summary>
        /// Clear and dispose of the internal state. Also request the GC not to call the finalizer, because all cleanup has been taken care of.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            /*
                * The Garbage Collector does not need to invoke the finalizer because Dispose(bool) has already done all the cleanup needed.
                */
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This method should only be invoked from Dispose() or the finalizer. This handles the actual cleanup of the resources.
        /// </summary>
        /// <param name="disposing">
        /// Should be true if called by Dispose(); false if called by the finalizer
        /// </param>
        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    /* Cleanup managed objects by calling their Dispose() methods */
                }

                /* Cleanup any unmanaged objects here */
                Array.Clear(state, 0, StateLength);
            }

            isDisposed = true;
        }
        #endregion // Destructor and Disposer

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateStateAndGenerateTemporaryBuffer(Span<uint> stateToModify, Span<uint> workingBuffer, Span<byte> temporaryBuffer)
        {
            // Copy state to working buffer
            stateToModify.Slice(0, StateLength).CopyTo(workingBuffer);

            for (int i = 0; i < 10; i++)
            {
                QuarterRound(workingBuffer, 0, 4, 8, 12);
                QuarterRound(workingBuffer, 1, 5, 9, 13);
                QuarterRound(workingBuffer, 2, 6, 10, 14);
                QuarterRound(workingBuffer, 3, 7, 11, 15);

                QuarterRound(workingBuffer, 0, 5, 10, 15);
                QuarterRound(workingBuffer, 1, 6, 11, 12);
                QuarterRound(workingBuffer, 2, 7, 8, 13);
                QuarterRound(workingBuffer, 3, 4, 9, 14);
            }

            for (int i = 0; i < StateLength; i++)
            {
                Util.ToBytes(temporaryBuffer, Util.Add(workingBuffer[i], stateToModify[i]), 4 * i);
            }

            stateToModify[12] = Util.AddOne(stateToModify[12]);
            if (stateToModify[12] <= 0)
            {
                /* Stopping at 2^70 bytes per nonce is the user's responsibility */
                stateToModify[13] = Util.AddOne(stateToModify[13]);
            }
        }

        /// <summary>
        /// The ChaCha Quarter Round operation. It operates on four 32-bit unsigned integers within the given buffer at indices a, b, c, and d.
        /// </summary>
        /// <remarks>
        /// The ChaCha state does not have four integer numbers: it has 16. So the quarter-round operation works on only four of them -- hence the name. Each quarter round operates on four predetermined numbers in the ChaCha state.
        /// See <a href="https://tools.ietf.org/html/rfc7539#page-4">ChaCha20 Spec Sections 2.1 - 2.2</a>.
        /// </remarks>
        /// <param name="x">A ChaCha state (vector). Must contain 16 elements.</param>
        /// <param name="a">Index of the first number</param>
        /// <param name="b">Index of the second number</param>
        /// <param name="c">Index of the third number</param>
        /// <param name="d">Index of the fourth number</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void QuarterRound(Span<uint> x, int a, int b, int c, int d)
        {
            x[a] = Util.Add(x[a], x[b]);
            x[d] = Util.Rotate(Util.XOr(x[d], x[a]), 16);

            x[c] = Util.Add(x[c], x[d]);
            x[b] = Util.Rotate(Util.XOr(x[b], x[c]), 12);

            x[a] = Util.Add(x[a], x[b]);
            x[d] = Util.Rotate(Util.XOr(x[d], x[a]), 8);

            x[c] = Util.Add(x[c], x[d]);
            x[b] = Util.Rotate(Util.XOr(x[b], x[c]), 7);
        }

        /// <summary>
        /// Utilities that are used during compression
        /// </summary>
        private static class Util
        {
            /// <summary>
            /// n-bit left rotation operation (towards the high bits) for 32-bit integers.
            /// </summary>
            /// <param name="v"></param>
            /// <param name="c"></param>
            /// <returns>The result of (v LEFTSHIFT c)</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint Rotate(uint v, int c)
            {
                unchecked
                {
                    return (v << c) | (v >> (32 - c));
                }
            }

            /// <summary>
            /// Unchecked integer exclusive or (XOR) operation.
            /// </summary>
            /// <param name="v"></param>
            /// <param name="w"></param>
            /// <returns>The result of (v XOR w)</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint XOr(uint v, uint w)
            {
                return unchecked(v ^ w);
            }

            /// <summary>
            /// Unchecked integer addition. The ChaCha spec defines certain operations to use 32-bit unsigned integer addition modulo 2^32.
            /// </summary>
            /// <remarks>
            /// See <a href="https://tools.ietf.org/html/rfc7539#page-4">ChaCha20 Spec Section 2.1</a>.
            /// </remarks>
            /// <param name="v"></param>
            /// <param name="w"></param>
            /// <returns>The result of (v + w) modulo 2^32</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint Add(uint v, uint w)
            {
                return unchecked(v + w);
            }

            /// <summary>
            /// Add 1 to the input parameter using unchecked integer addition. The ChaCha spec defines certain operations to use 32-bit unsigned integer addition modulo 2^32.
            /// </summary>
            /// <remarks>
            /// See <a href="https://tools.ietf.org/html/rfc7539#page-4">ChaCha20 Spec Section 2.1</a>.
            /// </remarks>
            /// <param name="v"></param>
            /// <returns>The result of (v + 1) modulo 2^32</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint AddOne(uint v)
            {
                return unchecked(v + 1);
            }

            /// <summary>
            /// Convert four bytes of the input buffer into an unsigned 32-bit integer, beginning at the inputOffset.
            /// </summary>
            /// <param name="p"></param>
            /// <param name="inputOffset"></param>
            /// <returns>An unsigned 32-bit integer</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint U8To32Little(ReadOnlySpan<byte> p, int inputOffset)
            {
                unchecked
                {
                    return ((uint)p[inputOffset]
                        | ((uint)p[inputOffset + 1] << 8)
                        | ((uint)p[inputOffset + 2] << 16)
                        | ((uint)p[inputOffset + 3] << 24));
                }
            }

            /// <summary>
            /// Serialize the input integer into the output buffer. The input integer will be split into 4 bytes and put into four sequential places in the output buffer, starting at the outputOffset.
            /// </summary>
            /// <param name="output"></param>
            /// <param name="input"></param>
            /// <param name="outputOffset"></param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ToBytes(Span<byte> output, uint input, int outputOffset)
            {
                unchecked
                {
                    output[outputOffset] = (byte)input;
                    output[outputOffset + 1] = (byte)(input >> 8);
                    output[outputOffset + 2] = (byte)(input >> 16);
                    output[outputOffset + 3] = (byte)(input >> 24);
                }
            }
        }
    }
}

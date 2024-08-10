﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace WzComparerR2.WzLib.Cryptography
{
    public class Snow2CryptoTransform : ICryptoTransform, IDisposable
    {
        public Snow2CryptoTransform(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, bool encrypting)
        {
            if (key == null) 
                throw new ArgumentNullException(nameof(key));
            if (key.Length != 16 && iv.Length != 32)
                throw new ArgumentOutOfRangeException(nameof(key), "Key size must be 16 or 32 bytes.");
            if (iv != null && iv.Length != 4)
                throw new ArgumentOutOfRangeException(nameof(iv), "Iv size must be 4 bytes.");

            this.encrypting = encrypting;
            this.keyStream = new uint[16];
            this.LoadKey(key, iv);
            this.RefreshKeyStream();
            this.curIndex = 0;
        }

        public int InputBlockSize => 4;

        public int OutputBlockSize => 4;

        public bool CanTransformMultipleBlocks => true;

        public bool CanReuseTransform => false;

        private bool encrypting;
        private uint s15, s14, s13, s12, s11, s10, s9, s8, s7, s6, s5, s4, s3, s2, s1, s0;
        private uint r1, r2;
        private uint[] keyStream;
        private int curIndex;

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

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Snow2CryptoTransform()
        {
            this.Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.keyStream != null)
                {
                    this.s15 = 0;
                    this.s14 = 0;
                    this.s13 = 0;
                    this.s12 = 0;
                    this.s11 = 0;
                    this.s10 = 0;
                    this.s9 = 0;
                    this.s8 = 0;
                    this.s7 = 0;
                    this.s6 = 0;
                    this.s5 = 0;
                    this.s4 = 0;
                    this.s3 = 0;
                    this.s2 = 0;
                    this.s1 = 0;
                    this.s0 = 0;
                    this.r1 = 0;
                    this.r2 = 0;
                    Array.Clear(this.keyStream, 0, this.keyStream.Length);
                    this.keyStream = null;
                }
            }
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

        private void TransformBlock(ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
        {
            ReadOnlySpan<uint> inputBlocks = MemoryMarshal.Cast<byte, uint>(input);
            Span<uint> outputBlocks = MemoryMarshal.Cast<byte, uint>(output);
            int i;
            for (i = 0; i < inputBlocks.Length; i++)
            {
                if (this.encrypting)
                {
                    outputBlocks[i] = inputBlocks[i] + this.keyStream[this.curIndex];
                }
                else
                {
                    outputBlocks[i] = inputBlocks[i] - this.keyStream[this.curIndex];
                }

                this.curIndex++;
                if (this.curIndex >= 16)
                {
                    this.RefreshKeyStream();
                    this.curIndex = 0;
                }
            }
            bytesWritten = i * 4;
        }

        #region Snow Cipher Algorithm
        // port from https://github.com/ahti/fsbench/blob/master/src/codecs/ecrypt/snow2_fast.c
        private void LoadKey(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
        {
            ReadOnlySpan<sbyte> signedKey = MemoryMarshal.Cast<byte, sbyte>(key);

            if (signedKey.Length == 16)
            {
                this.s15 = (uint)((signedKey[0] << 24) | (signedKey[1] << 16) | (signedKey[2] << 8) | (signedKey[3] << 0));
                this.s14 = (uint)((signedKey[4] << 24) | (signedKey[5] << 16) | (signedKey[6] << 8) | (signedKey[7] << 0));
                this.s13 = (uint)((signedKey[8] << 24) | (signedKey[9] << 16) | (signedKey[10] << 8) | (signedKey[11] << 0));
                this.s12 = (uint)((signedKey[12] << 24) | (signedKey[13] << 16) | (signedKey[14] << 8) | (signedKey[15] << 0));
                this.s11 = ~this.s15; /* bitwise inverse */
                this.s10 = ~this.s14;
                this.s9 = ~this.s13;
                this.s8 = ~this.s12;
                this.s7 = this.s15; /* just copy */
                this.s6 = this.s14;
                this.s5 = this.s13;
                this.s4 = this.s12;
                this.s3 = ~this.s15; /* bitwise inverse */
                this.s2 = ~this.s14;
                this.s1 = ~this.s13;
                this.s0 = ~this.s12;
            }
            else
            {
                /* assume keysize=256 */
                this.s15 = (uint)((signedKey[0] << 24) | (signedKey[1] << 16) | (signedKey[2] << 8) | (signedKey[3] << 0));
                this.s14 = (uint)((signedKey[4] << 24) | (signedKey[5] << 16) | (signedKey[6] << 8) | (signedKey[7] << 0));
                this.s13 = (uint)((signedKey[8] << 24) | (signedKey[9] << 16) | (signedKey[10] << 8) | (signedKey[11] << 0));
                this.s12 = (uint)((signedKey[12] << 24) | (signedKey[13] << 16) | (signedKey[14] << 8) | (signedKey[15] << 0));
                this.s11 = (uint)((signedKey[16] << 24) | (signedKey[17] << 16) | (signedKey[18] << 8) | (signedKey[19] << 0));
                this.s10 = (uint)((signedKey[20] << 24) | (signedKey[21] << 16) | (signedKey[22] << 8) | (signedKey[23] << 0));
                this.s9 = (uint)((signedKey[24] << 24) | (signedKey[25] << 16) | (signedKey[26] << 8) | (signedKey[27] << 0));
                this.s8 = (uint)((signedKey[28] << 24) | (signedKey[29] << 16) | (signedKey[30] << 8) | (signedKey[31] << 0));
                this.s7 = ~this.s15; /* bitwise inverse */
                this.s6 = ~this.s14;
                this.s5 = ~this.s13;
                this.s4 = ~this.s12;
                this.s3 = ~this.s11;
                this.s2 = ~this.s10;
                this.s1 = ~this.s9;
                this.s0 = ~this.s8;
            }

            /* XOR IV values */
            if (iv != null && iv.Length != 0)
            {
                this.s15 ^= iv[0];
                this.s12 ^= iv[1];
                this.s10 ^= iv[2];
                this.s9 ^= iv[3];
            }

            this.r1 = 0;
            this.r2 = 0;

            /* Do 32 initial clockings */
            for (int i = 0; i < 2; i++)
            {
                uint outfrom_fsm, fsmtmp;

                outfrom_fsm = (this.r1 + this.s15) ^ this.r2;
                this.s0 = a_mul(this.s0) ^ this.s2 ^ ainv_mul(this.s11) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s5;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s0) ^ this.r2;
                this.s1 = a_mul(this.s1) ^ this.s3 ^ ainv_mul(this.s12) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s6;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s1) ^ this.r2;
                this.s2 = a_mul(this.s2) ^ this.s4 ^ ainv_mul(this.s13) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s7;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s2) ^ this.r2;
                this.s3 = a_mul(this.s3) ^ this.s5 ^ ainv_mul(this.s14) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s8;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s3) ^ this.r2;
                this.s4 = a_mul(this.s4) ^ this.s6 ^ ainv_mul(this.s15) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s9;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s4) ^ this.r2;
                this.s5 = a_mul(this.s5) ^ this.s7 ^ ainv_mul(this.s0) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s10;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s5) ^ this.r2;
                this.s6 = a_mul(this.s6) ^ this.s8 ^ ainv_mul(this.s1) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s11;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s6) ^ this.r2;
                this.s7 = a_mul(this.s7) ^ this.s9 ^ ainv_mul(this.s2) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s12;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s7) ^ this.r2;
                this.s8 = a_mul(this.s8) ^ this.s10 ^ ainv_mul(this.s3) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s13;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s8) ^ this.r2;
                this.s9 = a_mul(this.s9) ^ this.s11 ^ ainv_mul(this.s4) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s14;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s9) ^ this.r2;
                this.s10 = a_mul(this.s10) ^ this.s12 ^ ainv_mul(this.s5) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s15;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s10) ^ this.r2;
                this.s11 = a_mul(this.s11) ^ this.s13 ^ ainv_mul(this.s6) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s0;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s11) ^ this.r2;
                this.s12 = a_mul(this.s12) ^ this.s14 ^ ainv_mul(this.s7) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s1;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s12) ^ this.r2;
                this.s13 = a_mul(this.s13) ^ this.s15 ^ ainv_mul(this.s8) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s2;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s13) ^ this.r2;
                this.s14 = a_mul(this.s14) ^ this.s0 ^ ainv_mul(this.s9) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s3;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;

                outfrom_fsm = (this.r1 + this.s14) ^ this.r2;
                this.s15 = a_mul(this.s15) ^ this.s1 ^ ainv_mul(this.s10) ^ outfrom_fsm;
                fsmtmp = this.r2 + this.s4;
                this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
                this.r1 = fsmtmp;
            }
        }

        private void RefreshKeyStream()
        {
            uint fsmtmp;

            this.s0 = a_mul(this.s0) ^ this.s2 ^ ainv_mul(this.s11);
            fsmtmp = this.r2 + this.s5;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[0] = (this.r1 + this.s0) ^ this.r2 ^ this.s1;

            this.s1 = a_mul(this.s1) ^ this.s3 ^ ainv_mul(this.s12);
            fsmtmp = this.r2 + this.s6;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[1] = (this.r1 + this.s1) ^ this.r2 ^ this.s2;

            this.s2 = a_mul(this.s2) ^ this.s4 ^ ainv_mul(this.s13);
            fsmtmp = this.r2 + this.s7;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[2] = (this.r1 + this.s2) ^ this.r2 ^ this.s3;

            this.s3 = a_mul(this.s3) ^ this.s5 ^ ainv_mul(this.s14);
            fsmtmp = this.r2 + this.s8;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[3] = (this.r1 + this.s3) ^ this.r2 ^ this.s4;

            this.s4 = a_mul(this.s4) ^ this.s6 ^ ainv_mul(this.s15);
            fsmtmp = this.r2 + this.s9;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[4] = (this.r1 + this.s4) ^ this.r2 ^ this.s5;

            this.s5 = a_mul(this.s5) ^ this.s7 ^ ainv_mul(this.s0);
            fsmtmp = this.r2 + this.s10;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[5] = (this.r1 + this.s5) ^ this.r2 ^ this.s6;

            this.s6 = a_mul(this.s6) ^ this.s8 ^ ainv_mul(this.s1);
            fsmtmp = this.r2 + this.s11;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[6] = (this.r1 + this.s6) ^ this.r2 ^ this.s7;

            this.s7 = a_mul(this.s7) ^ this.s9 ^ ainv_mul(this.s2);
            fsmtmp = this.r2 + this.s12;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[7] = (this.r1 + this.s7) ^ this.r2 ^ this.s8;

            this.s8 = a_mul(this.s8) ^ this.s10 ^ ainv_mul(this.s3);
            fsmtmp = this.r2 + this.s13;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[8] = (this.r1 + this.s8) ^ this.r2 ^ this.s9;

            this.s9 = a_mul(this.s9) ^ this.s11 ^ ainv_mul(this.s4);
            fsmtmp = this.r2 + this.s14;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[9] = (this.r1 + this.s9) ^ this.r2 ^ this.s10;

            this.s10 = a_mul(this.s10) ^ this.s12 ^ ainv_mul(this.s5);
            fsmtmp = this.r2 + this.s15;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[10] = (this.r1 + this.s10) ^ this.r2 ^ this.s11;

            this.s11 = a_mul(this.s11) ^ this.s13 ^ ainv_mul(this.s6);
            fsmtmp = this.r2 + this.s0;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[11] = (this.r1 + this.s11) ^ this.r2 ^ this.s12;

            this.s12 = a_mul(this.s12) ^ this.s14 ^ ainv_mul(this.s7);
            fsmtmp = this.r2 + this.s1;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[12] = (this.r1 + this.s12) ^ this.r2 ^ this.s13;

            this.s13 = a_mul(this.s13) ^ this.s15 ^ ainv_mul(this.s8);
            fsmtmp = this.r2 + this.s2;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[13] = (this.r1 + this.s13) ^ this.r2 ^ this.s14;

            this.s14 = a_mul(this.s14) ^ this.s0 ^ ainv_mul(this.s9);
            fsmtmp = this.r2 + this.s3;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[14] = (this.r1 + this.s14) ^ this.r2 ^ this.s15;

            this.s15 = a_mul(this.s15) ^ this.s1 ^ ainv_mul(this.s10);
            fsmtmp = this.r2 + this.s4;
            this.r2 = snow_T0[@byte(0, this.r1)] ^ snow_T1[@byte(1, this.r1)] ^ snow_T2[@byte(2, this.r1)] ^ snow_T3[@byte(3, this.r1)];
            this.r1 = fsmtmp;
            this.keyStream[15] = (this.r1 + this.s15) ^ this.r2 ^ this.s0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte @byte(int n, uint w) => (byte)(((w) >> (n * 8)) & 0xff);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ainv_mul(uint w) => (((w) >> 8) ^ (snow_alphainv_mul[w & 0xff]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint a_mul(uint w) => (((w) << 8) ^ (snow_alpha_mul[w >> 24]));

        private static uint[] snow_alpha_mul = {
                    0x0,0xE19FCF13,0x6B973726,0x8A08F835,0xD6876E4C,0x3718A15F,0xBD10596A,0x5C8F9679,
              0x5A7DC98,0xE438138B,0x6E30EBBE,0x8FAF24AD,0xD320B2D4,0x32BF7DC7,0xB8B785F2,0x59284AE1,
              0xAE71199,0xEB78DE8A,0x617026BF,0x80EFE9AC,0xDC607FD5,0x3DFFB0C6,0xB7F748F3,0x566887E0,
              0xF40CD01,0xEEDF0212,0x64D7FA27,0x85483534,0xD9C7A34D,0x38586C5E,0xB250946B,0x53CF5B78,
             0x1467229B,0xF5F8ED88,0x7FF015BD,0x9E6FDAAE,0xC2E04CD7,0x237F83C4,0xA9777BF1,0x48E8B4E2,
             0x11C0FE03,0xF05F3110,0x7A57C925,0x9BC80636,0xC747904F,0x26D85F5C,0xACD0A769,0x4D4F687A,
             0x1E803302,0xFF1FFC11,0x75170424,0x9488CB37,0xC8075D4E,0x2998925D,0xA3906A68,0x420FA57B,
             0x1B27EF9A,0xFAB82089,0x70B0D8BC,0x912F17AF,0xCDA081D6,0x2C3F4EC5,0xA637B6F0,0x47A879E3,
             0x28CE449F,0xC9518B8C,0x435973B9,0xA2C6BCAA,0xFE492AD3,0x1FD6E5C0,0x95DE1DF5,0x7441D2E6,
             0x2D699807,0xCCF65714,0x46FEAF21,0xA7616032,0xFBEEF64B,0x1A713958,0x9079C16D,0x71E60E7E,
             0x22295506,0xC3B69A15,0x49BE6220,0xA821AD33,0xF4AE3B4A,0x1531F459,0x9F390C6C,0x7EA6C37F,
             0x278E899E,0xC611468D,0x4C19BEB8,0xAD8671AB,0xF109E7D2,0x109628C1,0x9A9ED0F4,0x7B011FE7,
             0x3CA96604,0xDD36A917,0x573E5122,0xB6A19E31,0xEA2E0848, 0xBB1C75B,0x81B93F6E,0x6026F07D,
             0x390EBA9C,0xD891758F,0x52998DBA,0xB30642A9,0xEF89D4D0, 0xE161BC3,0x841EE3F6,0x65812CE5,
             0x364E779D,0xD7D1B88E,0x5DD940BB,0xBC468FA8,0xE0C919D1, 0x156D6C2,0x8B5E2EF7,0x6AC1E1E4,
             0x33E9AB05,0xD2766416,0x587E9C23,0xB9E15330,0xE56EC549, 0x4F10A5A,0x8EF9F26F,0x6F663D7C,
             0x50358897,0xB1AA4784,0x3BA2BFB1,0xDA3D70A2,0x86B2E6DB,0x672D29C8,0xED25D1FD, 0xCBA1EEE,
             0x5592540F,0xB40D9B1C,0x3E056329,0xDF9AAC3A,0x83153A43,0x628AF550,0xE8820D65, 0x91DC276,
             0x5AD2990E,0xBB4D561D,0x3145AE28,0xD0DA613B,0x8C55F742,0x6DCA3851,0xE7C2C064, 0x65D0F77,
             0x5F754596,0xBEEA8A85,0x34E272B0,0xD57DBDA3,0x89F22BDA,0x686DE4C9,0xE2651CFC, 0x3FAD3EF,
             0x4452AA0C,0xA5CD651F,0x2FC59D2A,0xCE5A5239,0x92D5C440,0x734A0B53,0xF942F366,0x18DD3C75,
             0x41F57694,0xA06AB987,0x2A6241B2,0xCBFD8EA1,0x977218D8,0x76EDD7CB,0xFCE52FFE,0x1D7AE0ED,
             0x4EB5BB95,0xAF2A7486,0x25228CB3,0xC4BD43A0,0x9832D5D9,0x79AD1ACA,0xF3A5E2FF,0x123A2DEC,
             0x4B12670D,0xAA8DA81E,0x2085502B,0xC11A9F38,0x9D950941,0x7C0AC652,0xF6023E67,0x179DF174,
             0x78FBCC08,0x9964031B,0x136CFB2E,0xF2F3343D,0xAE7CA244,0x4FE36D57,0xC5EB9562,0x24745A71,
             0x7D5C1090,0x9CC3DF83,0x16CB27B6,0xF754E8A5,0xABDB7EDC,0x4A44B1CF,0xC04C49FA,0x21D386E9,
             0x721CDD91,0x93831282,0x198BEAB7,0xF81425A4,0xA49BB3DD,0x45047CCE,0xCF0C84FB,0x2E934BE8,
             0x77BB0109,0x9624CE1A,0x1C2C362F,0xFDB3F93C,0xA13C6F45,0x40A3A056,0xCAAB5863,0x2B349770,
             0x6C9CEE93,0x8D032180, 0x70BD9B5,0xE69416A6,0xBA1B80DF,0x5B844FCC,0xD18CB7F9,0x301378EA,
             0x693B320B,0x88A4FD18, 0x2AC052D,0xE333CA3E,0xBFBC5C47,0x5E239354,0xD42B6B61,0x35B4A472,
             0x667BFF0A,0x87E43019, 0xDECC82C,0xEC73073F,0xB0FC9146,0x51635E55,0xDB6BA660,0x3AF46973,
             0x63DC2392,0x8243EC81, 0x84B14B4,0xE9D4DBA7,0xB55B4DDE,0x54C482CD,0xDECC7AF8,0x3F53B5EB};

        static uint[] snow_alphainv_mul = {
                    0x0,0x180F40CD,0x301E8033,0x2811C0FE,0x603CA966,0x7833E9AB,0x50222955,0x482D6998,
             0xC078FBCC,0xD877BB01,0xF0667BFF,0xE8693B32,0xA04452AA,0xB84B1267,0x905AD299,0x88559254,
             0x29F05F31,0x31FF1FFC,0x19EEDF02, 0x1E19FCF,0x49CCF657,0x51C3B69A,0x79D27664,0x61DD36A9,
             0xE988A4FD,0xF187E430,0xD99624CE,0xC1996403,0x89B40D9B,0x91BB4D56,0xB9AA8DA8,0xA1A5CD65,
             0x5249BE62,0x4A46FEAF,0x62573E51,0x7A587E9C,0x32751704,0x2A7A57C9, 0x26B9737,0x1A64D7FA,
             0x923145AE,0x8A3E0563,0xA22FC59D,0xBA208550,0xF20DECC8,0xEA02AC05,0xC2136CFB,0xDA1C2C36,
             0x7BB9E153,0x63B6A19E,0x4BA76160,0x53A821AD,0x1B854835, 0x38A08F8,0x2B9BC806,0x339488CB,
             0xBBC11A9F,0xA3CE5A52,0x8BDF9AAC,0x93D0DA61,0xDBFDB3F9,0xC3F2F334,0xEBE333CA,0xF3EC7307,
             0xA492D5C4,0xBC9D9509,0x948C55F7,0x8C83153A,0xC4AE7CA2,0xDCA13C6F,0xF4B0FC91,0xECBFBC5C,
             0x64EA2E08,0x7CE56EC5,0x54F4AE3B,0x4CFBEEF6, 0x4D6876E,0x1CD9C7A3,0x34C8075D,0x2CC74790,
             0x8D628AF5,0x956DCA38,0xBD7C0AC6,0xA5734A0B,0xED5E2393,0xF551635E,0xDD40A3A0,0xC54FE36D,
             0x4D1A7139,0x551531F4,0x7D04F10A,0x650BB1C7,0x2D26D85F,0x35299892,0x1D38586C, 0x53718A1,
             0xF6DB6BA6,0xEED42B6B,0xC6C5EB95,0xDECAAB58,0x96E7C2C0,0x8EE8820D,0xA6F942F3,0xBEF6023E,
             0x36A3906A,0x2EACD0A7, 0x6BD1059,0x1EB25094,0x569F390C,0x4E9079C1,0x6681B93F,0x7E8EF9F2,
             0xDF2B3497,0xC724745A,0xEF35B4A4,0xF73AF469,0xBF179DF1,0xA718DD3C,0x8F091DC2,0x97065D0F,
             0x1F53CF5B, 0x75C8F96,0x2F4D4F68,0x37420FA5,0x7F6F663D,0x676026F0,0x4F71E60E,0x577EA6C3,
             0xE18D0321,0xF98243EC,0xD1938312,0xC99CC3DF,0x81B1AA47,0x99BEEA8A,0xB1AF2A74,0xA9A06AB9,
             0x21F5F8ED,0x39FAB820,0x11EB78DE, 0x9E43813,0x41C9518B,0x59C61146,0x71D7D1B8,0x69D89175,
             0xC87D5C10,0xD0721CDD,0xF863DC23,0xE06C9CEE,0xA841F576,0xB04EB5BB,0x985F7545,0x80503588,
              0x805A7DC,0x100AE711,0x381B27EF,0x20146722,0x68390EBA,0x70364E77,0x58278E89,0x4028CE44,
             0xB3C4BD43,0xABCBFD8E,0x83DA3D70,0x9BD57DBD,0xD3F81425,0xCBF754E8,0xE3E69416,0xFBE9D4DB,
             0x73BC468F,0x6BB30642,0x43A2C6BC,0x5BAD8671,0x1380EFE9, 0xB8FAF24,0x239E6FDA,0x3B912F17,
             0x9A34E272,0x823BA2BF,0xAA2A6241,0xB225228C,0xFA084B14,0xE2070BD9,0xCA16CB27,0xD2198BEA,
             0x5A4C19BE,0x42435973,0x6A52998D,0x725DD940,0x3A70B0D8,0x227FF015, 0xA6E30EB,0x12617026,
             0x451FD6E5,0x5D109628,0x750156D6,0x6D0E161B,0x25237F83,0x3D2C3F4E,0x153DFFB0, 0xD32BF7D,
             0x85672D29,0x9D686DE4,0xB579AD1A,0xAD76EDD7,0xE55B844F,0xFD54C482,0xD545047C,0xCD4A44B1,
             0x6CEF89D4,0x74E0C919,0x5CF109E7,0x44FE492A, 0xCD320B2,0x14DC607F,0x3CCDA081,0x24C2E04C,
             0xAC977218,0xB49832D5,0x9C89F22B,0x8486B2E6,0xCCABDB7E,0xD4A49BB3,0xFCB55B4D,0xE4BA1B80,
             0x17566887, 0xF59284A,0x2748E8B4,0x3F47A879,0x776AC1E1,0x6F65812C,0x477441D2,0x5F7B011F,
             0xD72E934B,0xCF21D386,0xE7301378,0xFF3F53B5,0xB7123A2D,0xAF1D7AE0,0x870CBA1E,0x9F03FAD3,
             0x3EA637B6,0x26A9777B, 0xEB8B785,0x16B7F748,0x5E9A9ED0,0x4695DE1D,0x6E841EE3,0x768B5E2E,
             0xFEDECC7A,0xE6D18CB7,0xCEC04C49,0xD6CF0C84,0x9EE2651C,0x86ED25D1,0xAEFCE52F,0xB6F3A5E2};

        static uint[] snow_T0 = {
             0xa56363c6,0x847c7cf8,0x997777ee,0x8d7b7bf6, 0xdf2f2ff,0xbd6b6bd6,0xb16f6fde,0x54c5c591,
             0x50303060, 0x3010102,0xa96767ce,0x7d2b2b56,0x19fefee7,0x62d7d7b5,0xe6abab4d,0x9a7676ec,
             0x45caca8f,0x9d82821f,0x40c9c989,0x877d7dfa,0x15fafaef,0xeb5959b2,0xc947478e, 0xbf0f0fb,
             0xecadad41,0x67d4d4b3,0xfda2a25f,0xeaafaf45,0xbf9c9c23,0xf7a4a453,0x967272e4,0x5bc0c09b,
             0xc2b7b775,0x1cfdfde1,0xae93933d,0x6a26264c,0x5a36366c,0x413f3f7e, 0x2f7f7f5,0x4fcccc83,
             0x5c343468,0xf4a5a551,0x34e5e5d1, 0x8f1f1f9,0x937171e2,0x73d8d8ab,0x53313162,0x3f15152a,
              0xc040408,0x52c7c795,0x65232346,0x5ec3c39d,0x28181830,0xa1969637, 0xf05050a,0xb59a9a2f,
              0x907070e,0x36121224,0x9b80801b,0x3de2e2df,0x26ebebcd,0x6927274e,0xcdb2b27f,0x9f7575ea,
             0x1b090912,0x9e83831d,0x742c2c58,0x2e1a1a34,0x2d1b1b36,0xb26e6edc,0xee5a5ab4,0xfba0a05b,
             0xf65252a4,0x4d3b3b76,0x61d6d6b7,0xceb3b37d,0x7b292952,0x3ee3e3dd,0x712f2f5e,0x97848413,
             0xf55353a6,0x68d1d1b9,       0x0,0x2cededc1,0x60202040,0x1ffcfce3,0xc8b1b179,0xed5b5bb6,
             0xbe6a6ad4,0x46cbcb8d,0xd9bebe67,0x4b393972,0xde4a4a94,0xd44c4c98,0xe85858b0,0x4acfcf85,
             0x6bd0d0bb,0x2aefefc5,0xe5aaaa4f,0x16fbfbed,0xc5434386,0xd74d4d9a,0x55333366,0x94858511,
             0xcf45458a,0x10f9f9e9, 0x6020204,0x817f7ffe,0xf05050a0,0x443c3c78,0xba9f9f25,0xe3a8a84b,
             0xf35151a2,0xfea3a35d,0xc0404080,0x8a8f8f05,0xad92923f,0xbc9d9d21,0x48383870, 0x4f5f5f1,
             0xdfbcbc63,0xc1b6b677,0x75dadaaf,0x63212142,0x30101020,0x1affffe5, 0xef3f3fd,0x6dd2d2bf,
             0x4ccdcd81,0x140c0c18,0x35131326,0x2fececc3,0xe15f5fbe,0xa2979735,0xcc444488,0x3917172e,
             0x57c4c493,0xf2a7a755,0x827e7efc,0x473d3d7a,0xac6464c8,0xe75d5dba,0x2b191932,0x957373e6,
             0xa06060c0,0x98818119,0xd14f4f9e,0x7fdcdca3,0x66222244,0x7e2a2a54,0xab90903b,0x8388880b,
             0xca46468c,0x29eeeec7,0xd3b8b86b,0x3c141428,0x79dedea7,0xe25e5ebc,0x1d0b0b16,0x76dbdbad,
             0x3be0e0db,0x56323264,0x4e3a3a74,0x1e0a0a14,0xdb494992, 0xa06060c,0x6c242448,0xe45c5cb8,
             0x5dc2c29f,0x6ed3d3bd,0xefacac43,0xa66262c4,0xa8919139,0xa4959531,0x37e4e4d3,0x8b7979f2,
             0x32e7e7d5,0x43c8c88b,0x5937376e,0xb76d6dda,0x8c8d8d01,0x64d5d5b1,0xd24e4e9c,0xe0a9a949,
             0xb46c6cd8,0xfa5656ac, 0x7f4f4f3,0x25eaeacf,0xaf6565ca,0x8e7a7af4,0xe9aeae47,0x18080810,
             0xd5baba6f,0x887878f0,0x6f25254a,0x722e2e5c,0x241c1c38,0xf1a6a657,0xc7b4b473,0x51c6c697,
             0x23e8e8cb,0x7cdddda1,0x9c7474e8,0x211f1f3e,0xdd4b4b96,0xdcbdbd61,0x868b8b0d,0x858a8a0f,
             0x907070e0,0x423e3e7c,0xc4b5b571,0xaa6666cc,0xd8484890, 0x5030306, 0x1f6f6f7,0x120e0e1c,
             0xa36161c2,0x5f35356a,0xf95757ae,0xd0b9b969,0x91868617,0x58c1c199,0x271d1d3a,0xb99e9e27,
             0x38e1e1d9,0x13f8f8eb,0xb398982b,0x33111122,0xbb6969d2,0x70d9d9a9,0x898e8e07,0xa7949433,
             0xb69b9b2d,0x221e1e3c,0x92878715,0x20e9e9c9,0x49cece87,0xff5555aa,0x78282850,0x7adfdfa5,
             0x8f8c8c03,0xf8a1a159,0x80898909,0x170d0d1a,0xdabfbf65,0x31e6e6d7,0xc6424284,0xb86868d0,
             0xc3414182,0xb0999929,0x772d2d5a,0x110f0f1e,0xcbb0b07b,0xfc5454a8,0xd6bbbb6d,0x3a16162c};

        static uint[] snow_T1 = {
             0x6363c6a5,0x7c7cf884,0x7777ee99,0x7b7bf68d,0xf2f2ff0d,0x6b6bd6bd,0x6f6fdeb1,0xc5c59154,
             0x30306050, 0x1010203,0x6767cea9,0x2b2b567d,0xfefee719,0xd7d7b562,0xabab4de6,0x7676ec9a,
             0xcaca8f45,0x82821f9d,0xc9c98940,0x7d7dfa87,0xfafaef15,0x5959b2eb,0x47478ec9,0xf0f0fb0b,
             0xadad41ec,0xd4d4b367,0xa2a25ffd,0xafaf45ea,0x9c9c23bf,0xa4a453f7,0x7272e496,0xc0c09b5b,
             0xb7b775c2,0xfdfde11c,0x93933dae,0x26264c6a,0x36366c5a,0x3f3f7e41,0xf7f7f502,0xcccc834f,
             0x3434685c,0xa5a551f4,0xe5e5d134,0xf1f1f908,0x7171e293,0xd8d8ab73,0x31316253,0x15152a3f,
              0x404080c,0xc7c79552,0x23234665,0xc3c39d5e,0x18183028,0x969637a1, 0x5050a0f,0x9a9a2fb5,
              0x7070e09,0x12122436,0x80801b9b,0xe2e2df3d,0xebebcd26,0x27274e69,0xb2b27fcd,0x7575ea9f,
              0x909121b,0x83831d9e,0x2c2c5874,0x1a1a342e,0x1b1b362d,0x6e6edcb2,0x5a5ab4ee,0xa0a05bfb,
             0x5252a4f6,0x3b3b764d,0xd6d6b761,0xb3b37dce,0x2929527b,0xe3e3dd3e,0x2f2f5e71,0x84841397,
             0x5353a6f5,0xd1d1b968,       0x0,0xededc12c,0x20204060,0xfcfce31f,0xb1b179c8,0x5b5bb6ed,
             0x6a6ad4be,0xcbcb8d46,0xbebe67d9,0x3939724b,0x4a4a94de,0x4c4c98d4,0x5858b0e8,0xcfcf854a,
             0xd0d0bb6b,0xefefc52a,0xaaaa4fe5,0xfbfbed16,0x434386c5,0x4d4d9ad7,0x33336655,0x85851194,
             0x45458acf,0xf9f9e910, 0x2020406,0x7f7ffe81,0x5050a0f0,0x3c3c7844,0x9f9f25ba,0xa8a84be3,
             0x5151a2f3,0xa3a35dfe,0x404080c0,0x8f8f058a,0x92923fad,0x9d9d21bc,0x38387048,0xf5f5f104,
             0xbcbc63df,0xb6b677c1,0xdadaaf75,0x21214263,0x10102030,0xffffe51a,0xf3f3fd0e,0xd2d2bf6d,
             0xcdcd814c, 0xc0c1814,0x13132635,0xececc32f,0x5f5fbee1,0x979735a2,0x444488cc,0x17172e39,
             0xc4c49357,0xa7a755f2,0x7e7efc82,0x3d3d7a47,0x6464c8ac,0x5d5dbae7,0x1919322b,0x7373e695,
             0x6060c0a0,0x81811998,0x4f4f9ed1,0xdcdca37f,0x22224466,0x2a2a547e,0x90903bab,0x88880b83,
             0x46468cca,0xeeeec729,0xb8b86bd3,0x1414283c,0xdedea779,0x5e5ebce2, 0xb0b161d,0xdbdbad76,
             0xe0e0db3b,0x32326456,0x3a3a744e, 0xa0a141e,0x494992db, 0x6060c0a,0x2424486c,0x5c5cb8e4,
             0xc2c29f5d,0xd3d3bd6e,0xacac43ef,0x6262c4a6,0x919139a8,0x959531a4,0xe4e4d337,0x7979f28b,
             0xe7e7d532,0xc8c88b43,0x37376e59,0x6d6ddab7,0x8d8d018c,0xd5d5b164,0x4e4e9cd2,0xa9a949e0,
             0x6c6cd8b4,0x5656acfa,0xf4f4f307,0xeaeacf25,0x6565caaf,0x7a7af48e,0xaeae47e9, 0x8081018,
             0xbaba6fd5,0x7878f088,0x25254a6f,0x2e2e5c72,0x1c1c3824,0xa6a657f1,0xb4b473c7,0xc6c69751,
             0xe8e8cb23,0xdddda17c,0x7474e89c,0x1f1f3e21,0x4b4b96dd,0xbdbd61dc,0x8b8b0d86,0x8a8a0f85,
             0x7070e090,0x3e3e7c42,0xb5b571c4,0x6666ccaa,0x484890d8, 0x3030605,0xf6f6f701, 0xe0e1c12,
             0x6161c2a3,0x35356a5f,0x5757aef9,0xb9b969d0,0x86861791,0xc1c19958,0x1d1d3a27,0x9e9e27b9,
             0xe1e1d938,0xf8f8eb13,0x98982bb3,0x11112233,0x6969d2bb,0xd9d9a970,0x8e8e0789,0x949433a7,
             0x9b9b2db6,0x1e1e3c22,0x87871592,0xe9e9c920,0xcece8749,0x5555aaff,0x28285078,0xdfdfa57a,
             0x8c8c038f,0xa1a159f8,0x89890980, 0xd0d1a17,0xbfbf65da,0xe6e6d731,0x424284c6,0x6868d0b8,
             0x414182c3,0x999929b0,0x2d2d5a77, 0xf0f1e11,0xb0b07bcb,0x5454a8fc,0xbbbb6dd6,0x16162c3a};

        static uint[] snow_T2 = {
             0x63c6a563,0x7cf8847c,0x77ee9977,0x7bf68d7b,0xf2ff0df2,0x6bd6bd6b,0x6fdeb16f,0xc59154c5,
             0x30605030, 0x1020301,0x67cea967,0x2b567d2b,0xfee719fe,0xd7b562d7,0xab4de6ab,0x76ec9a76,
             0xca8f45ca,0x821f9d82,0xc98940c9,0x7dfa877d,0xfaef15fa,0x59b2eb59,0x478ec947,0xf0fb0bf0,
             0xad41ecad,0xd4b367d4,0xa25ffda2,0xaf45eaaf,0x9c23bf9c,0xa453f7a4,0x72e49672,0xc09b5bc0,
             0xb775c2b7,0xfde11cfd,0x933dae93,0x264c6a26,0x366c5a36,0x3f7e413f,0xf7f502f7,0xcc834fcc,
             0x34685c34,0xa551f4a5,0xe5d134e5,0xf1f908f1,0x71e29371,0xd8ab73d8,0x31625331,0x152a3f15,
              0x4080c04,0xc79552c7,0x23466523,0xc39d5ec3,0x18302818,0x9637a196, 0x50a0f05,0x9a2fb59a,
              0x70e0907,0x12243612,0x801b9b80,0xe2df3de2,0xebcd26eb,0x274e6927,0xb27fcdb2,0x75ea9f75,
              0x9121b09,0x831d9e83,0x2c58742c,0x1a342e1a,0x1b362d1b,0x6edcb26e,0x5ab4ee5a,0xa05bfba0,
             0x52a4f652,0x3b764d3b,0xd6b761d6,0xb37dceb3,0x29527b29,0xe3dd3ee3,0x2f5e712f,0x84139784,
             0x53a6f553,0xd1b968d1,       0x0,0xedc12ced,0x20406020,0xfce31ffc,0xb179c8b1,0x5bb6ed5b,
             0x6ad4be6a,0xcb8d46cb,0xbe67d9be,0x39724b39,0x4a94de4a,0x4c98d44c,0x58b0e858,0xcf854acf,
             0xd0bb6bd0,0xefc52aef,0xaa4fe5aa,0xfbed16fb,0x4386c543,0x4d9ad74d,0x33665533,0x85119485,
             0x458acf45,0xf9e910f9, 0x2040602,0x7ffe817f,0x50a0f050,0x3c78443c,0x9f25ba9f,0xa84be3a8,
             0x51a2f351,0xa35dfea3,0x4080c040,0x8f058a8f,0x923fad92,0x9d21bc9d,0x38704838,0xf5f104f5,
             0xbc63dfbc,0xb677c1b6,0xdaaf75da,0x21426321,0x10203010,0xffe51aff,0xf3fd0ef3,0xd2bf6dd2,
             0xcd814ccd, 0xc18140c,0x13263513,0xecc32fec,0x5fbee15f,0x9735a297,0x4488cc44,0x172e3917,
             0xc49357c4,0xa755f2a7,0x7efc827e,0x3d7a473d,0x64c8ac64,0x5dbae75d,0x19322b19,0x73e69573,
             0x60c0a060,0x81199881,0x4f9ed14f,0xdca37fdc,0x22446622,0x2a547e2a,0x903bab90,0x880b8388,
             0x468cca46,0xeec729ee,0xb86bd3b8,0x14283c14,0xdea779de,0x5ebce25e, 0xb161d0b,0xdbad76db,
             0xe0db3be0,0x32645632,0x3a744e3a, 0xa141e0a,0x4992db49, 0x60c0a06,0x24486c24,0x5cb8e45c,
             0xc29f5dc2,0xd3bd6ed3,0xac43efac,0x62c4a662,0x9139a891,0x9531a495,0xe4d337e4,0x79f28b79,
             0xe7d532e7,0xc88b43c8,0x376e5937,0x6ddab76d,0x8d018c8d,0xd5b164d5,0x4e9cd24e,0xa949e0a9,
             0x6cd8b46c,0x56acfa56,0xf4f307f4,0xeacf25ea,0x65caaf65,0x7af48e7a,0xae47e9ae, 0x8101808,
             0xba6fd5ba,0x78f08878,0x254a6f25,0x2e5c722e,0x1c38241c,0xa657f1a6,0xb473c7b4,0xc69751c6,
             0xe8cb23e8,0xdda17cdd,0x74e89c74,0x1f3e211f,0x4b96dd4b,0xbd61dcbd,0x8b0d868b,0x8a0f858a,
             0x70e09070,0x3e7c423e,0xb571c4b5,0x66ccaa66,0x4890d848, 0x3060503,0xf6f701f6, 0xe1c120e,
             0x61c2a361,0x356a5f35,0x57aef957,0xb969d0b9,0x86179186,0xc19958c1,0x1d3a271d,0x9e27b99e,
             0xe1d938e1,0xf8eb13f8,0x982bb398,0x11223311,0x69d2bb69,0xd9a970d9,0x8e07898e,0x9433a794,
             0x9b2db69b,0x1e3c221e,0x87159287,0xe9c920e9,0xce8749ce,0x55aaff55,0x28507828,0xdfa57adf,
             0x8c038f8c,0xa159f8a1,0x89098089, 0xd1a170d,0xbf65dabf,0xe6d731e6,0x4284c642,0x68d0b868,
             0x4182c341,0x9929b099,0x2d5a772d, 0xf1e110f,0xb07bcbb0,0x54a8fc54,0xbb6dd6bb,0x162c3a16};

        static uint[] snow_T3 = {
             0xc6a56363,0xf8847c7c,0xee997777,0xf68d7b7b,0xff0df2f2,0xd6bd6b6b,0xdeb16f6f,0x9154c5c5,
             0x60503030, 0x2030101,0xcea96767,0x567d2b2b,0xe719fefe,0xb562d7d7,0x4de6abab,0xec9a7676,
             0x8f45caca,0x1f9d8282,0x8940c9c9,0xfa877d7d,0xef15fafa,0xb2eb5959,0x8ec94747,0xfb0bf0f0,
             0x41ecadad,0xb367d4d4,0x5ffda2a2,0x45eaafaf,0x23bf9c9c,0x53f7a4a4,0xe4967272,0x9b5bc0c0,
             0x75c2b7b7,0xe11cfdfd,0x3dae9393,0x4c6a2626,0x6c5a3636,0x7e413f3f,0xf502f7f7,0x834fcccc,
             0x685c3434,0x51f4a5a5,0xd134e5e5,0xf908f1f1,0xe2937171,0xab73d8d8,0x62533131,0x2a3f1515,
              0x80c0404,0x9552c7c7,0x46652323,0x9d5ec3c3,0x30281818,0x37a19696, 0xa0f0505,0x2fb59a9a,
              0xe090707,0x24361212,0x1b9b8080,0xdf3de2e2,0xcd26ebeb,0x4e692727,0x7fcdb2b2,0xea9f7575,
             0x121b0909,0x1d9e8383,0x58742c2c,0x342e1a1a,0x362d1b1b,0xdcb26e6e,0xb4ee5a5a,0x5bfba0a0,
             0xa4f65252,0x764d3b3b,0xb761d6d6,0x7dceb3b3,0x527b2929,0xdd3ee3e3,0x5e712f2f,0x13978484,
             0xa6f55353,0xb968d1d1,       0x0,0xc12ceded,0x40602020,0xe31ffcfc,0x79c8b1b1,0xb6ed5b5b,
             0xd4be6a6a,0x8d46cbcb,0x67d9bebe,0x724b3939,0x94de4a4a,0x98d44c4c,0xb0e85858,0x854acfcf,
             0xbb6bd0d0,0xc52aefef,0x4fe5aaaa,0xed16fbfb,0x86c54343,0x9ad74d4d,0x66553333,0x11948585,
             0x8acf4545,0xe910f9f9, 0x4060202,0xfe817f7f,0xa0f05050,0x78443c3c,0x25ba9f9f,0x4be3a8a8,
             0xa2f35151,0x5dfea3a3,0x80c04040, 0x58a8f8f,0x3fad9292,0x21bc9d9d,0x70483838,0xf104f5f5,
             0x63dfbcbc,0x77c1b6b6,0xaf75dada,0x42632121,0x20301010,0xe51affff,0xfd0ef3f3,0xbf6dd2d2,
             0x814ccdcd,0x18140c0c,0x26351313,0xc32fecec,0xbee15f5f,0x35a29797,0x88cc4444,0x2e391717,
             0x9357c4c4,0x55f2a7a7,0xfc827e7e,0x7a473d3d,0xc8ac6464,0xbae75d5d,0x322b1919,0xe6957373,
             0xc0a06060,0x19988181,0x9ed14f4f,0xa37fdcdc,0x44662222,0x547e2a2a,0x3bab9090, 0xb838888,
             0x8cca4646,0xc729eeee,0x6bd3b8b8,0x283c1414,0xa779dede,0xbce25e5e,0x161d0b0b,0xad76dbdb,
             0xdb3be0e0,0x64563232,0x744e3a3a,0x141e0a0a,0x92db4949, 0xc0a0606,0x486c2424,0xb8e45c5c,
             0x9f5dc2c2,0xbd6ed3d3,0x43efacac,0xc4a66262,0x39a89191,0x31a49595,0xd337e4e4,0xf28b7979,
             0xd532e7e7,0x8b43c8c8,0x6e593737,0xdab76d6d, 0x18c8d8d,0xb164d5d5,0x9cd24e4e,0x49e0a9a9,
             0xd8b46c6c,0xacfa5656,0xf307f4f4,0xcf25eaea,0xcaaf6565,0xf48e7a7a,0x47e9aeae,0x10180808,
             0x6fd5baba,0xf0887878,0x4a6f2525,0x5c722e2e,0x38241c1c,0x57f1a6a6,0x73c7b4b4,0x9751c6c6,
             0xcb23e8e8,0xa17cdddd,0xe89c7474,0x3e211f1f,0x96dd4b4b,0x61dcbdbd, 0xd868b8b, 0xf858a8a,
             0xe0907070,0x7c423e3e,0x71c4b5b5,0xccaa6666,0x90d84848, 0x6050303,0xf701f6f6,0x1c120e0e,
             0xc2a36161,0x6a5f3535,0xaef95757,0x69d0b9b9,0x17918686,0x9958c1c1,0x3a271d1d,0x27b99e9e,
             0xd938e1e1,0xeb13f8f8,0x2bb39898,0x22331111,0xd2bb6969,0xa970d9d9, 0x7898e8e,0x33a79494,
             0x2db69b9b,0x3c221e1e,0x15928787,0xc920e9e9,0x8749cece,0xaaff5555,0x50782828,0xa57adfdf,
              0x38f8c8c,0x59f8a1a1, 0x9808989,0x1a170d0d,0x65dabfbf,0xd731e6e6,0x84c64242,0xd0b86868,
             0x82c34141,0x29b09999,0x5a772d2d,0x1e110f0f,0x7bcbb0b0,0xa8fc5454,0x6dd6bbbb,0x2c3a1616};
        #endregion
    }
}

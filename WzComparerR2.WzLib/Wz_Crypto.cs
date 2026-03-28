using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using AES = System.Security.Cryptography.Aes;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib.Utilities;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

using static WzComparerR2.WzLib.Utilities.MathHelper;

namespace WzComparerR2.WzLib
{
    public enum Wz_CryptoKeyType
    {
        Unknown = 0,
        BMS = 1,
        KMS = 2,
        GMS = 3,
        KMST1198 = 4,
        KMST1199 = 5,
    }

    public class Wz_Crypto
    {
        public Wz_Crypto()
        {
            this.keys_bms = Wz_NonOpCryptoKey.Instance;
            this.keys_kms = new Wz_CryptoKey(iv_kms);
            this.keys_gms = new Wz_CryptoKey(iv_gms);
            this.keys_kmst1198 = new Pkg2DirStringKey(0xDEADBEEF);
            this.UseListWz = false;
            this.Pkg1EncType = Wz_CryptoKeyType.Unknown;
            this.List = new StringCollection();
        }

        public void Reset()
        {
            this.UseListWz = false;
            this.Pkg1EncType = Wz_CryptoKeyType.Unknown;
            this.List.Clear();
            this.KnownProfiles.Clear();
        }

        // Known version cache: populated after successful detection, used as fast path for subsequent files.
        public List<KnownProfileEntry> KnownProfiles { get; } = new();

        public bool ListContains(string name)
        {
            bool contains = this.List.Contains(name);
            if (contains)
                this.List.Remove(name);
            return contains;
        }

        public void LoadListWz(string path)
        {
            path = Path.Combine(path, "List.wz");
            if (File.Exists(path))
            {
                this.UseListWz = true;
                using (FileStream list_file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BinaryReader listwz = new BinaryReader(list_file);
                    int length = (int)list_file.Length;
                    int len = 0;
                    byte b = 0;
                    string folder = "";
                    list_file.Position += 4;
                    byte check_for_d = listwz.ReadByte();

                    if ((char)(check_for_d ^ this.keys_gms[0]) == 'd')
                    {
                        this.Pkg1EncType = Wz_CryptoKeyType.GMS;
                    }
                    else if ((char)(check_for_d ^ this.keys_kms[0]) == 'd')
                    {
                        this.Pkg1EncType = Wz_CryptoKeyType.KMS;
                    }

                    list_file.Position = 0;
                    while (list_file.Position < length)
                    {
                        len = listwz.ReadInt32() * 2;
                        for (int i = 0; i < len; i += 2)
                        {
                            b = (byte)(listwz.ReadByte() ^ this.Pkg1Keys[i]);
                            folder += (char)(b);
                            list_file.Position++;
                        }
                        list_file.Position += 2;
                        folder.Replace(".im/", ".img");
                        this.List.Add(folder);
                        folder = "";
                    }
                    this.List.Remove("dummy");
                }
            }
        }

        static readonly byte[] iv_gms = { 0x4d, 0x23, 0xc7, 0x2b };
        static readonly byte[] iv_kms = { 0xb9, 0x7d, 0x63, 0xe9 };

        private IWzDecrypter keys_bms;
        private Wz_CryptoKey keys_gms, keys_kms;
        private IWzDecrypter keys_kmst1198;

        public bool UseListWz { get; private set; }
        public StringCollection List { get; private set; }

        public Wz_CryptoKeyType Pkg1EncType { get; set; }
        public Wz_CryptoKeyType Pkg2EncType { get; set; }
        public bool Pkg1DirEncDetected => this.Pkg1EncType != Wz_CryptoKeyType.Unknown;
        public bool Pkg2DirEncDetected => this.Pkg2EncType != Wz_CryptoKeyType.Unknown;
        public IWzDecrypter Pkg1Keys => this.GetKeys(this.Pkg1EncType);
        public IWzDecrypter Pkg2Keys => this.GetKeys(this.Pkg2EncType);

        public bool IsDirEncDetected(Wz_File wzFile)
        {
            if (wzFile.Header.IsPkg1) return this.Pkg1DirEncDetected;
            if (wzFile.Header.IsPkg2) return this.Pkg2DirEncDetected;
            throw new Exception($"Unknown wzfile signature: {wzFile.Header.Signature}");
        }

        public IWzDecrypter GetKeys(Wz_CryptoKeyType keyType)
        {
            switch (keyType)
            {
                case Wz_CryptoKeyType.Unknown: return null;
                case Wz_CryptoKeyType.BMS: return this.keys_bms;
                case Wz_CryptoKeyType.KMS: return this.keys_kms;
                case Wz_CryptoKeyType.GMS: return this.keys_gms;
                case Wz_CryptoKeyType.KMST1198 : return this.keys_kmst1198;
                case Wz_CryptoKeyType.KMST1199 : throw new NotSupportedException($"KMST1199 PKG2 directory encryption is not supported.");
                default: throw new ArgumentOutOfRangeException(nameof(keyType));
            }
        }

        public class Wz_CryptoKey : IWzDecrypter
        {
            public Wz_CryptoKey(byte[] iv)
            {
                this.iv = iv;
            }

            private byte[] keys;
            private byte[] iv;

            public byte this[int index]
            {
                get
                {
                    if (keys == null || keys.Length <= index)
                    {
                        EnsureKeySize(index + 1);
                    }
                    return this.keys[index];
                }
            }

            public void EnsureKeySize(int size)
            {
                if (this.keys != null && this.keys.Length >= size)
                {
                    return;
                }

                size = (size + 63) & ~63;
                int startIndex = 0;

                if (this.keys == null)
                {
                    keys = new byte[size];
                }
                else
                {
                    startIndex = this.keys.Length;
                    Array.Resize(ref this.keys, size);
                }

                using var aes = AES.Create();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = aesKey;
                aes.Mode = CipherMode.ECB;
                MemoryStream ms = new MemoryStream(keys, startIndex, keys.Length - startIndex, true);
                CryptoStream s = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);

                for (int i = startIndex; i < size; i += 16)
                {
                    if (i == 0)
                    {
                        byte[] block = new byte[16];
                        for (int j = 0; j < block.Length; j++)
                        {
                            block[j] = iv[j % 4];
                        }
                        s.Write(block, 0, block.Length);
                    }
                    else
                    {
                        s.Write(keys, i - 16, 16);
                    }
                }

                s.Flush();
                ms.Close();
            }

            public void Decrypt(Span<byte> data)
            {
                this.Decrypt(data, 0);
            }

            public void Decrypt(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
            {
                this.Decrypt(inputBuffer, outputBuffer, 0);
            }

            public void Decrypt(Span<byte> data, int keyOffset)
            {
                this.Decrypt((ReadOnlySpan<byte>)data, data, keyOffset);
            }

            public unsafe void Decrypt(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer, int keyOffset)
            {
                if (inputBuffer.Length != outputBuffer.Length)
                {
                    throw new ArgumentException("Input and output buffer lengths must match.");
                }

                this.EnsureKeySize(keyOffset + inputBuffer.Length);
                ReadOnlySpan<byte> keys = this.keys.AsSpan(keyOffset, inputBuffer.Length);

#if NET6_0_OR_GREATER
                if (Avx2.IsSupported && inputBuffer.Length >= 32)
                {
                    Vector256<byte> ymm0, ymm1;
                    while (inputBuffer.Length >= 32)
                    {
                        fixed (byte* pInput = inputBuffer, pOutput = outputBuffer, pKeys = keys)
                        {
                            ymm0 = Avx.LoadVector256(pInput);
                            ymm1 = Avx.LoadVector256(pKeys);
                            Avx.Store(pOutput, Avx2.Xor(ymm0, ymm1));
                        }
                        inputBuffer = inputBuffer.Slice(32);
                        outputBuffer = outputBuffer.Slice(32);
                        keys = keys.Slice(32);
                    }
                }

                if (Sse2.IsSupported && inputBuffer.Length >= 16)
                {
                    Vector128<byte> xmm0, xmm1;
                    while (inputBuffer.Length >= 16)
                    {
                        fixed (byte* pInput = inputBuffer, pOutput = outputBuffer, pKeys = keys)
                        {
                            xmm0 = Sse2.LoadVector128(pInput);
                            xmm1 = Sse2.LoadVector128(pKeys);
                            Sse2.Store(pOutput, Sse2.Xor(xmm0, xmm1));
                        }
                        inputBuffer = inputBuffer.Slice(16);
                        outputBuffer = outputBuffer.Slice(16);
                        keys = keys.Slice(16);
                    }
                }
#endif
                while (inputBuffer.Length >= 4)
                {
                    MemoryMarshal.Cast<byte, int>(outputBuffer)[0] =
                        MemoryMarshal.Cast<byte, int>(inputBuffer)[0] ^
                        MemoryMarshal.Cast<byte, int>(keys)[0];
                    inputBuffer = inputBuffer.Slice(4);
                    outputBuffer = outputBuffer.Slice(4);
                    keys = keys.Slice(4);
                }

                for (int i = 0; i < inputBuffer.Length; i++)
                {
                    outputBuffer[i] = (byte)(inputBuffer[i] ^ keys[i]);
                }
            }

            static readonly byte[] aesKey = {0x13, 0x00, 0x00, 0x00,
                                        0x08, 0x00, 0x00, 0x00,
                                        0x06, 0x00, 0x00, 0x00,
                                        0xB4, 0x00, 0x00, 0x00,
                                        0x1B, 0x00, 0x00, 0x00,
                                        0x0F, 0x00, 0x00, 0x00,
                                        0x33, 0x00, 0x00, 0x00,
                                        0x52, 0x00, 0x00, 0x00 };
        }

        public sealed class Wz_NonOpCryptoKey : IWzDecrypter
        {
            public static readonly IWzDecrypter Instance = new Wz_NonOpCryptoKey();

            public Wz_NonOpCryptoKey()
            {
            }

            public byte this[int index] => 0;

            public void Decrypt(Span<byte> data)
            {
            }

            public void Decrypt(Span<byte> data, int keyOffset)
            {
            }

            public void Decrypt(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
            {
                this.Decrypt(inputBuffer, outputBuffer, 0);
            }

            public void Decrypt(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer, int keyOffset)
            {
                if (inputBuffer.Length != outputBuffer.Length)
                {
                    throw new ArgumentException("Input and output buffer lengths must match.");
                }

                inputBuffer.CopyTo(outputBuffer);
            }
        }

        public class Pkg2DirStringKey : IWzDecrypter
        {
            public Pkg2DirStringKey(uint baseKey)
            {
                this.baseKey = baseKey;
            }

            private uint baseKey;
            private byte[] keys;

            public byte this[int index]
            {
                get
                {
                    this.CreateKeyIfNotExist();
                    return this.keys[index % this.keys.Length];
                }
            }

            private void CreateKeyIfNotExist()
            {
                if (this.keys != null) 
                { 
                    return; 
                }
                byte[] keys = new byte[8];
                Span<ushort> u16Keys = MemoryMarshal.Cast<byte, ushort>(keys.AsSpan());
                for(int i = 0; i < 4; i++)
                {
                    u16Keys[i] = (ushort)(this.baseKey >> (8 * i));
                }
                this.keys = keys;
            }

            public void Decrypt(Span<byte> data)
            {
                this.Decrypt(data, 0);
            }

            public void Decrypt(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
            {
                this.Decrypt(inputBuffer, outputBuffer, 0);
            }

            public void Decrypt(Span<byte> data, int keyOffset)
            {
                this.Decrypt((ReadOnlySpan<byte>)data, data, keyOffset);
            }

            public unsafe void Decrypt(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer, int keyOffset)
            {
                if (inputBuffer.Length != outputBuffer.Length)
                {
                    throw new ArgumentException("Input and output buffer lengths must match.");
                }
                if ((inputBuffer.Length & 1) != 0)
                {
                    throw new ArgumentException("Data length must be a multiple of 2.", nameof(inputBuffer));
                }
                if ((keyOffset & 7) != 0)
                {
                    throw new ArgumentException("KeyOffset must be a multiple of 8.", nameof(keyOffset));
                }

                this.CreateKeyIfNotExist();

                long keyVal = MemoryMarshal.Cast<byte, long>(this.keys)[0];
                while (inputBuffer.Length >= 8)
                {
                    MemoryMarshal.Cast<byte, long>(outputBuffer)[0] =
                        MemoryMarshal.Cast<byte, long>(inputBuffer)[0] ^ keyVal;
                    inputBuffer = inputBuffer.Slice(8);
                    outputBuffer = outputBuffer.Slice(8);
                }
                for (int i = 0; i < inputBuffer.Length; i++)
                {
                    outputBuffer[i] = (byte)(inputBuffer[i] ^ keys[i % keys.Length]);
                }
            }
        }

        // KMST1199
        public class Pkg2DirStringKeyV2 : Pkg2DirStringKey, IWzDecrypter
        {
            public Pkg2DirStringKeyV2(uint hash1, uint hashVersion) : base(ConvertKey(hash1, hashVersion))
            {
            }

            private static uint ConvertKey(uint hash1, uint hashVersion)
            {
                uint baseHash = hash1 ^ hashVersion ^ 0x6D4C3B2A;
                return Mix(Mix(baseHash) ^ 0x4F4CB34A);
            }
        }

        public class KnownProfileEntry
        {
            public KnownProfileEntry(string profileName, int wzVersion, uint hashVersion)
            {
                this.ProfileName = profileName;
                this.WzVersion = wzVersion;
                this.HashVersion = hashVersion;
            }

            public string ProfileName { get; }
            public int WzVersion { get; }
            public uint HashVersion { get; }
        }
    }
}

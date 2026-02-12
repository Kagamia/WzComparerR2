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

namespace WzComparerR2.WzLib
{
    public enum Wz_CryptoKeyType
    {
        Unknown = 0,
        BMS = 1,
        KMS = 2,
        GMS = 3,
        KMST1198 = 4,
    }

    public class Wz_Crypto
    {
        public Wz_Crypto()
        {
            this.keys_bms = Wz_NonOpCryptoKey.Instance;
            this.keys_kms = new Wz_CryptoKey(iv_kms);
            this.keys_gms = new Wz_CryptoKey(iv_gms);
            this.keys_kmst1198 = Pkg2DirStringKey.Instance;
            this.UseListWz = false;
            this.Pkg1EncType = Wz_CryptoKeyType.Unknown;
            this.List = new StringCollection();
        }

        public void Reset()
        {
            this.UseListWz = false;
            this.Pkg1EncType = Wz_CryptoKeyType.Unknown;
            this.List.Clear();
        }

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

        public void DetectEncryption(Wz_File wzFile)
        {
            Wz_CryptoKeyType encType = Wz_CryptoKeyType.Unknown;
            long old_off = wzFile.FileStream.Position;

            wzFile.FileStream.Position = wzFile.Header.DataStartPosition;
            var br = new WzBinaryReader(wzFile.FileStream, false);
            int nodeCount = br.ReadCompressedInt32();
            if (wzFile.Header.Signature == Wz_Header.PKG2)
            {
                var filePos = br.BaseStream.Position;
                byte nextByte = br.ReadByte();
                if (nextByte != 0x04 && nextByte != 0x03)
                {
                    br.BaseStream.Position = filePos;
                    int offsetCount = br.ReadCompressedInt32();
                    if (offsetCount == nodeCount)
                    {
                        // no dir entry
                        goto lbl_end;
                    }
                    else
                    {
                        // unknown file format
                        goto lbl_end;
                    }
                }
                br.BaseStream.Position = filePos;
                nodeCount = 1; // at least one node
            }
            if (nodeCount <= 0) // no dir, skip
            {
                goto lbl_end;
            }
            wzFile.FileStream.Position++; // skip node type
            int len = (int)(-br.ReadSByte()); // always cp1252
            long stringStartPos = br.BaseStream.Position;

            // try pkg1 style string
            byte[] bytes = br.ReadBytes(len);
            for (int i = 0; i < len; i++)
            {
                bytes[i] ^= (byte)(0xAA + i);
            }
            Span<char> charBuf = stackalloc char[len];

            // try BMS
            for (int i = 0; i < len; i++)
            {
                charBuf[i] = (char)bytes[i];
            }
            if (IsLegalNodeName(charBuf))
            {
                encType = Wz_CryptoKeyType.BMS;
                goto lbl_end;
            }

            // try KMS
            for (int i = 0; i < len; i++)
            {
                charBuf[i] = (char)(keys_kms[i] ^ bytes[i]);
            }
            if (IsLegalNodeName(charBuf))
            {
                encType = Wz_CryptoKeyType.KMS;
                goto lbl_end;
            }

            // try GMS
            for (int i = 0; i < len; i++)
            {
                charBuf[i] = (char)(keys_gms[i] ^ bytes[i]);
            }
            if (IsLegalNodeName(charBuf))
            {
                encType = Wz_CryptoKeyType.GMS;
                goto lbl_end;
            }

            // try KMST1198
            if (wzFile.Header.Signature == Wz_Header.PKG2)
            {
                br.BaseStream.Position = stringStartPos;
                bytes = br.ReadBytes(len * 2);
                this.keys_kmst1198.Decrypt(bytes.AsSpan());
                var charBuffer = MemoryMarshal.Cast<byte, char>(bytes.AsSpan());
                if (IsLegalNodeName(charBuffer))
                {
                    encType = Wz_CryptoKeyType.KMST1198;
                    goto lbl_end;
                }
            }

        lbl_end:
            wzFile.FileStream.Position = old_off;
            this.SetEncType(wzFile, encType);
        }

        private void SetEncType(Wz_File wzFile, Wz_CryptoKeyType encType)
        {
            string wzFileSig = wzFile.Header.Signature;
            if (wzFileSig == Wz_Header.PKG1)
            {
                this.Pkg1EncType = encType;
            }
            else if (wzFileSig == Wz_Header.PKG2)
            {
                this.Pkg2EncType = encType;
            }
            else
            {
                throw new Exception($"Unknown wzfile signature: {wzFileSig}");
            }
        }

        private bool IsLegalNodeName(ReadOnlySpan<char> nodeName)
        {
            if (nodeName.Length == 0)
            {
                return false;
            }

            if (nodeName.EndsWith(".img".AsSpan()) || nodeName.EndsWith(".lua".AsSpan()))
            {
                return true;
            }

            // MSEA 225 has a node in Base.wz named "Base,Character,Effect,Etc,Item,Map,Mob,Morph,Npc,Quest,Reactor,Skill,Sound,String,TamingMob,UI"
            // It is so funny but wzlib have to be compatible with it.
            // 2025-06-04: MSEA 242 has a new node named "Base Character Effect Etc Item Map Mob Morph Npc Quest Reactor Skill Sound String TamingMob UI"
            // so we only verify if the nodeName is a valid ascii string.
            foreach (var c in nodeName)
            {
                if (!(0x20 <= c && c <= 0x7f))
                {
                    return false;
                }
            }
            return true;
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
            string wzFileSig = wzFile.Header.Signature;
            return wzFileSig switch
            {
                Wz_Header.PKG1 => this.Pkg1DirEncDetected,
                Wz_Header.PKG2 => this.Pkg2DirEncDetected,
                _ => throw new Exception($"Unknown wzfile signature: {wzFileSig}")
            };
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

            public void Decrypt(byte[] buffer, int startIndex, int length)
            {
                this.Decrypt(buffer, startIndex, length, 0);
            }

            public void Decrypt(byte[] buffer, int startIndex, int length, int keyOffset)
            {
                this.Decrypt(buffer.AsSpan(startIndex, length), keyOffset);
            }

            public void Decrypt(Span<byte> data)
            {
                this.Decrypt(data, 0);
            }

            public unsafe void Decrypt(Span<byte> data, int keyOffset)
            {
                this.EnsureKeySize(keyOffset + data.Length);
                ReadOnlySpan<byte> keys = this.keys.AsSpan(keyOffset, data.Length);

#if NET6_0_OR_GREATER
                if (Avx2.IsSupported && data.Length >= 32)
                {
                    Vector256<byte> ymm0, ymm1;
                    while (data.Length >= 32)
                    {
                        fixed (byte* pData = data, pKeys = keys)
                        {
                            ymm0 = Avx.LoadVector256(pData);
                            ymm1 = Avx.LoadVector256(pKeys);
                            Avx.Store(pData, Avx2.Xor(ymm0, ymm1));
                        }
                        data = data.Slice(32);
                        keys = keys.Slice(32);
                    }
                }

                if (Sse2.IsSupported && data.Length >= 16)
                {
                    Vector128<byte> xmm0, xmm1;
                    while (data.Length >= 16)
                    {
                        fixed (byte* pData = data, pKeys = keys)
                        {
                            xmm0 = Sse2.LoadVector128(pData);
                            xmm1 = Sse2.LoadVector128(pKeys);
                            Sse2.Store(pData, Sse2.Xor(xmm0, xmm1));
                        }
                        data = data.Slice(16);
                        keys = keys.Slice(16);
                    }
                }
#endif
                while (data.Length >= 4)
                {
                    fixed (byte* pData = data, pKeys = keys)
                    {
                        *((int*)pData) ^= *(int*)(pKeys);
                    }
                    data = data.Slice(4);
                    keys = keys.Slice(4);
                }

                for (int i = 0; i < data.Length; i++)
                {
                    data[i] ^= keys[i];
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

            public void Decrypt(byte[] buffer, int startIndex, int length)
            {
            }

            public void Decrypt(byte[] buffer, int startIndex, int length, int keyOffset)
            {
            }

            public void Decrypt(Span<byte> data)
            {
            }

            public void Decrypt(Span<byte> data, int keyOffset)
            {
            }
        }

        public sealed class Pkg2DirStringKey : IWzDecrypter
        {
            public static readonly IWzDecrypter Instance = new Pkg2DirStringKey();

            public Pkg2DirStringKey()
            {
            }

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
                    u16Keys[i] = (ushort)(0xDEADBEEF >> (8 * i));
                }
                this.keys = keys;
            }

            public void Decrypt(byte[] buffer, int startIndex, int length)
            {
                this.Decrypt(buffer, startIndex, length, 0);
            }

            public void Decrypt(byte[] buffer, int startIndex, int length, int keyOffset)
            {
                this.Decrypt(buffer.AsSpan(startIndex, length), keyOffset);
            }

            public void Decrypt(Span<byte> data)
            {
                this.Decrypt(data, 0);
            }

            public unsafe void Decrypt(Span<byte> data, int keyOffset)
            {
                if ((data.Length & 1) != 0)
                {
                    throw new ArgumentException("Data length must be a multiple of 2.", nameof(data));
                }
                if ((keyOffset & 7) != 0)
                {
                    throw new ArgumentException("KeyOffset must be a multiple of 8.", nameof(keyOffset));
                }

                this.CreateKeyIfNotExist();

                while (data.Length >= 8)
                {
                    fixed (byte* pData = data, pKeys = this.keys)
                    {
                        *((long*)pData) ^= *(long*)(pKeys);
                    }
                    data = data.Slice(8);
                }
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] ^= keys[i % keys.Length];
                }
            }
        }
    }
}

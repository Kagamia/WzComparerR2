using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace WzComparerR2.WzLib
{
    public enum Wz_CryptoKeyType
    {
        Unknown = 0,
        BMS = 1,
        KMS = 2,
        GMS = 3
    }

    public class Wz_Crypto
    {
        public Wz_Crypto()
        {
            this.keys_bms = new Wz_CryptoKey(iv_bms);
            this.keys_kms = new Wz_CryptoKey(iv_kms);
            this.keys_gms = new Wz_CryptoKey(iv_gms);
            this.listwz = false;
            this.EncType = Wz_CryptoKeyType.Unknown;
            this.List = new StringCollection();
        }

        public void Reset()
        {
            this.encryption_detected = false;
            this.listwz = false;
            this.EncType = Wz_CryptoKeyType.Unknown;
            this.List.Clear();
        }

        public bool list_contains(string name)
        {
            bool contains = this.List.Contains(name);
            if (contains)
                this.List.Remove(name);
            return contains;
            //    foreach (string list_entry in this.list)
            //    {
            //        // if (list_entry.Contains(Name))
            //        if (list_entry == Name)
            //        {
            //            this.list.Remove(list_entry);
            //            return true;
            //        }
            //    }
            //    return false;
        }

        public void LoadListWz(string path)
        {
            path = Path.Combine(path, "List.wz");
            if (File.Exists(path))
            {
                this.listwz = true;
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
                        this.EncType = Wz_CryptoKeyType.GMS;
                    }
                    else if ((char)(check_for_d ^ this.keys_kms[0]) == 'd')
                    {
                        this.EncType = Wz_CryptoKeyType.KMS;
                    }

                    list_file.Position = 0;
                    while (list_file.Position < length)
                    {
                        len = listwz.ReadInt32() * 2;
                        for (int i = 0; i < len; i += 2)
                        {
                            b = (byte)(listwz.ReadByte() ^ this.keys[i]);
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

        public void DetectEncryption(Wz_File f)
        {
            int old_off = (int)f.FileStream.Position;
            f.FileStream.Position = f.Header.DataStartPosition;
            if (f.ReadInt32() <= 0) //只有文件头 无法预判
            {
                return;
            }
            f.FileStream.Position++;
            int len = (int)(-f.BReader.ReadSByte());
            byte[] bytes = f.BReader.ReadBytes(len);

            for (int i = 0; i < len; i++)
            {
                bytes[i] ^= (byte)(0xAA + i);
            }

            StringBuilder sb = new StringBuilder();
            if (!this.encryption_detected)
            {
                //测试bms
                sb.Clear();
                for (int i = 0; i < len; i++)
                {
                    sb.Append((char)(keys_bms[i] ^ bytes[i]));
                }
                if (IsLegalNodeName(sb.ToString()))
                {
                    this.EncType = Wz_CryptoKeyType.BMS;
                    this.encryption_detected = true;
                    goto lbl_end;
                }

                //测试kms
                sb.Clear();
                for (int i = 0; i < len; i++)
                {
                    sb.Append((char)(keys_kms[i] ^ bytes[i]));
                }
                if (IsLegalNodeName(sb.ToString()))
                {
                    this.EncType = Wz_CryptoKeyType.KMS;
                    this.encryption_detected = true;
                    goto lbl_end;
                }

                //测试gms
                sb.Clear();
                for (int i = 0; i < len; i++)
                {
                    sb.Append((char)(keys_gms[i] ^ bytes[i]));
                }
                if (IsLegalNodeName(sb.ToString()))
                {
                    this.EncType = Wz_CryptoKeyType.GMS;
                    this.encryption_detected = true;
                    goto lbl_end;
                }
            }

        lbl_end:
            f.FileStream.Position = old_off;
        }

        private bool IsLegalNodeName(string nodeName)
        {
            // MSEA 225 has a node in Base.wz named "Base,Character,Effect,Etc,Item,Map,Mob,Morph,Npc,Quest,Reactor,Skill,Sound,String,TamingMob,UI"
            // It is so funny but wzlib have to be compatible with it.
            return nodeName.EndsWith(".img") || nodeName.EndsWith(".lua") || Regex.IsMatch(nodeName, @"^[A-Za-z0-9_,]+$");
        }

        static readonly byte[] iv_gms = { 0x4d, 0x23, 0xc7, 0x2b };
        static readonly byte[] iv_kms = { 0xb9, 0x7d, 0x63, 0xe9 };
        static readonly byte[] iv_bms = { 0x00, 0x00, 0x00, 0x00 };

        private Wz_CryptoKey keys_bms, keys_gms, keys_kms;
        private Wz_CryptoKeyType enc_type;

        public bool encryption_detected = false;
        public bool listwz = false;

        public Wz_CryptoKey keys { get; private set; }
        public StringCollection List { get; private set; }

        public Wz_CryptoKeyType EncType
        {
            get { return enc_type; }
            set
            {
                this.keys = this.GetKeys(value);
                enc_type = value;
            }
        }

        public Wz_CryptoKey GetKeys(Wz_CryptoKeyType keyType)
        {
            switch (keyType)
            {
                case Wz_CryptoKeyType.Unknown: return null;
                case Wz_CryptoKeyType.BMS: return this.keys_bms;
                case Wz_CryptoKeyType.KMS: return this.keys_kms;
                case Wz_CryptoKeyType.GMS: return this.keys_gms;
                default: throw new ArgumentOutOfRangeException(nameof(keyType));
            }
        }

        public class Wz_CryptoKey
        {
            public Wz_CryptoKey(byte[] iv)
            {
                this.iv = iv;
                if (iv == null || BitConverter.ToInt32(iv, 0) == 0)
                {
                    this.isEmptyIV = true;
                }
            }

            private byte[] keys;
            private byte[] iv;
            private bool isEmptyIV;

            public byte this[int index]
            {
                get
                {
                    if (isEmptyIV)
                    {
                        return 0;
                    }
                    if (keys == null || keys.Length <= index)
                    {
                        EnsureKeySize(index + 1);
                    }
                    return this.keys[index];
                }
            }

            public void EnsureKeySize(int size)
            {
                if (isEmptyIV)
                {
                    return;
                }
                if (this.keys != null && this.keys.Length >= size)
                {
                    return;
                }

                size = (int)Math.Ceiling(1.0 * size / 64) * 64;
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

                var aes = Aes.Create();
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

            public unsafe void Decrypt(byte[] buffer, int startIndex, int length)
            {
                if (isEmptyIV)
                    return;

                this.EnsureKeySize(length);

                fixed (byte* pBuffer = buffer, pKeys = this.keys)
                {
                    int i = 0;
                    byte* pData = pBuffer + startIndex;

                    for (int i1 = length / 4 * 4; i < i1; i += 4, pData += 4)
                    {
                        *((int*)pData) ^= *(int*)(pKeys + i);
                    }

                    for (; i < length; i++, pData++)
                    {
                        *pData ^= *(pKeys + i);
                    }
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
    }
}

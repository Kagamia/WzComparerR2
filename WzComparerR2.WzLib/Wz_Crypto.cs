using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace WzComparerR2.WzLib
{
    public class Wz_Crypto
    {
        public Wz_Crypto()
        {
            this.crypto = Rijndael.Create();
            this.crypto.KeySize = 256;
            this.crypto.Key = this.key;
            this.crypto.Mode = CipherMode.ECB;
            this.memStream = new MemoryStream();
            this.cryptoStream = new CryptoStream(memStream, this.crypto.CreateEncryptor(), CryptoStreamMode.Write);
            this.keys_kms = this.getKeys(this.iv_kms);
            this.cryptoStream.Dispose();
            this.memStream.Dispose();
            this.memStream = new MemoryStream();
            this.cryptoStream = new CryptoStream(memStream, this.crypto.CreateEncryptor(), CryptoStreamMode.Write);
            this.keys_gms = this.getKeys(this.iv_gms);
            this.cryptoStream.Dispose();
            this.memStream.Dispose();
            this.listwz = false;
            this.enc_type = enc_unknown;
            this.list = new Dictionary<string, bool>();
        }

        private byte[] getKeys(byte[] iv)
        {
            byte[] retKey = new byte[1024*1024];
            byte[] input = multiplyBytes(iv, 4, 4);
            int retlen = retKey.Length;

            for (int i = 0; i < (retlen / 16); i++)
            {
                cryptoStream.Write(input, 0, 16);
                input = memStream.ToArray();
                Array.Copy(memStream.ToArray(), 0, retKey, (i * 16), 16);
                memStream.Position = 0;
            }
            cryptoStream.Write(input, 0, 16);
            Array.Copy(memStream.ToArray(), 0, retKey, (retlen - 15), 15);
            return retKey;
        }

        private byte[] multiplyBytes(byte[] iv, int count, int mul)
        {
            int count_mul = count * mul;
            byte[] ret = new byte[count_mul];
            for (int i = 0; i < count_mul; i++)
            {
                ret[i] = iv[i % count];
            }
            return ret;
        }

        public void Reset()
        {
            this.encryption_detected = false;
            this.all_strings_encrypted = false;
            this.listwz = false;
            this.enc_type = Wz_Crypto.enc_unknown;
            this.keys = null;
            this.list.Clear();
        }

        public bool list_contains(string name)
        {
            bool contains = this.list.ContainsKey(name);
            if (contains)
                this.list.Remove(name);
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
            path += "\\List.wz";
            if (File.Exists(path))
            {
                this.listwz = true;
                FileStream list_file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                BinaryReader listwz = new BinaryReader(list_file);
                int length = (int)list_file.Length;
                int len = 0;
                byte b = 0;
                string folder = "";
                list_file.Position += 4;
                byte check_for_d = listwz.ReadByte();

                if ((char)(check_for_d ^ this.keys_gms[0]) == 'd')
                {
                    this.enc_type = Wz_Crypto.enc_GMS;
                    this.keys = this.keys_gms;
                }
                else if ((char)(check_for_d ^ this.keys_kms[0]) == 'd')
                {
                    this.enc_type = Wz_Crypto.enc_KMS;
                    this.keys = this.keys_kms;
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
                    this.list.Add(folder, true);
                    folder = "";
                }
                this.list.Remove("dummy");
            }
        }

        public void DetectEncryption(Wz_File f)
        {
            int old_off = (int)f.FileStream.Position;
            f.FileStream.Position = 62;
            if (f.ReadInt32() <= 0) //只有文件头 无法预判
            {
                return;
            }
            f.FileStream.Position++;
            int len = (int)(-f.BReader.ReadSByte());
            byte[] bytes = f.BReader.ReadBytes(len);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < len; i++)
            {
                bytes[i] ^= (byte)(0xAA + i);
                sb.Append((char)bytes[i]);
            }

            if (sb.ToString().Contains(".img") || sb.ToString().Contains("Cash"))
            {
                this.all_strings_encrypted = false;
            }
            else
            {
                sb.Remove(0, sb.Length);
                if (this.enc_type != Wz_Crypto.enc_unknown)
                {
                    for (int i = 0; i < len; i++)
                    {
                        bytes[i] ^= this.keys[i];
                        sb.Append((char)bytes[i]);
                    }
                    if (sb.ToString().Contains(".img") || sb.ToString().Contains("Cash"))
                    {
                        this.all_strings_encrypted = true;
                        this.encryption_detected = true;
                    }
                }
                else
                {
                    for (int i = 0; i < len; i++) { sb.Append((char)(bytes[i] ^ keys_kms[i])); }
                    if (sb.ToString().Contains(".img") || sb.ToString().Contains("Cash"))
                    {
                        this.enc_type = Wz_Crypto.enc_KMS;
                        this.keys = this.keys_kms;
                        this.all_strings_encrypted = true;
                        this.encryption_detected = true;
                    }
                    else
                    {
                        sb.Remove(0, sb.Length);
                        for (int i = 0; i < len; i++) { sb.Append((char)(bytes[i] ^ keys_gms[i])); }
                        if (sb.ToString().Contains(".img") || sb.ToString().Contains("Cash"))
                        {
                            this.enc_type = Wz_Crypto.enc_GMS;
                            this.keys = this.keys_gms;
                            this.all_strings_encrypted = true;
                            this.encryption_detected = true;
                        }
                    }
                }
            }

            f.FileStream.Position = old_off;
        }

        public const byte enc_unknown = 0;
        public const byte enc_KMS = 2;
        public const byte enc_GMS = 3;

        readonly byte[] iv_gms = { 0x4d, 0x23, 0xc7, 0x2b };
        readonly byte[] iv_kms = { 0xb9, 0x7d, 0x63, 0xe9 };
        readonly byte[] key = {	0x13, 0x00, 0x00, 0x00,
										0x08, 0x00, 0x00, 0x00,
										0x06, 0x00, 0x00, 0x00,
										0xB4, 0x00, 0x00, 0x00,
										0x1B, 0x00, 0x00, 0x00,
										0x0F, 0x00, 0x00, 0x00,
										0x33, 0x00, 0x00, 0x00,
										0x52, 0x00, 0x00, 0x00 };

        internal byte[] keys_gms, keys_kms;
        Rijndael crypto;
        MemoryStream memStream;
        CryptoStream cryptoStream;

        public bool encryption_detected = false;
        public bool all_strings_encrypted = false;
        public bool listwz = false;
        public byte enc_type = Wz_Crypto.enc_unknown;
        public byte[] keys;
        public Dictionary<string, bool> list;
    }
}

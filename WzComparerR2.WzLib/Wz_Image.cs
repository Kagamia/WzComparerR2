using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace WzComparerR2.WzLib
{
    public class Wz_Image
    {
        public Wz_Image(string name, int size, int cs32, uint hashOff, uint hashPos, Wz_File wz_f)
        {
            this.Name = name;
            this.WzFile = wz_f;
            this.Size = size;
            this.Checksum = cs32;
            this.HashedOffset = hashOff;
            this.HashedOffsetPosition = hashPos;
            this.Node = new Wz_ImageNode(name, this);

            this.extr = false;
            this.chec = false;
            this.checEnc = false;
        }

        private bool extr;
        private bool chec;
        private bool checEnc;
        private Wz_CryptoKeyType encType;

        public string Name { get; set; }
        public Wz_File WzFile { get; set; }
        public int Size { get; set; }
        public int Checksum { get; set; }
        public uint HashedOffset { get; set; }
        public uint HashedOffsetPosition { get; set; }
        public long Offset { get; set; }
        
        public Wz_Node Node { get; private set; }

        public Wz_Node OwnerNode { get; set; }

        public bool IsChecksumChecked
        {
            get { return this.chec; }
            internal set { this.chec = value; }
        }
        public bool IsLuaImage
        {
            get { return this.Name.EndsWith(".lua"); }
        }

        public Wz_Crypto.Wz_CryptoKey EncKeys
        {
            get
            {
                var crypto = this.WzFile.WzStructure.encryption;
                if (this.checEnc && this.encType != default)
                {
                    return crypto.GetKeys(this.encType);
                }
                return crypto.keys;
            }
        }

        public bool TryExtract()
        {
            Exception ex;
            return TryExtract(out ex);
        }

        public bool TryExtract(out Exception e)
        {
            if (!this.extr)
            {
                bool disabledChec = this.WzFile?.WzStructure?.ImgCheckDisabled ?? false;
                if (!disabledChec && !this.chec)
                {
                    if (this.Checksum != CalcCheckSum())
                    {
                        e = new ArgumentException("checksum error");
                        return false;
                    }
                    this.chec = true;
                }
                if (!this.checEnc)
                {
                    if (!this.IsLuaImage)
                    {
                        try
                        {
                            this.TryDetectEnc();
                            if (!this.checEnc)
                            {
                                e = null;
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            e = ex;
                            this.Unextract();
                            return false;
                        }
                    }
                }

                try
                {
                    lock (this.WzFile.ReadLock)
                    {
                        this.WzFile.FileStream.Position = this.Offset;
                        if (!this.IsLuaImage)
                        {
                            ExtractImg(this.Offset, this.Node, 0);
                            this.WzFile.stringTable.Clear();
                        }
                        else
                        {
                            ExtractLua();
                        }
                        this.extr = true;
                    }
                }
                catch (Exception ex)
                {
                    e = ex;
                    this.Unextract();
                    return false;
                }
            }
            e = null;
            return true;
        }

        public void Unextract()
        {
            this.extr = false;
            this.Node.Nodes.Clear();
        }

        public unsafe int CalcCheckSum()
        {
            lock (this.WzFile.ReadLock)
            {
                this.WzFile.FileStream.Position = this.Offset;
                int cs = 0;
                byte[] buffer = new byte[4096];
                int count;
                int size = this.Size;
                while ((count = this.WzFile.FileStream.Read(buffer, 0, Math.Min(size, buffer.Length))) > 0)
                {
                    fixed (byte* pBuffer = buffer)
                    {
                        int* p = (int*)pBuffer;
                        int i, j = count / 4;
                        for (i = 0; i < j; i++)
                        {
                            int data = *(p + i);
                            cs += (data & 0xff) + (data >> 8 & 0xff) + (data >> 16 & 0xff) + (data >> 24 & 0xff);
                        }
                        for (i = i * 4; i < count; i++)
                        {
                            cs += buffer[i];
                        }
                    }

                    size -= count;
                }
                return cs;
            }
        }

        private void ExtractImg(long offset, Wz_Node parent, long eob)
        {
            int entries = 0;
            string tag = this.WzFile.ReadString(offset, this.EncKeys);
            switch (tag)
            {
                case "Property":
                    this.WzFile.FileStream.Position += 2;
                    entries = this.WzFile.ReadInt32();
                    for (int i = 0; i < entries; i++)
                    {
                        ExtractValue(offset, parent);
                    }
                    break;

                case "Shape2D#Vector2D":
                    parent.Value = new Wz_Vector(this.WzFile.ReadInt32(), this.WzFile.ReadInt32());
                    break;

                case "Canvas":
                    this.WzFile.FileStream.Position++;
                    if (this.WzFile.BReader.ReadByte() == 0x01)
                    {
                        this.WzFile.FileStream.Position += 2;
                        entries = this.WzFile.ReadInt32();
                        for (int i = 0; i < entries; i++)
                        {
                            ExtractValue(offset, parent);
                        }
                    }
                    int w = this.WzFile.ReadInt32();
                    int h = this.WzFile.ReadInt32();
                    int form = this.WzFile.ReadInt32() + this.WzFile.BReader.ReadByte();
                    this.WzFile.FileStream.Position += 4;
                    int bufsize = this.WzFile.BReader.ReadInt32();
                    parent.Value = new Wz_Png(w, h, bufsize - 1, form, (uint)this.WzFile.FileStream.Position + 1, this);
                    this.WzFile.FileStream.Position += bufsize;
                    break;

                case "Shape2D#Convex2D":
                    entries = this.WzFile.ReadInt32();
                    for (int i = 0; i < entries; i++)
                    {
                        ExtractImg(offset, parent, 0);
                    }
                    break;

                case "Sound_DX8":
                    this.WzFile.FileStream.Position++;
                    int len = this.WzFile.ReadInt32();
                    int ms = this.WzFile.ReadInt32();
                    int headerLen = (int)(eob - len - this.WzFile.FileStream.Position);
                    byte[] header = this.WzFile.BReader.ReadBytes(headerLen);
                    parent.Value = new Wz_Sound((uint)(eob - len), len, header, ms, this);
                    this.WzFile.FileStream.Position = eob;
                    break;

                case "UOL":
                    this.WzFile.FileStream.Position++;
                    parent.Value = new Wz_Uol(this.WzFile.ReadString(offset, this.EncKeys));
                    break;

                case "RawData": // introduced in GMS v243
                    this.WzFile.FileStream.Position++;
                    int rawDataLen = this.WzFile.ReadInt32();
                    uint rawDataOffset = (uint)this.WzFile.FileStream.Position;
                    parent.Value = new Wz_RawData(rawDataOffset, rawDataLen, this);
                    this.WzFile.FileStream.Position += rawDataLen;
                    break;

                default:
                    throw new Exception("unknown wz tag: " + tag);
            }
        }

        private void TryDetectEnc()
        {
            this.encType = default;
            this.checEnc = false;

            var wzsEncType = this.WzFile.WzStructure.encryption.EncType;
            if (wzsEncType != default)
            {
                if (this.IsIllegalTag(wzsEncType))
                {
                    this.encType = wzsEncType;
                    this.checEnc = true;
                    return;
                }
            }

            foreach (var enc in new[] {
                Wz_CryptoKeyType.BMS,
                Wz_CryptoKeyType.KMS,
                Wz_CryptoKeyType.GMS,
            })
            {
                if (this.IsIllegalTag(enc))
                {
                    this.encType = enc;
                    this.checEnc = true;
                    return;
                }
            }
        }

        private bool IsIllegalTag(Wz_CryptoKeyType keyType)
        {
            this.WzFile.FileStream.Position = this.Offset;
            this.WzFile.stringTable.Remove(Offset);
            switch (this.WzFile.ReadString(Offset, keyType))
            {
                case "Property":
                case "Shape2D#Vector2D":
                case "Canvas":
                case "Shape2D#Convex2D":
                case "Sound_DX8":
                case "UOL":
                    return true;
                default:
                    return false;
            }
        }

        private void ExtractValue(long offset, Wz_Node parent)
        {
            parent = parent.Nodes.Add(this.WzFile.ReadString(offset, this.EncKeys));
            byte flag = this.WzFile.BReader.ReadByte();
            switch (flag)
            {
                case 0x00:
                    parent.Value = null;
                    break;

                case 0x02:
                case 0x0B:
                    parent.Value = this.WzFile.BReader.ReadInt16();
                    break;

                case 0x03:
                case 0x13:
               // case 0x14:
                    parent.Value = this.WzFile.ReadInt32();
                    break;

                case 0x14:
                    parent.Value = this.WzFile.ReadInt64();
                    break;

                case 0x04:
                    parent.Value = this.WzFile.ReadSingle();
                    break;

                case 0x05:
                    parent.Value = this.WzFile.BReader.ReadDouble();
                    break;

                case 0x08:
                    parent.Value = this.WzFile.ReadString(offset, this.EncKeys);
                    break;

                case 0x09:
                    ExtractImg(offset, parent, this.WzFile.BReader.ReadInt32() + this.WzFile.FileStream.Position);
                    break;

                default:
                    throw new Exception("读取值错误." + flag + " at Offset: " + this.WzFile.FileStream.Position);
            }
        }

        private void ExtractLua()
        {
            while(this.WzFile.FileStream.Position < this.Offset + this.Size)
            {
                var flag = this.WzFile.BReader.ReadByte();

                switch (flag)
                {
                    case 0x01:
                        ExtractLuaValue(this.Node);
                        break;

                    default:
                        throw new Exception("读取Lua错误." + flag + " at Offset: " + this.WzFile.FileStream.Position);
                }
            }
        }

        private void ExtractLuaValue(Wz_Node parent)
        {
            int len = this.WzFile.ReadInt32();
            byte[] data = this.WzFile.BReader.ReadBytes(len);
            if (!this.checEnc)
            {
                TryDetectLuaEnc(data);
            }
            this.EncKeys.Decrypt(data, 0, data.Length);
            string luaCode = Encoding.UTF8.GetString(data);
            parent.Value = luaCode;
        }

        private void TryDetectLuaEnc(byte[] luaBinary)
        {
            byte[] tempBuffer = new byte[Math.Min(luaBinary.Length, 64)];
            char[] tempStr = new char[tempBuffer.Length];

            //测试各种加密方式 判断符合度最高的
            int maxCharCount = 0;
            var maxCharEnc = Wz_CryptoKeyType.Unknown;

            foreach (var enc in new[] {
                Wz_CryptoKeyType.BMS,
                Wz_CryptoKeyType.KMS,
                Wz_CryptoKeyType.GMS,
            })
            {
                Buffer.BlockCopy(luaBinary, 0, tempBuffer, 0, tempBuffer.Length);

                this.WzFile.WzStructure.encryption.GetKeys(enc).Decrypt(tempBuffer, 0, tempBuffer.Length);
                int count = Encoding.UTF8.GetChars(tempBuffer, 0, tempBuffer.Length, tempStr, 0);
                int asciiCount = tempStr.Take(count).Count(chr => 32 <= chr && chr <= 127);

                if (maxCharCount < asciiCount)
                {
                    maxCharEnc = enc;
                    maxCharCount = asciiCount;
                }
            }
            this.encType = maxCharEnc;
            this.checEnc = true;
        }
        
        internal class Wz_ImageNode : Wz_Node
        {
            public Wz_ImageNode(string nodeText, Wz_Image image) : base(nodeText)
            {
                this.Image = image;
            }

            public Wz_Image Image { get; private set; }
        }
    }
}

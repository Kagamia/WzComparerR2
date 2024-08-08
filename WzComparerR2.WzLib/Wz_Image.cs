using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;

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
                            ExtractImg(this.Offset, this.Node);
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

        private void ExtractImg(long offset, Wz_Node parent)
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
                    int form = this.WzFile.ReadInt32();
                    this.WzFile.FileStream.Position += 5;
                    int dataLen = this.WzFile.BReader.ReadInt32();
                    parent.Value = new Wz_Png(w, h, dataLen, form, (uint)this.WzFile.FileStream.Position, this);
                    this.WzFile.FileStream.Position += dataLen;
                    break;

                case "Shape2D#Convex2D":
                    entries = this.WzFile.ReadInt32();
                    Wz_Vector[] points = new Wz_Vector[entries];
                    Wz_Node virtualNode = new Wz_Node();
                    for (int i = 0; i < entries; i++)
                    {
                        ExtractImg(offset, virtualNode);
                        if (virtualNode.Value is Wz_Vector point)
                        {
                            points[i] = point;
                        }
                        else
                        {
                            throw new Exception("Convex2D contains non vector2D items.");
                        }
                    }
                    parent.Value = new Wz_Convex(points);
                    break;

                case "Sound_DX8":
                    this.WzFile.FileStream.Position++;
                    dataLen = this.WzFile.ReadInt32();
                    int duration = this.WzFile.ReadInt32();
                    int soundDecl = this.WzFile.BReader.ReadByte();
                    var mediaType = new Interop.AM_MEDIA_TYPE();
                    mediaType.MajorType = new Guid(this.WzFile.BReader.ReadBytes(16));
                    mediaType.SubType = new Guid(this.WzFile.BReader.ReadBytes(16));
                    mediaType.FixedSizeSamples = this.WzFile.BReader.ReadByte() != 0;
                    mediaType.TemporalCompression = this.WzFile.BReader.ReadByte() != 0;
                    mediaType.FormatType = new Guid(this.WzFile.BReader.ReadBytes(16));
                    switch(soundDecl)
                    {
                        case 2:
                            int fmtExLen = this.WzFile.ReadInt32();
                            var fmtExData = this.WzFile.BReader.ReadBytes(fmtExLen);
                            mediaType.CbFormat = (uint)fmtExLen;

                            GCHandle gcHandle = GCHandle.Alloc(fmtExData, GCHandleType.Pinned);
                            try
                            {
                                var waveFormatEx = Marshal.PtrToStructure<Interop.WAVEFORMATEX>(gcHandle.AddrOfPinnedObject());
                                if (fmtExLen != waveFormatEx.CbSize + Marshal.SizeOf<Interop.WAVEFORMATEX>())
                                {
                                    //  parse waveFormatEx after decryption
                                    this.EncKeys.Decrypt(fmtExData, 0, fmtExLen);
                                    waveFormatEx = Marshal.PtrToStructure<Interop.WAVEFORMATEX>(gcHandle.AddrOfPinnedObject());
                                    if (fmtExLen != waveFormatEx.CbSize + Marshal.SizeOf<Interop.WAVEFORMATEX>())
                                    {
                                        throw new Exception($"Failed to parse WAVEFORMATEX struct at offset {this.WzFile.FileStream.Position}.");
                                    }
                                }
                                switch (waveFormatEx.FormatTag)
                                {
                                    case Interop.WAVE_FORMAT_PCM:
                                        mediaType.PbFormat = waveFormatEx;
                                        break;

                                    case Interop.WAVE_FORMAT_MPEGLAYER3:
                                        mediaType.PbFormat = Marshal.PtrToStructure<Interop.MPEGLAYER3WAVEFORMAT>(gcHandle.AddrOfPinnedObject());
                                        break;

                                    default:
                                        throw new Exception($"Unknown WAVEFORMATEX.FormatTag {waveFormatEx.FormatTag} at offset {this.WzFile.FileStream.Position}.");
                                }
                            }
                            finally
                            {
                                gcHandle.Free();
                            }
                            break;
                    }
                    parent.Value = new Wz_Sound((uint)this.WzFile.FileStream.Position, dataLen, duration, mediaType, this);
                    this.WzFile.FileStream.Position += dataLen;
                    break;

                case "UOL":
                    this.WzFile.FileStream.Position++;
                    parent.Value = new Wz_Uol(this.WzFile.ReadString(offset, this.EncKeys));
                    break;

                case "RawData": // introduced in GMS v243
                    int rawDataVer = this.WzFile.BReader.ReadByte();
                    if (rawDataVer == 1) // introduced in KMST 1177
                    {
                        if (this.WzFile.BReader.ReadByte() == 0x01) // read sub property
                        {
                            this.WzFile.FileStream.Position += 2;
                            entries = this.WzFile.ReadInt32();
                            for (int i = 0; i < entries; i++)
                            {
                                ExtractValue(offset, parent);
                            }
                        }
                    }
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
                    int objDataLen = this.WzFile.BReader.ReadInt32();
                    long eob = this.WzFile.FileStream.Position + objDataLen;
                    ExtractImg(offset, parent);
                    if (this.WzFile.FileStream.Position != eob)
                    {
                        throw new Exception($"Object is not fully loaded at offset {this.WzFile.FileStream.Position}.");
                    }
                    break;

                default:
                    throw new Exception($"Unknown value type {flag} at offset {this.WzFile.FileStream.Position}.");
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

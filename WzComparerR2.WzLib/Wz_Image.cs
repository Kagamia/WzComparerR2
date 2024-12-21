using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib
{
    public class Wz_Image
    {
        public Wz_Image(string name, int size, int cs32, uint hashOff, uint hashPos, IMapleStoryFile wz_f)
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
        private Stream stream;
        private Wz_CryptoKeyType encType;

        public string Name { get; set; }
        public IMapleStoryFile WzFile { get; set; }
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
                if (this.stream == null)
                {
                    this.stream = this.OpenRead();
                }

                bool disabledChec = this.WzFile?.WzStructure?.ImgCheckDisabled ?? false;
                if (!disabledChec && !this.chec)
                {
                    if (this.Checksum != this.CalcCheckSum(this.stream))
                    {
                        e = new ArgumentException("checksum error");
                        return false;
                    }
                    this.chec = true;
                }

                if (this.IsTextFormat())
                {
                    var reader = new WzStreamReader(this.stream);

                    try
                    {
                        lock (this.WzFile.ReadLock)
                        {
                            reader.BaseStream.Position = 0;
                            this.ExtractImgInTextFormat(reader, this.Node);
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
                else
                {
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
                            var reader = new WzBinaryReader(this.stream, true);
                            reader.BaseStream.Position = 0;

                            if (!this.IsLuaImage)
                            {
                                ExtractImg(reader, this.Node);
                            }
                            else
                            {
                                ExtractLua(reader);
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
            }
            e = null;
            return true;
        }

        public void Unextract()
        {
            this.extr = false;
            if (this.stream != null)
            {
                this.stream.Dispose();
                this.stream = null;
            }
            this.Node.Nodes.Clear();
        }

        public virtual unsafe int CalcCheckSum(Stream stream)
        {
            lock (this.WzFile.ReadLock)
            {
                stream.Position = 0;
                int cs = 0;
                int size = this.Size;
                var buffer = ArrayPool<byte>.Shared.Rent(4096);

                try
                {
                    int count;
                    while ((count = stream.Read(buffer, 0, Math.Min(size, buffer.Length))) > 0)
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
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                if (size > 0)
                {
                    throw new EndOfStreamException();
                }
                return cs;
            }
        }

        public virtual Stream OpenRead()
        {
            if (this.stream == null)
            {
                this.stream = new PartialStream(this.WzFile.FileStream, this.Offset, this.Size, true);
            }
            return this.stream;
        }

        private void ExtractImg(WzBinaryReader reader, Wz_Node parent)
        {
            int entries;
            string tag = reader.ReadImageObjectTypeName(this.EncKeys);
            switch (tag)
            {
                case "Property":
                    reader.SkipBytes(2);
                    entries = reader.ReadCompressedInt32();
                    for (int i = 0; i < entries; i++)
                    {
                        ExtractValue(reader, parent);
                    }
                    break;

                case "Shape2D#Vector2D":
                    parent.Value = new Wz_Vector(reader.ReadCompressedInt32(), reader.ReadCompressedInt32());
                    break;

                case "Canvas":
                    reader.SkipBytes(1);
                    if (reader.ReadByte() == 0x01)
                    {
                        // read a mini Property
                        reader.SkipBytes(2);
                        entries = reader.ReadCompressedInt32();
                        for (int i = 0; i < entries; i++)
                        {
                            ExtractValue(reader, parent);
                        }
                    }
                    int w = reader.ReadCompressedInt32();
                    int h = reader.ReadCompressedInt32();
                    int form = reader.ReadCompressedInt32() + reader.ReadByte();
                    reader.SkipBytes(4);
                    int dataLen = reader.ReadInt32();
                    parent.Value = new Wz_Png(w, h, dataLen, form, (uint)reader.BaseStream.Position, this);
                    reader.SkipBytes(dataLen);
                    break;

                case "Shape2D#Convex2D":
                    entries = reader.ReadCompressedInt32();
                    Wz_Vector[] points = new Wz_Vector[entries];
                    Wz_Node virtualNode = new Wz_Node();
                    for (int i = 0; i < entries; i++)
                    {
                        ExtractImg(reader, virtualNode);
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
                    reader.SkipBytes(1);
                    dataLen = reader.ReadCompressedInt32();
                    int duration = reader.ReadCompressedInt32();
                    int soundDecl = reader.ReadByte();
                    var mediaType = new Interop.AM_MEDIA_TYPE();
                    mediaType.MajorType = new Guid(reader.ReadBytes(16));
                    mediaType.SubType = new Guid(reader.ReadBytes(16));
                    mediaType.FixedSizeSamples = reader.ReadByte() != 0;
                    mediaType.TemporalCompression = reader.ReadByte() != 0;
                    mediaType.FormatType = new Guid(reader.ReadBytes(16));
                    switch(soundDecl)
                    {
                        case 2:
                            int fmtExLen = reader.ReadCompressedInt32();
                            var fmtExData = reader.ReadBytes(fmtExLen);
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
                                        throw new Exception($"Failed to parse WAVEFORMATEX struct at offset {this.Offset}+{reader.BaseStream.Position}.");
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
                                        throw new Exception($"Unknown WAVEFORMATEX.FormatTag {waveFormatEx.FormatTag} at offset {this.Offset}+{reader.BaseStream.Position}.");
                                }
                            }
                            finally
                            {
                                gcHandle.Free();
                            }
                            break;
                    }
                    parent.Value = new Wz_Sound((uint)reader.BaseStream.Position, dataLen, duration, mediaType, this);
                    reader.SkipBytes(dataLen);
                    break;

                case "UOL":
                    reader.SkipBytes(1);
                    parent.Value = new Wz_Uol(reader.ReadImageString(this.EncKeys));
                    break;

                case "RawData": // introduced in GMS v243
                    int rawDataVer = reader.ReadByte();
                    if (rawDataVer == 1) // introduced in KMST 1177
                    {
                        if (reader.ReadByte() == 0x01) // read sub property
                        {
                            reader.SkipBytes(2);
                            entries = reader.ReadCompressedInt32();
                            for (int i = 0; i < entries; i++)
                            {
                                ExtractValue(reader, parent);
                            }
                        }
                    }
                    int rawDataLen = reader.ReadCompressedInt32();
                    parent.Value = new Wz_RawData((uint)reader.BaseStream.Position, rawDataLen, this);
                    reader.SkipBytes(rawDataLen);
                    break;

                case "Canvas#Video": // introduced in KMST v1181
                    reader.SkipBytes(3);
                    int videoLen = reader.ReadCompressedInt32();
                    parent.Value = new Wz_Video((uint)reader.BaseStream.Position, videoLen, this);
                    reader.SkipBytes(videoLen);
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
            this.stream.Position = 0;
            var reader = new WzBinaryReader(this.stream, false);
            var encKey = this.WzFile.WzStructure.encryption.GetKeys(keyType);
            switch (reader.ReadImageObjectTypeName(encKey))
            {
                case "Property":
                case "Shape2D#Vector2D":
                case "Canvas":
                case "Shape2D#Convex2D":
                case "Sound_DX8":
                case "UOL":
                case "RawData":
                    return true;
                default:
                    return false;
            }
        }

        private void ExtractValue(WzBinaryReader reader, Wz_Node parent)
        {
            parent = parent.Nodes.Add(reader.ReadImageString(this.EncKeys));
            byte flag = reader.ReadByte();
            switch (flag)
            {
                case 0x00:
                    parent.Value = null;
                    break;

                case 0x02:
                case 0x0B:
                    parent.Value = reader.ReadInt16();
                    break;

                case 0x03:
                case 0x13:
                    // case 0x14:
                    parent.Value = reader.ReadCompressedInt32();
                    break;

                case 0x14:
                    parent.Value = reader.ReadCompressedInt64();
                    break;

                case 0x04:
                    parent.Value = reader.ReadCompressedSingle();
                    break;

                case 0x05:
                    parent.Value = reader.ReadDouble();
                    break;

                case 0x08:
                    parent.Value = reader.ReadImageString(this.EncKeys);
                    break;

                case 0x09:
                    int objDataLen = reader.ReadInt32();
                    long eob = reader.BaseStream.Position + objDataLen;
                    this.ExtractImg(reader, parent);
                    if (reader.BaseStream.Position != eob)
                    {
                        throw new Exception($"Object is not fully loaded at offset {this.Offset}+{reader.BaseStream.Position}.");
                    }
                    break;

                default:
                    throw new Exception($"Unknown value type {flag} at offset {this.Offset}+{reader.BaseStream.Position}.");
            }
        }

        private void ExtractLua(WzBinaryReader reader)
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var flag = reader.ReadByte();

                switch (flag)
                {
                    case 0x01:
                        ExtractLuaValue(reader, this.Node);
                        break;

                    default:
                        throw new Exception($"Unknown Lua flag {flag} at Offset {this.Offset}+{reader.BaseStream.Position}.");
                }
            }
        }

        private void ExtractLuaValue(WzBinaryReader reader, Wz_Node parent)
        {
            int len = reader.ReadCompressedInt32();
            byte[] data = reader.ReadBytes(len);
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

        private bool IsTextFormat()
        {
            ReadOnlySpan<byte> signatureBytes = "#Property"u8;
            if (this.stream.Length < signatureBytes.Length)
            {
                return false;
            }

            this.stream.Position = 0;
            Span<byte> buffer = stackalloc byte[signatureBytes.Length];
            this.stream.ReadExactly(buffer);
            
            return buffer.SequenceEqual(signatureBytes);
        }

        private void ExtractImgInTextFormat(WzStreamReader reader, Wz_Node parent)
        {
            reader.SkipLine();
            this.ReadProperty(reader, parent, true);
        }

        private void ReadProperty(WzStreamReader reader, Wz_Node parent, bool isTopLevel = false)
        {
            while (!reader.EndOfStream)
            {
                reader.SkipWhitespaceExceptLineEnding();
                string key = reader.ReadUntilWhitespace();

                if (string.IsNullOrEmpty(key)) // skip empty line
                {
                    reader.SkipLine();
                    continue;
                }
                else if (key == "}" && !isTopLevel) // end property
                {
                    if (!reader.SkipLineAndCheckEmpty())
                    {
                        throw new Exception("Incorrect property end line.");
                    }
                    return;
                }

                reader.SkipWhitespaceExceptLineEnding();
                int equalSign = reader.Read();
                if (equalSign != '=')
                    throw new Exception($"Expect '=' sign but got '{(char)equalSign}'.");
                reader.SkipWhitespaceExceptLineEnding();

                string stringVal = reader.ReadLine();

                if (string.IsNullOrEmpty(stringVal))
                {
                    parent.Nodes.Add(key);
                }
                else if (stringVal == "{") // start property
                {
                    Wz_Node child = parent.Nodes.Add(key);
                    this.ReadProperty(reader, child, false);
                }
                else if (int.TryParse(stringVal, out var intVal))
                {
                    parent.Nodes.Add(key).Value = intVal;
                }
                else if (long.TryParse(stringVal, out var longVal))
                {
                    parent.Nodes.Add(key).Value = longVal;
                }
                else if (double.TryParse(stringVal, out var doubleVal))
                {
                    parent.Nodes.Add(key).Value = doubleVal;
                }
                else
                {
                    parent.Nodes.Add(key).Value = stringVal;
                }
            }
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

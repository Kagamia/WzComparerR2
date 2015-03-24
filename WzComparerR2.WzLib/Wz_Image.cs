using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.WzLib
{
    public class Wz_Image
    {
        public Wz_Image(string name, int size, int cs32, int offs, bool on_list, Wz_File wz_f)
        {
            this.name = name;
            this.wzFile = wz_f;
            this.size = size;
            this.checksum = cs32;
            this.offset = offs;
            this.isOnList = on_list;
            this.node = new Wz_Node(name);
            this.node.Value = this;
            this.extr = false;
            this.chec = false;
            this.checEnc = false;
        }

        private string name;
        private Wz_File wzFile;
        private int size;
        private int checksum;
        private int offset;
        private bool isOnList;
        private Wz_Node node;

        private bool extr;
        private bool chec;
        private bool checEnc;

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
        public Wz_File WzFile
        {
            get { return this.wzFile; }
            set { this.wzFile = value; }
        }
        public int Size
        {
            get { return this.size; }
            set { this.size = value; }
        }
        public int Checksum
        {
            get { return this.checksum; }
            set { this.checksum = value; }
        }
        public int Offset
        {
            get { return this.offset; }
            set { this.offset = value; }
        }
        public bool IsOnList
        {
            get { return this.isOnList; }
            set { this.isOnList = value; }
        }
        public Wz_Node Node
        {
            get { return this.node; }
        }

        public Wz_Node OwnerNode { get; set; }

        public bool TryExtract()
        {
            Exception ex;
            return TryExtract(out ex);
        }

        public bool TryExtract(out Exception e)
        {
            if (!this.extr)
            {
                if (!this.chec)
                {
                    if (this.Checksum != GetCheckSum())
                    {
                        e = new ArgumentException("checksum error");
                        return false;
                    }
                    this.chec = true;
                }
                if (!this.checEnc)
                {
                    TryDetectEnc();
                }

                try
                {
                    lock (this.WzFile.ReadLock)
                    {
                        this.WzFile.FileStream.Position = this.Offset;
                        ExtractImg(this.offset, this.Node, 0);
                        this.WzFile.stringTable.Clear();
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

        private unsafe int GetCheckSum()
        {
            lock (this.WzFile.ReadLock)
            {
                this.WzFile.FileStream.Position = this.offset;
                int cs = 0;
                byte[] buffer = new byte[4096];
                int count, size = this.Size;
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

        private void ExtractImg(int offset, Wz_Node parent, int eob)
        {
            int entries = 0;

            switch (this.WzFile.ReadString(offset, this.IsOnList))
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
                    parent.Value = new Wz_Png(w, h, bufsize - 1, form, (int)this.WzFile.FileStream.Position + 1, this.WzFile);
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
                    int headerLen = eob - len - (int)wzFile.FileStream.Position;
                    byte[] header = wzFile.BReader.ReadBytes(headerLen);
                    parent.Value = new Wz_Sound(eob - len, len, header, ms, this.WzFile);
                    this.WzFile.FileStream.Position = eob;
                    break;

                case "UOL":
                    this.WzFile.FileStream.Position++;
                    parent.Value = new Wz_Uol(this.WzFile.ReadString(offset, this.IsOnList));
                    break;

                default:
                    break;
            }
        }

        private void TryDetectEnc()
        {
            Wz_Crypto crypto = this.wzFile.WzStructure.encryption;

            if (IsIllegalTag(this.isOnList))
            {
                this.checEnc = true;
            }
            else if (crypto.enc_type != Wz_Crypto.enc_unknown)
            {
                if (IsIllegalTag(false))
                {
                    this.isOnList = false;
                    this.checEnc = true;
                }
                else if (IsIllegalTag(true))
                {
                    this.isOnList = true;
                    this.checEnc = true;
                }
            }
            else
            {
                byte[] keys = crypto.keys;
                crypto.enc_type = Wz_Crypto.enc_KMS;
                crypto.keys = crypto.keys_kms;
                if (IsIllegalTag(false))
                {
                    this.isOnList = false;
                    this.checEnc = true;
                    return;
                }
                else if (IsIllegalTag(true))
                {
                    this.isOnList = true;
                    this.checEnc = true;
                    return;
                }

                crypto.enc_type = Wz_Crypto.enc_GMS;
                crypto.keys = crypto.keys_gms;
                if (IsIllegalTag(false))
                {
                    this.isOnList = false;
                    this.checEnc = true;
                    return;
                }
                else if (IsIllegalTag(true))
                {
                    this.isOnList = true;
                    this.checEnc = true;
                    return;
                }

                crypto.enc_type = Wz_Crypto.enc_unknown;
                crypto.keys = keys;
            }
        }

        private bool IsIllegalTag(bool useEnc)
        {
            this.WzFile.FileStream.Position = this.offset;
            switch (this.WzFile.ReadString(offset, useEnc))
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

        private void ExtractValue(int offset, Wz_Node parent)
        {
            parent = parent.Nodes.Add(this.WzFile.ReadString(offset, this.IsOnList));
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
                    parent.Value = this.WzFile.ReadString(offset, this.IsOnList);
                    break;

                case 0x09:
                    ExtractImg(offset, parent, this.WzFile.BReader.ReadInt32() + (int)this.WzFile.FileStream.Position);
                    break;

                default:
                    throw new Exception("读取值错误." + flag + " at Offset: " + this.WzFile.FileStream.Position);
            }
        }

    }
}

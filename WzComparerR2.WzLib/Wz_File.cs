using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WzComparerR2.WzLib
{
    public class Wz_File
    {
        public Wz_File(string fileName, Wz_Structure wz)
        {
            this.imageCount = 0;
            this.wzStructure = wz;
            this.fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this.bReader = new BinaryReader(this.FileStream);
            this.loaded = this.GetHeader(fileName);
            this.stringTable = new Dictionary<long, string>();
        }

        private FileStream fileStream;
        private BinaryReader bReader;
        private Wz_Structure wzStructure;
        private Wz_Header header;
        private Wz_Node node;
        private int imageCount;
        private bool loaded;
        private Wz_Type type;

        public Encoding TextEncoding { get; set; }

        public readonly object ReadLock = new object();

        internal Dictionary<long, string> stringTable;
        internal byte[] tempBuffer;

        public FileStream FileStream
        {
            get { return fileStream; }
        }

        public BinaryReader BReader
        {
            get { return bReader; }
        }

        public Wz_Structure WzStructure
        {
            get { return wzStructure; }
            set { wzStructure = value; }
        }

        public Wz_Header Header
        {
            get { return header; }
            private set { header = value; }
        }

        public Wz_Node Node
        {
            get { return node; }
            set { node = value; }
        }

        public int ImageCount
        {
            get { return imageCount; }
        }

        public bool Loaded
        {
            get { return loaded; }
        }

        public Wz_Type Type
        {
            get { return type; }
            set { type = value; }
        }

        public void Close()
        {
            if (this.bReader != null)
                this.bReader.Close();
            if (this.fileStream != null)
                this.fileStream.Close();
        }

        private bool GetHeader(string fileName)
        {
            this.fileStream.Position = 0;
            int filesize = (int)this.FileStream.Length;
            if (filesize < 4) { goto __failed; }

            string signature = new string(this.BReader.ReadChars(4));
            if (signature != "PKG1") { goto __failed; }

            int datasize = (int)this.BReader.ReadInt64();
            int headersize = this.BReader.ReadInt32();
            string copyright = new string(this.BReader.ReadChars(headersize - (int)this.FileStream.Position));
            int encver = this.BReader.ReadUInt16();

            this.Header = new Wz_Header(signature, copyright, fileName, headersize, datasize, filesize, encver);
            return true;

            __failed:
            this.header = new Wz_Header(null, null, fileName, 0, 0, filesize, 0);
            return false;
        }

        public int ReadInt32()
        {
            int s = this.BReader.ReadSByte();
            return (s == -128) ? this.BReader.ReadInt32() : s;
        }

        public long ReadInt64()
        {
            int s = this.BReader.ReadSByte();
            return (s == -128) ? this.BReader.ReadInt64() : s;
        }

        public float ReadSingle()
        {
            float fl = this.BReader.ReadSByte();
            return (fl == -128) ? this.BReader.ReadSingle() : fl;
        }

        public string ReadString(long offset)
        {
            byte b = this.BReader.ReadByte();
            switch (b)
            {
                case 0x00:
                case 0x73:
                    return ReadString();

                case 0x01:
                case 0x1B:
                    return ReadStringAt(offset + this.BReader.ReadInt32());

                case 0x04:
                    this.FileStream.Position += 8;
                    break;

                default:
                    throw new Exception("读取字符串错误 在:" + this.FileStream.Name + " " + this.FileStream.Position);
            }
            return string.Empty;
        }

        public string ReadStringAt(long offset)
        {
            long oldoffset = this.FileStream.Position;
            string str;
            if (!stringTable.TryGetValue(offset, out str))
            {
                this.FileStream.Position = offset;
                str = ReadString();
                stringTable[offset] = str;
                this.FileStream.Position = oldoffset;
            }
            return str;
        }

        public unsafe string ReadString()
        {
            int size = this.BReader.ReadSByte();
            string result = null;
            if (size < 0)
            {
                byte mask = 0xAA;
                size = (size == -128) ? this.BReader.ReadInt32() : -size;

                var buffer = GetStringBuffer(size);
                this.fileStream.Read(buffer, 0, size);
                this.WzStructure.encryption.keys.Decrypt(buffer, 0, size);

                fixed (byte* pData = buffer)
                {
                    for (int i = 0; i < size; i++)
                    {
                        pData[i] ^= mask;
                        unchecked { mask++; }
                    }

                    var enc = this.TextEncoding ?? Encoding.Default;
                    result = enc.GetString(buffer, 0, size);
                }
            }
            else if (size > 0)
            {
                ushort mask = 0xAAAA;
                if (size == 127)
                {
                    size = this.BReader.ReadInt32();
                }

                var buffer = GetStringBuffer(size * 2);
                this.fileStream.Read(buffer, 0, size * 2);
                this.WzStructure.encryption.keys.Decrypt(buffer, 0, size * 2);

                fixed (byte* pData = buffer)
                {
                    ushort* pChar = (ushort*)pData;
                    for (int i = 0; i < size; i++)
                    {
                        pChar[i] ^= mask;
                        unchecked { mask++; }
                    }

                    result = new string((char*)pChar, 0, size);
                }
            }
            else
            {
                return string.Empty;
            }

            //memory optimize
            if (result.Length <= 4)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    if (result[i] >= 0x80)
                    {
                        return result;
                    }
                }
                return string.Intern(result);
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// 为字符串解密提供缓冲区。
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private byte[] GetStringBuffer(int size)
        {
            if (size <= 4096)
            {
                if (tempBuffer == null || tempBuffer.Length < size)
                {
                    Array.Resize(ref tempBuffer, size);
                }
                return tempBuffer;
            }
            else
            {
                return new byte[size];
            }
        }

        public uint CalcOffset(uint filePos, uint hashedOffset)
        {
            uint offset = (uint)(filePos - 0x3C) ^ 0xFFFFFFFF;
            int distance = 0;
            long pos = this.FileStream.Position;

            offset *= this.Header.HashVersion;
            offset -= 0x581C3F6D;
            distance = (int)offset & 0x1F;
            offset = (offset << distance) | (offset >> (32 - distance));
            offset ^= hashedOffset;
            offset += 0x78;

            return offset;
        }

        public void GetDirTree(Wz_Node parent)
        {
            GetDirTree(parent, true);
        }

        public void GetDirTree(Wz_Node parent, bool useBaseWz)
        {
            List<string> dirs = new List<string>();
            string name = null;
            int size = 0;
            int cs32 = 0;
            //int offs = 0;

            int count = ReadInt32();

            for (int i = 0; i < count; i++)
            {
                switch ((int)this.BReader.ReadByte())
                {
                    case 0x02:
                        name = this.ReadStringAt(this.Header.HeaderSize + 1 + this.BReader.ReadInt32());
                        goto case 0xffff;
                    case 0x04:
                        name = this.ReadString();
                        goto case 0xffff;

                    case 0xffff:
                        size = this.ReadInt32();
                        cs32 = this.ReadInt32();
                        uint pos = (uint)this.bReader.BaseStream.Position;
                        uint hashOffset = this.bReader.ReadUInt32();

                        Wz_Image img = new Wz_Image(name, size, cs32, hashOffset, pos, this);
                        Wz_Node childNode = parent.Nodes.Add(name);
                        childNode.Value = img;
                        img.OwnerNode = childNode;

                        this.imageCount++;
                        break;

                    case 0x03:
                        name = this.ReadString();
                        size = this.ReadInt32();
                        cs32 = this.ReadInt32();
                        this.FileStream.Position += 4;
                        dirs.Add(name);
                        break;
                }
            }

            int dirCount = dirs.Count;
            bool willLoadBaseWz = useBaseWz ? parent.Text.Equals("base.wz", StringComparison.OrdinalIgnoreCase) : false;

            var baseFolder = Path.GetDirectoryName(this.header.FileName);

            if (willLoadBaseWz && this.WzStructure.AutoDetectExtFiles)
            {
                for (int i = 0; i < dirCount; i++)
                {
                    //检测文件名
                    var m = Regex.Match(dirs[i], @"^([A-Za-z]+)$");
                    if (m.Success)
                    {
                        string wzTypeName = m.Result("$1");

                        //检测扩展wz文件
                        for (int fileID = 2; ; fileID++)
                        {
                            string extDirName = wzTypeName + fileID;
                            string extWzFile = Path.Combine(baseFolder, extDirName + ".wz");
                            if (File.Exists(extWzFile))
                            {
                                if (!dirs.Take(dirCount).Any(dir => extDirName.Equals(dir, StringComparison.OrdinalIgnoreCase)))
                                {
                                    dirs.Add(extDirName);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        //检测KMST1058的wz文件
                        for (int fileID = 1; ; fileID++)
                        {
                            string extDirName = wzTypeName + fileID.ToString("D3");
                            string extWzFile = Path.Combine(baseFolder, extDirName + ".wz");
                            if (File.Exists(extWzFile))
                            {
                                if (!dirs.Take(dirCount).Any(dir => extDirName.Equals(dir, StringComparison.OrdinalIgnoreCase)))
                                {
                                    dirs.Add(extDirName);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < dirs.Count; i++)
            {
                string dir = dirs[i];
                Wz_Node t = parent.Nodes.Add(dir);
                if (willLoadBaseWz)
                {
                    this.WzStructure.has_basewz = true;
                    if (i < dirCount)
                    {
                        GetDirTree(t, false);
                    }

                    try
                    {
                        string filePath = Path.Combine(baseFolder, dir + ".wz");
                        if (File.Exists(filePath))
                            this.WzStructure.LoadFile(filePath, t);
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else
                {
                    GetDirTree(t, false);
                }
            }

            parent.Nodes.Trim();
        }

        private string getFullPath(Wz_Node parent, string name)
        {
            List<string> path = new List<string>(5);
            path.Add(name.ToLower());
            while (parent != null && !(parent.Value is Wz_File))
            {
                path.Insert(0, parent.Text.ToLower());
                parent = parent.ParentNode;
            }
            if (parent != null)
            {
                path.Insert(0, parent.Text.ToLower().Replace(".wz", ""));
            }
            return string.Join("/", path.ToArray());
        }

        public void DetectWzType()
        {
            this.type = Wz_Type.Unknown;
            if (this.node == null)
            {
                return;
            }

            if (this.node.Nodes["smap.img"] != null
                || this.node.Nodes["zmap.img"] != null)
            {
                this.type = Wz_Type.Base;
            }
            else if (this.node.Nodes["00002000.img"] != null
                || this.node.Nodes["Accessory"] != null
                || this.node.Nodes["Weapon"] != null)
            {
                this.type = Wz_Type.Character;
            }
            else if (this.node.Nodes["BasicEff.img"] != null
                || this.node.Nodes["SetItemInfoEff.img"] != null)
            {
                this.type = Wz_Type.Effect;
            }
            else if (this.node.Nodes["Commodity.img"] != null
                || this.node.Nodes["Curse.img"] != null)
            {
                this.type = Wz_Type.Etc;
            }
            else if (this.node.Nodes["Cash"] != null
                || this.node.Nodes["Consume"] != null)
            {
                this.type = Wz_Type.Item;
            }
            else if (this.node.Nodes["Back"] != null
                || this.node.Nodes["Obj"] != null
                || this.node.Nodes["Physics.img"] != null)
            {
                this.type = Wz_Type.Map;
            }
            else if (this.node.Nodes["PQuest.img"] != null
                || this.node.Nodes["QuestData"] != null)
            {
                this.type = Wz_Type.Quest;
            }
            else if (this.node.Nodes["Attacktype.img"] != null
                || this.node.Nodes["Recipe_9200.img"] != null)
            {
                this.type = Wz_Type.Skill;
            }
            else if (this.node.Nodes["Bgm00.img"] != null
                || this.node.Nodes["BgmUI.img"] != null)
            {
                this.type = Wz_Type.Sound;
            }
            else if (this.node.Nodes["MonsterBook.img"] != null
                || this.node.Nodes["EULA.img"] != null)
            {
                this.type = Wz_Type.String;
            }
            else if (this.node.Nodes["CashShop.img"] != null
                || this.node.Nodes["UIWindow.img"] != null)
            {
                this.type = Wz_Type.UI;
            }
            
            if (this.type == Wz_Type.Unknown) //用文件名来判断
            {
                string wzName = this.node.Text;

                Match m = Regex.Match(wzName, @"^([A-Za-z]+)(\d+)?(?:\.wz)?$");
                if (m.Success)
                {
                    wzName = m.Result("$1");
                }

                try
                {
                    this.type = (Wz_Type)Enum.Parse(typeof(Wz_Type), wzName, true);
                }
                catch
                {
                    this.type = Wz_Type.Unknown;
                }
            }
        }

        public void DetectWzVersion()
        {
            if (!this.header.VersionChecked)
            {
                //选择最小的img作为实验品
                Wz_Image minSizeImg = null;
                List<Wz_Image> imgList = new List<Wz_Image>(this.imageCount);
                foreach (var img in (EnumerableAllWzImage(this.node)))
                {
                    if (img.Size >= 20
                        && (minSizeImg == null || img.Size < minSizeImg.Size))
                    {
                        minSizeImg = img;
                    }

                    imgList.Add(img);
                }

                if (minSizeImg == null)
                {
                    if (imgList.Count <= 0)
                    {
                        return;
                    }
                    minSizeImg = imgList[0];
                }

                while (this.header.TryGetNextVersion())
                {
                    uint offs = CalcOffset(minSizeImg.HashedOffsetPosition, minSizeImg.HashedOffset);

                    if (offs < this.header.HeaderSize || offs + minSizeImg.Size > this.fileStream.Length)  //img块越界
                    {
                        continue;
                    }

                    this.fileStream.Position = offs;
                    switch (this.fileStream.ReadByte())
                    {
                        case 0x73:
                        case 0x1b:
                            //试读img第一个string
                            break;
                        default:
                            continue;
                    }

                    minSizeImg.Offset = offs;
                    if (minSizeImg.TryExtract()) //试读成功
                    {
                        minSizeImg.Unextract();
                        this.header.VersionChecked = true;
                        break;
                    }
                }

                if (this.header.VersionChecked) //重新计算全部img
                {
                    foreach (var img in imgList)
                    {
                        img.Offset = CalcOffset(img.HashedOffsetPosition, img.HashedOffset);
                    }
                }
                else //最终测试失败 那就失败吧..
                {
                    this.header.VersionChecked = true;
                }
            }
        }

        private IEnumerable<Wz_Image> EnumerableAllWzImage(Wz_Node parentNode)
        {
            foreach (var node in parentNode.Nodes)
            {
                Wz_Image img = node.Value as Wz_Image;
                if (img != null)
                {
                    yield return img;
                }

                if (!(node.Value is Wz_File) && node.Nodes.Count > 0)
                {
                    foreach (var imgChild in EnumerableAllWzImage(node))
                    {
                        yield return imgChild;
                    }
                }
            }
        }
    }
}

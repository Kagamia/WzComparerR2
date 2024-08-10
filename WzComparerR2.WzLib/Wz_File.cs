﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WzComparerR2.WzLib
{
    public class Wz_File : IMapleStoryFile, IDisposable
    {
        public Wz_File(string fileName, Wz_Structure wz)
        {
            this.imageCount = 0;
            this.wzStructure = wz;
            this.fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this.bReader = new BinaryReader(this.FileStream);
            this.loaded = this.GetHeader(fileName);
            this.stringTable = new Dictionary<long, string>();
            this.directories = new List<Wz_Directory>();
        }

        private FileStream fileStream;
        private BinaryReader bReader;
        private Wz_Structure wzStructure;
        private Wz_Header header;
        private Wz_Node node;
        private int imageCount;
        private bool loaded;
        private bool isSubDir;
        private Wz_Type type;
        private List<Wz_File> mergedWzFiles;
        private Wz_File ownerWzFile;
        private readonly List<Wz_Directory> directories;

        public Encoding TextEncoding { get; set; }

        public object ReadLock => this.fileStream;

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

        public bool IsSubDir
        {
            get { return this.isSubDir; }
        }

        public Wz_Type Type
        {
            get { return type; }
            set { type = value; }
        }

        public IEnumerable<Wz_File> MergedWzFiles
        {
            get { return this.mergedWzFiles ?? Enumerable.Empty<Wz_File>(); }
        }

        public Wz_File OwnerWzFile
        {
            get { return this.ownerWzFile; }
        }

        Wz_Structure IMapleStoryFile.WzStructure => this.wzStructure;

        Stream IMapleStoryFile.FileStream => this.fileStream;

        object IMapleStoryFile.ReadLock => this.ReadLock;

        public void Close()
        {
            if (this.bReader != null)
                this.bReader.Close();
            if (this.fileStream != null)
                this.fileStream.Close();
        }

        void IDisposable.Dispose()
        {
            this.Close();
        }

        private bool GetHeader(string fileName)
        {
            this.fileStream.Position = 0;
            long filesize = this.FileStream.Length;
            if (filesize < 4) { goto __failed; }

            string signature = new string(this.BReader.ReadChars(4));
            if (signature != "PKG1") { goto __failed; }

            long dataSize = this.BReader.ReadInt64();
            int headerSize = this.BReader.ReadInt32();
            string copyright = new string(this.BReader.ReadChars(headerSize - (int)this.FileStream.Position));

            // encver detecting:
            // Since KMST1132, wz removed the 2 bytes encver, and use a fixed wzver '777'.
            // Here we try to read the first 2 bytes from data part and guess if it looks like an encver.
            bool encverMissing = false;
            int encver = -1;
            if (dataSize >= 2)
            {
                this.fileStream.Position = headerSize;
                encver = this.BReader.ReadUInt16();
                // encver always less than 256
                if (encver > 0xff)
                {
                    encverMissing = true;
                }
                else if (encver == 0x80)
                {
                    // there's an exceptional case that the first field of data part is a compressed int which determined property count,
                    // if the value greater than 127 and also to be a multiple of 256, the first 5 bytes will become to
                    //   80 00 xx xx xx
                    // so we additional check the int value, at most time the child node count in a wz won't greater than 65536.
                    if (dataSize >= 5)
                    {
                        this.fileStream.Position = headerSize;
                        int propCount = this.ReadInt32();
                        if (propCount > 0 && (propCount & 0xff) == 0 && propCount <= 0xffff)
                        {
                            encverMissing = true;
                        }
                    }
                }
            }
            else
            {
                // Obviously, if data part have only 1 byte, encver must be deleted.
                encverMissing = true;
            }

            int dataStartPos = headerSize + (encverMissing ? 0 : 2);
            this.Header = new Wz_Header(signature, copyright, fileName, headerSize, dataSize, filesize, dataStartPos);

            if (encverMissing)
            {
                // not sure if nexon will change this magic version, just hard coded.
                this.Header.SetWzVersion(777);
                this.Header.VersionChecked = true;
                this.Header.Capabilities |= Wz_Capabilities.EncverMissing;
            }
            else
            {
                this.Header.SetOrdinalVersionDetector(encver);
            }

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
            return this.ReadString(offset, this.WzStructure.encryption.keys);
        }

        public string ReadString(long offset, Wz_CryptoKeyType keyType)
        {
            return this.ReadString(offset, this.WzStructure.encryption.GetKeys(keyType));
        }

        public string ReadString(long offset, Wz_Crypto.Wz_CryptoKey cryptoKey)
        {
            byte b = this.BReader.ReadByte();
            switch (b)
            {
                case 0x00:
                case 0x73:
                    return ReadString(cryptoKey);

                case 0x01:
                case 0x1B:
                    return ReadStringAt(offset + this.BReader.ReadInt32(), cryptoKey);

                case 0x04:
                    this.FileStream.Position += 8;
                    break;

                default:
                    throw new Exception("读取字符串错误 在:" + this.FileStream.Name + " " + this.FileStream.Position);
            }
            return string.Empty;
        }

        private string ReadStringAt(long offset, Wz_Crypto.Wz_CryptoKey cryptoKey)
        {
            long oldoffset = this.FileStream.Position;
            string str;
            if (!stringTable.TryGetValue(offset, out str))
            {
                this.FileStream.Position = offset;
                str = ReadString(cryptoKey);
                stringTable[offset] = str;
                this.FileStream.Position = oldoffset;
            }
            return str;
        }

        private unsafe string ReadString(Wz_Crypto.Wz_CryptoKey cryptoKey)
        {
            int size = this.BReader.ReadSByte();
            string result = null;
            if (size < 0)
            {
                byte mask = 0xAA;
                size = (size == -128) ? this.BReader.ReadInt32() : -size;

                var buffer = GetStringBuffer(size);
                this.fileStream.Read(buffer, 0, size);
                cryptoKey.Decrypt(buffer, 0, size);

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
                cryptoKey.Decrypt(buffer, 0, size * 2);

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
            int distance;

            offset *= this.Header.HashVersion;
            offset -= 0x581C3F6D;
            distance = (int)offset & 0x1F;
            offset = (offset << distance) | (offset >> (32 - distance));
            offset ^= hashedOffset;
            offset += 0x78;

            return offset;
        }

        public void GetDirTree(Wz_Node parent, bool useBaseWz = false, bool loadWzAsFolder = false)
        {
            List<string> dirs = new List<string>();
            string name = null;
            int size = 0;
            int cs32 = 0;
            uint pos = 0, hashOffset = 0;
            //int offs = 0;

            int count = ReadInt32();
            var cryptoKey = this.WzStructure.encryption.keys;

            for (int i = 0; i < count; i++)
            {
                switch ((int)this.BReader.ReadByte())
                {
                    case 0x02:
                        int stringOffAdd = this.Header.HasCapabilities(Wz_Capabilities.EncverMissing) ? 2 : 1;
                        name = this.ReadStringAt(this.Header.HeaderSize + stringOffAdd + this.BReader.ReadInt32(), cryptoKey);
                        goto case 0xffff;
                    case 0x04:
                        name = this.ReadString(cryptoKey);
                        goto case 0xffff;

                    case 0xffff:
                        size = this.ReadInt32();
                        cs32 = this.ReadInt32();
                        pos = (uint)this.bReader.BaseStream.Position;
                        hashOffset = this.bReader.ReadUInt32();

                        Wz_Image img = new Wz_Image(name, size, cs32, hashOffset, pos, this);
                        Wz_Node childNode = parent.Nodes.Add(name);
                        childNode.Value = img;
                        img.OwnerNode = childNode;

                        this.imageCount++;
                        break;

                    case 0x03:
                        name = this.ReadString(cryptoKey);
                        size = this.ReadInt32();
                        cs32 = this.ReadInt32();
                        pos = (uint)this.bReader.BaseStream.Position;
                        hashOffset = this.bReader.ReadUInt32();
                        this.directories.Add(new Wz_Directory(name, size, cs32, hashOffset, pos, this));
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
                if (i < dirCount)
                {
                    GetDirTree(t, false);
                }

                if (t.Nodes.Count == 0)
                {
                    this.WzStructure.has_basewz |= willLoadBaseWz;

                    try
                    {
                        if (loadWzAsFolder)
                        {
                            string wzFolder = willLoadBaseWz ? Path.Combine(Path.GetDirectoryName(baseFolder), dir) : Path.Combine(baseFolder, dir);
                            if (Directory.Exists(wzFolder))
                            {
                                this.wzStructure.LoadWzFolder(wzFolder, ref t, false);
                                if (!willLoadBaseWz)
                                {
                                    var dirWzFile = t.GetValue<Wz_File>();
                                    dirWzFile.Type = Wz_Type.Unknown;
                                    dirWzFile.isSubDir = true;
                                }
                            }
                        }
                        else if (willLoadBaseWz)
                        {
                            string filePath = Path.Combine(baseFolder, dir + ".wz");
                            if (File.Exists(filePath))
                            {
                                this.WzStructure.LoadFile(filePath, t, false, loadWzAsFolder);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
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

                Match m = Regex.Match(wzName, @"^([A-Za-z]+)_?(\d+)?(?:\.wz)?$");
                if (m.Success)
                {
                    wzName = m.Result("$1");
                }
                this.type = Enum.TryParse<Wz_Type>(wzName, true, out var result) ? result : Wz_Type.Unknown;
            }
        }

        public void DetectWzVersion()
        {
            IWzVersionVerifier wzVersionVerifier;

            switch (this.wzStructure?.WzVersionVerifyMode)
            {
                default:
                case WzVersionVerifyMode.Default:
                    wzVersionVerifier = new DefaultVersionVerifier();
                    break;

                case WzVersionVerifyMode.Fast:
                    wzVersionVerifier = new FastVersionVerifier();
                    break;
            }

            wzVersionVerifier.Verify(this);
        }

        public void MergeWzFile(Wz_File wz_File)
        {
            var children = wz_File.node.Nodes.ToList();
            wz_File.node.Nodes.Clear();
            foreach (var child in children)
            {
                this.node.Nodes.Add(child);
            }

            if (this.mergedWzFiles == null)
            {
                this.mergedWzFiles = new List<Wz_File>();
            }
            this.mergedWzFiles.Add(wz_File);

            wz_File.ownerWzFile = this;
        }


        public interface IWzVersionVerifier
        {
            bool Verify(Wz_File wzFile);
        }

        public abstract class WzVersionVerifier
        {
            protected IEnumerable<Wz_Image> EnumerableAllWzImage(Wz_Node parentNode)
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

            protected bool FastCheckFirstByte(Wz_Image image, byte firstByte)
            {
                if (image.IsLuaImage)
                {
                    // for lua image, the first byte is always 01
                    return firstByte == 0x01;
                }
                else
                {
                    // first element is always a string
                    return firstByte == 0x73 || firstByte == 0x1b;
                }
            }

            protected void CalcOffset(Wz_File wzFile, IEnumerable<Wz_Image> imgList)
            {
                foreach (var img in imgList)
                {
                    img.Offset = wzFile.CalcOffset(img.HashedOffsetPosition, img.HashedOffset);
                }
            }

            protected bool DetectWithWzImage(Wz_File wzFile, Wz_Image testWzImg)
            {
                while (wzFile.header.TryGetNextVersion())
                {
                    uint offs = wzFile.CalcOffset(testWzImg.HashedOffsetPosition, testWzImg.HashedOffset);

                    if (offs < wzFile.header.DirEndPosition || offs + testWzImg.Size > wzFile.fileStream.Length)  //img offset out of file size
                    {
                        continue;
                    }

                    wzFile.fileStream.Position = offs;
                    var firstByte = (byte)wzFile.fileStream.ReadByte();
                    if (!FastCheckFirstByte(testWzImg, firstByte))
                    {
                        continue;
                    }

                    testWzImg.Offset = offs;
                    if (!testWzImg.TryExtract())
                    {
                        continue;
                    }

                    testWzImg.Unextract();
                    wzFile.header.VersionChecked = true;
                    break;
                }

                return wzFile.header.VersionChecked;
            }

            protected bool DetectWithAllWzDir(Wz_File wzFile)
            {
                while (wzFile.header.TryGetNextVersion())
                {
                    bool isSuccess = wzFile.directories.All(testDir =>
                    {
                        uint offs = wzFile.CalcOffset(testDir.HashedOffsetPosition, testDir.HashedOffset);

                        if (offs < wzFile.header.DataStartPosition || offs + 1 > wzFile.header.DirEndPosition) // dir offset out of file size.
                        {
                            return false;
                        }

                        wzFile.fileStream.Position = offs;
                        if (wzFile.fileStream.ReadByte() != 0) // for splitted wz format, dir data only contains one byte: 0x00
                        {
                            return false;
                        }

                        return true;
                    });

                    if (isSuccess)
                    {
                        wzFile.header.VersionChecked = true;
                        break;
                    }
                }

                return wzFile.header.VersionChecked;
            }

            protected bool FastDetectWithAllWzImages(Wz_File wzFile, IList<Wz_Image> imgList)
            {
                var imageSizes = new SizeRange[imgList.Count];
                while (wzFile.header.TryGetNextVersion())
                {
                    int count = 0;
                    bool isSuccess = imgList.All(img =>
                    {
                        uint offs = wzFile.CalcOffset(img.HashedOffsetPosition, img.HashedOffset);
                        if (offs < wzFile.header.DirEndPosition || offs + img.Size > wzFile.fileStream.Length)  //img offset out of file size
                        {
                            return false;
                        }

                        imageSizes[count++] = new SizeRange()
                        {
                            Start = offs,
                            End = offs + img.Size,
                        };
                        return true;
                    });

                    if (isSuccess)
                    {
                        // check if there's any image overlaps with another image.
                        Array.Sort(imageSizes, 0, count);
                        for (int i = 1; i < count; i++)
                        {
                            if (imageSizes[i - 1].End > imageSizes[i].Start)
                            {
                                isSuccess = false;
                                break;
                            }
                        }

                        if (isSuccess)
                        {
                            wzFile.header.VersionChecked = true;
                            break;
                        }
                    }
                }

                return wzFile.header.VersionChecked;
            }

            private struct SizeRange : IComparable<SizeRange>
            {
                public long Start;
                public long End;

                public int CompareTo(SizeRange sr)
                {
                    int result = this.Start.CompareTo(sr.Start);
                    if (result == 0)
                    {
                        result = this.End.CompareTo(sr.End);
                    }
                    return result;
                }
            }
        }

        public class DefaultVersionVerifier : WzVersionVerifier, IWzVersionVerifier
        {
            public bool Verify(Wz_File wzFile)
            {
                List<Wz_Image> imgList = EnumerableAllWzImage(wzFile.node).Where(_img => _img.WzFile == wzFile).ToList();

                if (wzFile.header.VersionChecked)
                {
                    this.CalcOffset(wzFile, imgList);
                }
                else
                {
                    // find the wzImage with minimum size.
                    Wz_Image minSizeImg = imgList.DefaultIfEmpty().Aggregate((_img1, _img2) => _img1.Size < _img2.Size ? _img1 : _img2);

                    if (minSizeImg == null && imgList.Count > 0)
                    {
                        minSizeImg = imgList[0];
                    }

                    if (minSizeImg != null)
                    {
                        this.DetectWithWzImage(wzFile, minSizeImg);
                    }
                    else if (wzFile.directories.Count > 0)
                    {
                        this.DetectWithAllWzDir(wzFile);
                    }

                    if (wzFile.header.VersionChecked)
                    {
                        this.CalcOffset(wzFile, imgList);
                    }
                }

                return wzFile.header.VersionChecked;
            }
        }

        public class FastVersionVerifier : WzVersionVerifier, IWzVersionVerifier
        {
            public bool Verify(Wz_File wzFile)
            {
                List<Wz_Image> imgList = EnumerableAllWzImage(wzFile.node).Where(_img => _img.WzFile == wzFile).ToList();

                if (wzFile.header.VersionChecked)
                {
                    this.CalcOffset(wzFile, imgList);
                }
                else
                {
                    if (imgList.Count > 0)
                    {
                        this.FastDetectWithAllWzImages(wzFile, imgList);
                    }
                    else if (wzFile.directories.Count > 0)
                    {
                        this.DetectWithAllWzDir(wzFile);
                    }

                    if (wzFile.header.VersionChecked)
                    {
                        this.CalcOffset(wzFile, imgList);
                    }
                }

                return wzFile.header.VersionChecked;
            }
        }
    }

    public enum WzVersionVerifyMode
    {
        Default = 0,
        Fast = 1,
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib.Utilities;
using WzComparerR2.WzLib.Compatibility;
using static WzComparerR2.WzLib.Utilities.MathHelper;

namespace WzComparerR2.WzLib
{
    public class Wz_File : IMapleStoryFile, IDisposable
    {
        public Wz_File(string fileName, Wz_Structure wz)
        {
            this.imageCount = 0;
            this.wzStructure = wz;
            this.fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this.loaded = this.GetHeader(fileName);
        }

        private FileStream fileStream;
        private Wz_Structure wzStructure;
        private Wz_Header header;
        private Wz_Node node;
        private int imageCount;
        private bool loaded;
        private bool isSubDir;
        private Wz_Type type;
        private List<Wz_File> mergedWzFiles;
        private Wz_File ownerWzFile;

        /// <summary>
        /// The offset calculator assigned during version detection, used for dir tree reading and image offset calculation.
        /// </summary>
        internal IWzImageOffsetCalc OffsetCalc { get; set; }

        public Encoding TextEncoding { get; set; }

        public object ReadLock => this.fileStream;

        public FileStream FileStream
        {
            get { return fileStream; }
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
            var br = new WzBinaryReader(this.fileStream, false);

            long filesize = this.FileStream.Length;
            if (filesize < 4) { goto __failed; }

            string signature = new string(br.ReadChars(4));
            if (signature != Wz_Header.PKG1 && signature != Wz_Header.PKG2) { goto __failed; }

            long dataSize = br.ReadInt64();
            int headerSize = br.ReadInt32();
            string copyright = new string(br.ReadChars(headerSize - (int)this.FileStream.Position));

            if (signature == Wz_Header.PKG1)
            {
                // encver detecting:
                // Since KMST1132, wz removed the 2 bytes encver, and use a fixed wzver '777'.
                // Here we try to read the first 2 bytes from data part and guess if it looks like an encver.
                bool encverMissing = false;
                int encver = -1;
                if (dataSize >= 2)
                {
                    this.fileStream.Position = headerSize;
                    encver = br.ReadUInt16();
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
                            int propCount = br.ReadCompressedInt32();
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
                this.Header = new Wz_Header.WzPkg1Header(signature, copyright, fileName, headerSize, dataSize, filesize, dataStartPos, encverMissing, encver);
            }
            else if (signature == Wz_Header.PKG2)
            {
                uint hash1 = br.ReadUInt32();
                uint hash2 = br.ReadUInt32();
                int dataStartPos = (int)this.fileStream.Position;
                this.Header = new Wz_Header.WzPkg2Header(signature, copyright, fileName, headerSize, dataSize, filesize, dataStartPos, hash1, hash2);
            }
            else
            {
                goto __failed;
            }

            return true;

        __failed:
            this.header = new Wz_Header(null, null, fileName, 0, 0, filesize, 0);
            return false;
        }

        public void GetDirTree(Wz_Node parent, bool useBaseWz = false, bool loadWzAsFolder = false)
        {
            var ps = new PartialStream(this.FileStream, this.header.DirStartPosition, this.fileStream.Length - this.header.DirStartPosition, true);
            ps.Position = 0;
            var reader = new WzBinaryReader(ps, false);
            this.GetDirTree(reader, parent, useBaseWz, loadWzAsFolder);
        }

        private void GetDirTree(WzBinaryReader reader, Wz_Node parent, bool useBaseWz = false, bool loadWzAsFolder = false)
        {
            List<Wz_Directory> dirs = new List<Wz_Directory>();

            if (this.header.IsPkg1)
            {
                this.ReadDirTree(reader, parent, ref dirs);
            }
            else if (this.header.IsPkg2)
            {
                this.ReadDirTreePkg2(reader, parent, ref dirs);
            }
            else
            {
                throw new Exception($"Unknown signature: {this.header.Signature}");
            }

            int dirCount = dirs.Count;
            bool willLoadBaseWz = useBaseWz ? parent.Text.Equals("base.wz", StringComparison.OrdinalIgnoreCase) : false;

            var baseFolder = Path.GetDirectoryName(this.header.FileName);

            if (willLoadBaseWz && this.WzStructure.AutoDetectExtFiles)
            {
                for (int i = 0; i < dirCount; i++)
                {
                    //检测文件名
                    var m = Regex.Match(dirs[i].Name, @"^([A-Za-z]+)$");
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
                                if (!dirs.Take(dirCount).Any(dir => extDirName.Equals(dir.Name, StringComparison.OrdinalIgnoreCase)))
                                {
                                    dirs.Add(new Wz_Directory(extDirName, 0, 0, 0, 0, this));
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
                                if (!dirs.Take(dirCount).Any(dir => extDirName.Equals(dir.Name, StringComparison.OrdinalIgnoreCase)))
                                {
                                    dirs.Add(new Wz_Directory(extDirName, 0, 0, 0, 0, this));
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
                string dir = dirs[i].Name;
                Wz_Node t = parent.Nodes.Add(dir);
                if (i < dirCount)
                {
                    this.GetDirTree(reader, t, false);
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

        private void ReadDirTree(WzBinaryReader reader, Wz_Node parent, ref List<Wz_Directory> dirs)
        {
            var cryptoKey = this.WzStructure.encryption.Pkg1Keys;
            int count = reader.ReadCompressedInt32();

            for (int i = 0; i < count; i++)
            {
                byte nodeType = reader.ReadByte();
                string name;
                switch (nodeType)
                {
                    case 0x02:
                        int stringOffAdd = this.Header.HasCapabilities(Wz_Capabilities.EncverMissing) ? 2 : -1;
                        name = reader.ReadStringAt(reader.ReadInt32() + stringOffAdd, cryptoKey);
                        break;
                    case 0x04:
                    case 0x03:
                        name = reader.ReadString(cryptoKey);
                        break;
                    default:
                        throw new Exception($"Unknown type {nodeType} in WzDirTree.");
                }

                int size = reader.ReadCompressedInt32();
                int cs32 = reader.ReadCompressedInt32();
                uint pos = (uint)this.fileStream.Position;
                uint hashOffset = reader.ReadUInt32();

                switch (nodeType)
                {
                    case 0x02:
                    case 0x04:
                        Wz_Image img = new Wz_Image(name, size, cs32, hashOffset, pos, this);
                        if (this.OffsetCalc != null)
                            img.Offset = this.OffsetCalc.CalcOffset(pos, hashOffset);
                        Wz_Node childNode = parent.Nodes.Add(name);
                        childNode.Value = img;
                        img.OwnerNode = childNode;
                        this.imageCount++;
                        break;

                    case 0x03:
                        var dir = new Wz_Directory(name, size, cs32, hashOffset, pos, this);
                        if (this.OffsetCalc != null)
                            dir.Offset = this.OffsetCalc.CalcOffset(pos, hashOffset);
                        dirs.Add(dir);
                        break;
                }
            }
        }

        private void ReadDirTreePkg2(WzBinaryReader reader, Wz_Node parent, ref List<Wz_Directory> dirs)
        {
            var dirReader = ((Wz_Header.WzPkg2Header)this.Header).DirStringReader;
            var pkg2Calc = this.OffsetCalc as IPkg2ImageOffsetCalc;
            int encryptedEntryCount = reader.ReadCompressedInt32();
            int entryCount = pkg2Calc?.DecryptEntryCount(encryptedEntryCount) ?? 0;

            List<Pkg2DirEntry> entries = new();
            for (int i = 0; i < entryCount; i++)
            {
                byte nodeType = reader.ReadByte();
                string name;
                if (nodeType == 0x03 || nodeType == 0x04)
                {
                    name = dirReader.ReadName(reader, entries.Count == 0);
                }
                else
                {
                    throw new Exception($"Unknown type {nodeType} in WzDirTree.");
                }

                int size = reader.ReadCompressedInt32();
                int cs32 = reader.ReadCompressedInt32();
                entries.Add(new Pkg2DirEntry
                {
                    NodeType = nodeType,
                    Name = name,
                    DataLength = size,
                    Checksum = cs32
                });
            }

            int encryptedOffsetCount = reader.ReadCompressedInt32();
            if (encryptedOffsetCount == encryptedEntryCount && entries.Count > 0)
            {
                Span<Pkg2DirEntry> list = CollectionsMarshal.AsSpan(entries);
                for (int i = 0; i < list.Length; i++)
                {
                    uint pos = (uint)this.fileStream.Position;
                    uint hashOffset = reader.ReadUInt32();
                    ref Pkg2DirEntry entry = ref list[i];
                    switch (entry.NodeType)
                    {
                        case 0x04:
                            Wz_Image img = new Wz_Image(entry.Name, entry.DataLength, entry.Checksum, hashOffset, pos, this);
                            if (this.OffsetCalc != null)
                                img.Offset = this.OffsetCalc.CalcOffset(pos, hashOffset);
                            Wz_Node childNode = parent.Nodes.Add(entry.Name);
                            childNode.Value = img;
                            img.OwnerNode = childNode;
                            this.imageCount++;
                            break;

                        case 0x03:
                            var dir = new Wz_Directory(entry.Name, entry.DataLength, entry.Checksum, hashOffset, pos, this);
                            if (this.OffsetCalc != null)
                                dir.Offset = this.OffsetCalc.CalcOffset(pos, hashOffset);
                            dirs.Add(dir);
                            break;
                    }
                }
            }
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


        private struct Pkg2DirEntry
        {
            public int NodeType;
            public string Name;
            public int DataLength;
            public int Checksum;
        }
    }

}

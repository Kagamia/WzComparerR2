using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.IO.Compression;
using WzComparerR2.Patcher.Builder;

namespace WzComparerR2.Patcher
{
    public class WzPatcher : IDisposable
    {
        public WzPatcher(string fileName)
        {
            this.patchFile = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read,
                0x4000, FileOptions.Asynchronous | FileOptions.RandomAccess);
        }

        private FileStream patchFile;
        private PartialStream patchBlock;
        private InflateStream inflateStream;

        private string noticeText;
        private List<PatchPartContext> patchParts;

        public List<PatchPartContext> PatchParts
        {
            get { return patchParts; }
        }

        public string NoticeText
        {
            get { return noticeText; }
        }

        public event EventHandler<PatchingEventArgs> PatchingStateChanged;


        /// <summary>
        /// 验证并初始化补丁解压流。
        /// </summary>
        public void OpenDecompress()
        {
            this.patchBlock = TrySplit(this.patchFile);

            if (this.patchBlock == null)
            {
                throw new Exception("Decompress Error, cannot find patch block from the stream.");
            }

            BinaryReader r = new BinaryReader(patchBlock);
            patchBlock.Seek(8, SeekOrigin.Begin);
            int ver = r.ReadInt32();
            uint checkSum0 = r.ReadUInt32();
            uint checkSum1 = CheckSum.ComputeHash(patchBlock, patchBlock.Length - 0x10);
            VerifyCheckSum(checkSum0, checkSum1, "PatchFile", "0");

            patchBlock.Seek(16, SeekOrigin.Begin);
            byte lb = r.ReadByte(), hb = r.ReadByte();
            if (!(lb == 0x78 && (lb * 0x100 + hb) % 31 == 0)) // zlib头标识 没有就把这两字节都当数据段好了..
            {
                patchBlock.Seek(-2, SeekOrigin.Current);
            }
            this.inflateStream = new InflateStream(patchBlock);
        }

        private PartialStream TrySplit(Stream metaStream)
        {
            metaStream.Seek(0, SeekOrigin.Begin);
            BinaryReader r = new BinaryReader(metaStream);
            PartialStream patchBlock;

            if (r.ReadUInt16() == 0x5a4d)//"MZ"
            {
                metaStream.Seek(-4, SeekOrigin.End);
                ulong check = r.ReadUInt32();
                if (check == 0xf2f7fbf3)
                {
                    metaStream.Seek(-12, SeekOrigin.End);
                    long patchBlockLength = r.ReadUInt32();
                    long noticeLength = r.ReadUInt32();
                    metaStream.Seek(-12 - noticeLength - patchBlockLength, SeekOrigin.End);
                    patchBlock = new PartialStream(metaStream, metaStream.Position, patchBlockLength);
                    metaStream.Seek(patchBlockLength, SeekOrigin.Current);
                    noticeText = Encoding.Default.GetString(r.ReadBytes((int)noticeLength));
                }
                else //兼容TMS的patch.exe
                {
                    metaStream.Seek(-8, SeekOrigin.End);
                    check = r.ReadUInt64();
                    if (check == 0xf2f7fbf3)
                    {
                        metaStream.Seek(-24, SeekOrigin.End);
                        long patchBlockLength = r.ReadInt64();
                        long noticeLength = r.ReadInt64();
                        metaStream.Seek(-24 - noticeLength - patchBlockLength, SeekOrigin.End);
                        patchBlock = new PartialStream(metaStream, metaStream.Position, patchBlockLength);
                        metaStream.Seek(patchBlockLength, SeekOrigin.Current);
                        noticeText = Encoding.Default.GetString(r.ReadBytes((int)noticeLength));
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                patchBlock = new PartialStream(metaStream, 0, metaStream.Length);
            }

            if (patchBlock != null)
            {
                patchBlock.Seek(0, SeekOrigin.Begin);
                r = new BinaryReader(patchBlock);
                if (Encoding.ASCII.GetString(r.ReadBytes(8)) == "WzPatch\x1A")
                {
                    patchBlock.Seek(0, SeekOrigin.Begin);
                }
                else
                {
                    return null;
                }
            }

            return patchBlock;
        }

        public long PrePatch()
        {
            if (this.inflateStream == null)
            {
                this.OpenDecompress();
            }
            else if (this.inflateStream.Position > 0) //重置到初始化
            {
                this.inflateStream = new InflateStream(this.inflateStream);
            }

            List<PatchPartContext> patchParts = new List<PatchPartContext>();
            BinaryReader r = new BinaryReader(this.inflateStream);
            while (true)
            {
                PatchPartContext part = ReadPatchPart(r);

                if (part == null)
                {
                    break;
                }

                patchParts.Add(part);

                //跳过当前段
                switch (part.Type)
                {
                    case 0:
                        if (part.NewFileLength > 0)
                        {
                            this.inflateStream.Seek(part.NewFileLength, SeekOrigin.Current);
                        }
                        break;

                    case 1:
                        {
                            part.NewFileLength = CalcNewFileLength(part, r);
                        }
                        break;

                    case 2:
                        break;
                }
            }

            this.patchParts = patchParts;
            return this.inflateStream.Position;
        }

        private PatchPartContext ReadPatchPart(BinaryReader r)
        {
            string fileName;
            int patchType = GetFileName(r, out fileName);
            PatchPartContext part;

            switch (patchType)
            {
                case 0:
                    if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
                    {
                        part = new PatchPartContext(fileName, -1, 0);
                    }
                    else
                    {
                        int fileLength = r.ReadInt32();
                        uint checkSum0 = r.ReadUInt32();
                        part = new PatchPartContext(fileName, this.inflateStream.Position, patchType);
                        part.NewChecksum = checkSum0;
                        part.NewFileLength = fileLength;
                    }
                    break;

                case 1:
                    {
                        uint oldCheckSum0 = r.ReadUInt32();
                        uint newCheckSum0 = r.ReadUInt32();
                        part = new PatchPartContext(fileName, this.inflateStream.Position, patchType);
                        part.OldChecksum = oldCheckSum0;
                        part.NewChecksum = newCheckSum0;
                    }
                    break;

                case 2:
                    {
                        part = new PatchPartContext(fileName, -1, patchType);
                    }
                    break;

                case -1:
                    return null;

                default:
                    throw new Exception("Unknown patch type " + patchType + ".");
            }
            return part;
        }

        /// <summary>
        /// 对于已经解压的patch文件，向客户端执行更新过程。
        /// </summary>
        /// <param Name="mapleStoryFolder">冒险岛客户端所在文件夹。</param>
        public void Patch(string mapleStoryFolder)
        {
            this.Patch(mapleStoryFolder, mapleStoryFolder);
        }

        /// <summary>
        /// 对于已经解压的patch文件，向客户端执行更新过程，可以自己指定临时文件的文件夹。
        /// </summary>
        /// <param Name="mapleStoryFolder">冒险岛客户端所在文件夹。</param>
        /// <param Name="tempFileFolder">生成临时文件的文件夹。</param>
        public void Patch(string mapleStoryFolder, string tempFileFolder)
        {
            string tempDir = CreateRandomDir(tempFileFolder);

            if (this.inflateStream.Position > 0) //重置到初始化
            {
                this.inflateStream = new InflateStream(this.inflateStream);
            }

            if (this.patchParts == null) //边读取边执行
            {
                this.patchParts = new List<PatchPartContext>();
                BinaryReader r = new BinaryReader(this.inflateStream);
                while (true)
                {
                    PatchPartContext part = ReadPatchPart(r);

                    if (part == null)
                    {
                        break;
                    }

                    patchParts.Add(part);

                    //跳过当前段
                    switch (part.Type)
                    {
                        case 0:
                            CreateNewFile(part, tempDir);
                            break;
                        case 1:
                            RebuildFile(part, tempDir, mapleStoryFolder);
                            break;
                        case 2:
                            break;
                    }
                }
            }
            else  //按照调整后顺序执行
            {
                foreach (PatchPartContext part in this.patchParts)
                {
                    switch (part.Type)
                    {
                        case 0:
                            CreateNewFile(part, tempDir);
                            break;
                        case 1:
                            RebuildFile(part, tempDir, mapleStoryFolder);
                            break;
                        case 2:
                            break;
                    }
                }
            }

            foreach (PatchPartContext part in this.patchParts)
            {
                if (part.Type != 2 && !string.IsNullOrEmpty(part.TempFilePath))
                {
                    SafeMove(part.TempFilePath, Path.Combine(mapleStoryFolder, part.FileName));
                }
                else if (part.Type == 2)
                {
                    if (part.FileName.EndsWith("\\"))
                        SafeDeleteDirectory(Path.Combine(mapleStoryFolder, part.FileName));
                    else
                        SafeDeleteFile(Path.Combine(mapleStoryFolder, part.FileName));
                }
            }

            SafeDeleteDirectory(tempDir);
        }

        private string CreateRandomDir(string folder)
        {
            string randomDir = null;
            do
            {
                randomDir = Path.Combine(folder, Path.GetRandomFileName());
            }
            while (Directory.Exists(randomDir));

            Directory.CreateDirectory(randomDir);
            return randomDir;
        }

        public void SafeMove(string srcFile, string dstFile)
        {
            if (!File.Exists(srcFile))
                return;

            DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(dstFile));
            if (!dir.Exists)
                dir.Create();

            SafeDeleteFile(dstFile);

            File.Move(srcFile, dstFile);
        }

        private static void SafeDeleteFile(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            if (fi.Exists)
            {
                if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                {
                    fi.Attributes = fi.Attributes & (~FileAttributes.ReadOnly);
                }
                fi.Delete();
            }
        }

        private static void SafeDeleteDirectory(string dirName)
        {
            DirectoryInfo di = new DirectoryInfo(dirName);
            if (di.Exists)
            {
                if ((di.Attributes & FileAttributes.ReadOnly) != 0)
                {
                    di.Attributes = di.Attributes & (~FileAttributes.ReadOnly);
                }

                foreach (var f in di.GetFileSystemInfos())
                {
                    if ((f.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        f.Attributes = f.Attributes & (~FileAttributes.ReadOnly);
                    }
                }

                di.Delete(true);
            }
        }


        private int GetFileName(BinaryReader reader, out string fileName)
        {
            int switchByte = 0;
            StringBuilder sb = new StringBuilder();

            while ((switchByte = reader.BaseStream.ReadByte()) > 2)
            {
                sb.Append((char)switchByte);
            }

            fileName = sb.ToString();
            return switchByte;
        }

        public void CreateNewFile(PatchPartContext part, string tempDir)
        {
            this.OnPatchStart(part);
            string tempFileName = Path.Combine(tempDir, part.FileName);
            EnsureDirExists(tempFileName);

            if (part.NewFileLength <= 0)
                return;

            this.InflateStreamSeek(part.Offset);
            FileStream tempFileStream = new FileStream(tempFileName, FileMode.Create, FileAccess.ReadWrite);
            part.TempFilePath = tempFileName;
            this.OnTempFileCreated(part);
            //创建文件同时计算checksum
            uint checkSum1 = StreamUtils.MoveStreamWithCrc32(this.inflateStream, tempFileStream, part.NewFileLength, 0U);
            tempFileStream.Flush();

            this.OnVerifyNewChecksumBegin(part);
            VerifyCheckSum(part.NewChecksum, checkSum1, part.FileName, "0");
            this.OnVerifyNewChecksumEnd(part);

            tempFileStream.Close();
            this.OnTempFileClosed(part);
        }

        public void RebuildFile(PatchPartContext part, string tempDir, string msDir)
        {
            this.OnPatchStart(part);
            string tempFileName = Path.Combine(tempDir, part.FileName);
            EnsureDirExists(tempFileName);

            part.OldFilePath = Path.Combine(msDir, part.FileName);

            this.OnVerifyOldChecksumBegin(part);
            FileStream oldWzFile = new FileStream(part.OldFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                0x4000, FileOptions.Asynchronous | FileOptions.RandomAccess);
            uint oldCheckSum1 = CheckSum.ComputeHash(oldWzFile, (int)oldWzFile.Length); //旧版本文件实际hash
            this.OnVerifyOldChecksumEnd(part);

            try
            {
                VerifyCheckSum(part.OldChecksum, oldCheckSum1, part.FileName, "origin");
            }
            catch
            {
                if (oldWzFile.Length == part.NewFileLength && oldCheckSum1 == part.NewChecksum) //文件已更新的场合
                {
                    oldWzFile.Close();
                    return;
                }
                else
                {
                    throw;
                }
            }

            int cmd;
            //int blockLength;
            FileStream tempFileStream = new FileStream(tempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 0x4000);
            part.TempFilePath = tempFileName;
            if (part.NewFileLength > 0) //预申请硬盘空间 似乎可以加快读写速度
            {
                tempFileStream.SetLength(part.NewFileLength);
                tempFileStream.Seek(0, SeekOrigin.Begin);
            }
            this.OnTempFileCreated(part);
            uint newCheckSum1 = 0;

            this.InflateStreamSeek(part.Offset);
            BinaryReader r = new BinaryReader(this.inflateStream);

            double patchProc = 0;
            const double patchProcReportInverval = 0.005;

            //v3新增读缓冲
            List<RebuildFileOperation> operList = new List<RebuildFileOperation>(32768);
            List<RebuildFileOperation> readFileOperList = new List<RebuildFileOperation>(operList.Capacity);
            MemoryStream msBuffer = new MemoryStream(1024 * 1024 * 64);
            int preLoadByteCount = 0;
            while (true)
            {
                cmd = r.ReadInt32();
                RebuildFileOperation op = null;
                if (cmd != 0)
                {
                    switch ((uint)cmd >> 0x1C)
                    {
                        case 0x08:
                            op = new RebuildFileOperation(0);
                            op.Length = cmd & 0x0fffffff;
                            break;
                        case 0x0c:
                            op = new RebuildFileOperation(1);
                            op.FillByte = (byte)(cmd & 0xff);
                            op.Length = (cmd & 0x0fffff00) >> 8;
                            break;
                        default:
                            op = new RebuildFileOperation(2);
                            op.Length = cmd;
                            op.StartPosition = r.ReadInt32();
                            break;
                    }
                }

                //如果大于 先处理当前所有预读操作
                if (cmd == 0 || (operList.Count >= operList.Capacity - 1)
                    || (op.OperType != 1 && (op.Length + preLoadByteCount > msBuffer.Capacity)))
                {
                    //排序预读原文件
                    readFileOperList.Sort((first, second) => first.StartPosition.CompareTo(second.StartPosition));
                    foreach (var readFileOp in readFileOperList)
                    {
                        int position = (int)msBuffer.Position;
                        readFileOp.Flush(oldWzFile, null, null, msBuffer);
                        readFileOp.bufferStartIndex = position;
                    }

                    //向新文件输出
                    foreach (var tempOp in operList)
                    {
                        newCheckSum1 = tempOp.Flush(oldWzFile, r.BaseStream, msBuffer, tempFileStream, newCheckSum1);

                        //计算更新进度
                        if (part.NewFileLength > 0)
                        {
                            double curProc = 1.0 * tempFileStream.Position / part.NewFileLength;
                            if (curProc - patchProc >= patchProcReportInverval)// || curProc >= 1 - patchProcReportInverval)
                            {
                                this.OnTempFileUpdated(part, tempFileStream.Position);//更新进度改变
                                patchProc = curProc;
                            }
                        }
                        else
                        {
                            if (tempFileStream.Position - patchProc > 1024 * 1024 * 10)
                            {
                                this.OnTempFileUpdated(part, tempFileStream.Position);//更新进度改变
                                patchProc = tempFileStream.Position;
                            }
                        }
                    }

                    //重置缓冲区
                    msBuffer.SetLength(0);
                    preLoadByteCount = 0;
                    operList.Clear();
                    readFileOperList.Clear();
                    if (cmd == 0) // 更新结束 这里是出口无误
                    {
                        break;
                    }
                }

                if (op.OperType != 1 && op.Length >= msBuffer.Capacity) //还是大于的话 单独执行
                {
                    newCheckSum1 = op.Flush(oldWzFile, r.BaseStream, null, tempFileStream, newCheckSum1);
                }
                else //直接放进缓冲区里
                {
                    op.Index = (ushort)operList.Count;
                    operList.Add(op);
                    switch (op.OperType)
                    {
                        case 0:
                            int position = (int)msBuffer.Position;
                            op.Flush(null, r.BaseStream, null, msBuffer);
                            op.bufferStartIndex = position;
                            break;

                        case 1:
                            continue;

                        case 2:
                            readFileOperList.Add(op);
                            break;
                    }
                    preLoadByteCount += op.Length;
                }

            }
            msBuffer.Dispose();
            msBuffer = null;
            tempFileStream.Flush();
            tempFileStream.SetLength(tempFileStream.Position);  //设置文件大小为当前长度

            this.OnVerifyNewChecksumBegin(part);
            tempFileStream.Seek(0, SeekOrigin.Begin);
            //uint _newCheckSum1 = CheckSum.ComputeHash(tempFileStream, (int)tempFileStream.Length); //新生成文件的hash
            VerifyCheckSum(part.NewChecksum, newCheckSum1, part.FileName, "new");
            this.OnVerifyNewChecksumEnd(part);

            oldWzFile.Close();

            tempFileStream.Flush();
            tempFileStream.Close();
            this.OnTempFileClosed(part);
        }

        private class RebuildFileOperation
        {
            public RebuildFileOperation(byte operType)
            {
                this.OperType = operType;
                this.bufferStartIndex = -1;
            }
            public byte OperType; //0-从补丁文件复制  1-填充字节  2-从原文件复制
            public byte FillByte; //只有oper1时可用
            public ushort Index; //操作索引
            public int StartPosition; //只有oper2时可用 原文件起始坐标
            public int Length; //输出区块长度
            public int bufferStartIndex; //输出缓冲流的起始索引 执行后才有值

            public void Flush(Stream oldStream, Stream patchFileStream, Stream bufferStream, Stream newStream)
            {
                this.Flush(oldStream, patchFileStream, bufferStream, newStream, false, 0U);
            }

            public uint Flush(Stream oldStream, Stream patchFileStream, Stream bufferStream, Stream newStream, uint crc)
            {
                return this.Flush(oldStream, patchFileStream, bufferStream, newStream, true, crc);
            }

            private uint Flush(Stream oldStream, Stream patchFileStream, Stream bufferStream, Stream newStream, bool withCrc, uint crc)
            {
                Stream srcStream = null;
                if (this.bufferStartIndex > -1) //使用缓冲流
                {
                    srcStream = bufferStream;
                    srcStream.Seek(this.bufferStartIndex, SeekOrigin.Begin);
                }
                else //使用原始流
                {
                    switch (this.OperType)
                    {
                        case 0:
                            srcStream = patchFileStream;
                            break;

                        case 2:
                            srcStream = oldStream;
                            srcStream.Seek(this.StartPosition, SeekOrigin.Begin);
                            break;
                    }
                }

                //执行更新
                switch (this.OperType)
                {
                    case 0:
                    case 2:
                        if (withCrc)
                        {
                            crc = StreamUtils.MoveStreamWithCrc32(srcStream, newStream, this.Length, crc);
                        }
                        else
                        {
                            StreamUtils.CopyStream(srcStream, newStream, this.Length);
                        }
                        break;
                    case 1:
                        if (withCrc)
                        {
                            crc = StreamUtils.FillStreamWithCrc32(newStream, this.Length, this.FillByte, crc);
                        }
                        else
                        {
                            StreamUtils.FillStream(newStream, this.Length, this.FillByte);
                        }
                        break;
                }
                return crc;
            }
        }

        private int CalcNewFileLength(PatchPartContext patchPart, BinaryReader reader)
        {
            patchPart.Action0 = patchPart.Action1 = patchPart.Action2 = 0;

            int length = 0;
            int cmd;
            int blockLength;
            while ((cmd = reader.ReadInt32()) != 0)
            {
                switch (((uint)cmd) >> 0x1C)
                {
                    case 0x08:
                        blockLength = cmd & 0x0fffffff;
                        reader.BaseStream.Seek(blockLength, SeekOrigin.Current);
                        patchPart.Action0++;
                        break;

                    case 0x0c:
                        blockLength = (cmd & 0x0fffff00) >> 8;
                        patchPart.Action1++;
                        break;

                    default:
                        blockLength = cmd;
                        reader.BaseStream.Seek(4, SeekOrigin.Current);
                        patchPart.Action2++;
                        break;
                }
                length += blockLength;
            }
            return length;
        }

        private void EnsureDirExists(string fileName)
        {
            bool isDirectory;
            EnsureDirExists(fileName, out isDirectory);
        }

        private void EnsureDirExists(string fileName, out bool isDirectory)
        {
            string ext = Path.GetExtension(fileName);
            string dir;
            if (string.IsNullOrEmpty(ext))
            {
                dir = fileName;
                isDirectory = true;
            }
            else
            {
                dir = Path.GetDirectoryName(fileName);
                isDirectory = false;
            }
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private void VerifyCheckSum(uint checksum0, uint checksum1, string fileName, string reason)
        {
            if (checksum0 != checksum1)
            {
                throw new Exception(string.Format("CheckSum Error on \"{0}\"({1}). (0x{2:x8}, 0x{3:x8})", fileName, reason, checksum0, checksum1));
            }
        }

        private void InflateStreamSeek(long offset)
        {
            try
            {
                this.inflateStream.Seek(offset, SeekOrigin.Begin);
            }
            catch
            {
                this.inflateStream = new InflateStream(this.inflateStream);
                this.inflateStream.Seek(offset, SeekOrigin.Begin);
            }
        }

        #region eventhandler
        protected virtual void OnPatchingStateChanged(PatchingEventArgs e)
        {
            if (this.PatchingStateChanged != null)
            {
                this.PatchingStateChanged(this, e);
            }
        }

        private void OnPatchStart(PatchPartContext part)
        {
            PatchingEventArgs e = new PatchingEventArgs(part, PatchingState.PatchStart);
            OnPatchingStateChanged(e);
        }
        private void OnVerifyOldChecksumBegin(PatchPartContext part)
        {
            PatchingEventArgs e = new PatchingEventArgs(part, PatchingState.VerifyOldChecksumBegin);
            OnPatchingStateChanged(e);
        }
        private void OnVerifyOldChecksumEnd(PatchPartContext part)
        {
            PatchingEventArgs e = new PatchingEventArgs(part, PatchingState.VerifyOldChecksumEnd);
            OnPatchingStateChanged(e);
        }
        private void OnVerifyNewChecksumBegin(PatchPartContext part)
        {
            PatchingEventArgs e = new PatchingEventArgs(part, PatchingState.VerifyNewChecksumBegin);
            OnPatchingStateChanged(e);
        }
        private void OnVerifyNewChecksumEnd(PatchPartContext part)
        {
            PatchingEventArgs e = new PatchingEventArgs(part, PatchingState.VerifyNewChecksumEnd);
            OnPatchingStateChanged(e);
        }
        private void OnTempFileCreated(PatchPartContext part)
        {
            PatchingEventArgs e = new PatchingEventArgs(part, PatchingState.TempFileCreated);
            OnPatchingStateChanged(e);
        }
        private void OnTempFileUpdated(PatchPartContext part, long filelen)
        {
            PatchingEventArgs e = new PatchingEventArgs(part, PatchingState.TempFileBuildProcessChanged, filelen);
            OnPatchingStateChanged(e);
        }
        private void OnTempFileClosed(PatchPartContext part)
        {
            PatchingEventArgs e = new PatchingEventArgs(part, PatchingState.TempFileClosed);
            OnPatchingStateChanged(e);
        }
        #endregion

        ~WzPatcher()
        {
            this.Dispose(false);
        }

        public void Close()
        {
            this.Dispose(true);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.patchFile != null)
                {
                    this.patchFile.Close();
                }
            }

            this.patchFile = null;
            this.patchBlock = null;
            this.inflateStream = null;
        }
    }
}

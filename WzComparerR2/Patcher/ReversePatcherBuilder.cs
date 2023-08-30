using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.Patcher.Builder;
using System.IO;
using System.IO.Compression;

namespace WzComparerR2.Patcher
{
    public class ReversePatcherBuilder
    {
        public ReversePatcherBuilder()
        {
        }

        public string msDir;
        public string outputFileName;
        public string patchFileName;

        public void Build()
        {
            List<FileReversePart> preReverse = new List<FileReversePart>();

            using (FileStream fs = new FileStream(patchFileName, FileMode.Open, FileAccess.Read))
            {
                fs.Position = 18;
                InflateStream stream = new InflateStream(fs, true);
                WzPatcherReader reader = new WzPatcherReader(stream);
                PatchPart filePart;

                PatchPart reversePart;

                while ((filePart = reader.ReadPart()) != null)
                {
                    switch (filePart.Type)
                    {
                        case PatchType.Create:
                            if (filePart.FileLength > 0)
                            {
                                stream.Seek(filePart.FileLength, SeekOrigin.Current);
                                //在原文件夹寻找同名文件
                                string oldFile = Path.Combine(msDir, filePart.FileName);
                                if (File.Exists(oldFile))
                                {
                                    reversePart = new PatchPart() { Type = PatchType.Create };
                                    reversePart.FileName = filePart.FileName;
                                    reversePart.FileLength = (int)new FileInfo(oldFile).Length;
                                    preReverse.Add(new FileReversePart(reversePart));
                                }
                                else
                                {
                                    reversePart = new PatchPart() { Type = PatchType.Delete };
                                    reversePart.FileName = filePart.FileName;
                                    preReverse.Add(new FileReversePart(reversePart));
                                }
                            }
                            break;

                        case PatchType.Rebuild:
                            reversePart = new PatchPart()
                            {
                                Type = PatchType.Rebuild,
                                OldChecksum = filePart.Checksum,
                                Checksum = filePart.OldChecksum,
                                FileName = filePart.FileName
                            };
                            List<FileReverseInst> instList = new List<FileReverseInst>();

                            BuildInstruction inst;
                            int filePos = 0;
                            while ((inst = reader.ReadInst())?.Type != null)
                            {
                                if (inst.Type == BuildType.Ending)
                                {
                                    break;
                                }

                                switch (inst.Type)
                                {
                                    case BuildType.FromPatcher:
                                        stream.Seek(inst.Length, SeekOrigin.Current);

                                        break;

                                    case BuildType.FillBytes:

                                        break;

                                    case BuildType.FromOldFile:
                                        instList.Add(new FileReverseInst() { Inst = inst, NewFilePosition = filePos });
                                        break;
                                }
                                filePos += inst.Length;
                            }

                            preReverse.Add(new FileReversePart(reversePart) { InstList = instList });
                            break;

                        case PatchType.Delete:
                            {
                                string oldFile = Path.Combine(msDir, filePart.FileName);
                                if (File.Exists(oldFile))
                                {
                                    reversePart = new PatchPart() { Type = PatchType.Create };
                                    reversePart.FileName = filePart.FileName;
                                    reversePart.FileLength = (int)new FileInfo(oldFile).Length;
                                    preReverse.Add(new FileReversePart(reversePart));
                                }
                            }

                            break;
                    }
                }//end while
            }//end using

            preReverse.Sort();

            using (FileStream dest = new FileStream(outputFileName, FileMode.Create))
            {
                WzPatcherWriter writer = new WzPatcherWriter(dest);

                writer.Begin();
                foreach (var part in preReverse)
                {
                    string oldFileName = Path.Combine(msDir, part.Part.FileName);
                    switch (part.Part.Type)
                    {
                        case PatchType.Create:
                            //计算hash copy文件
                            using (FileStream oldFs = new FileStream(oldFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                part.Part.FileLength = (int)oldFs.Length;
                                part.Part.Checksum = CheckSum.ComputeHash(oldFs, part.Part.FileLength);
                                oldFs.Position = 0;
                                writer.WritePart(part.Part);
                                writer.WriteContent(oldFs, part.Part.FileLength);
                            }
                            break;

                        case PatchType.Rebuild:
                            writer.WritePart(part.Part);

                            using (FileStream oldFs = new FileStream(oldFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                //计算指令
                                var instList = Work(part.InstList, (int)oldFs.Length);
                                //开始执行
                                foreach (var inst in instList)
                                {
                                    switch (inst.Inst.Type)
                                    {
                                        case BuildType.FromOldFile: //参数反转
                                            inst.Inst.OldFilePosition = inst.NewFilePosition;
                                            writer.WriteInst(inst.Inst);
                                            break;

                                        case BuildType.FromPatcher: //go 待优化
                                            writer.WriteInst(inst.Inst);
                                            oldFs.Position = inst.NewFilePosition;
                                            writer.WriteContent(oldFs, inst.Inst.Length);
                                            break;
                                    }
                                }
                                //结束执行
                                writer.WriteInst(new BuildInstruction(BuildType.Ending));
                            }
                            break;

                        case PatchType.Delete:
                            writer.WritePart(part.Part);
                            break;
                    }
                }
                writer.End();
            }
        }


        private IEnumerable<FileReverseInst> Work(List<FileReverseInst> instList, int oldFileLength)
        {
            //筛选两个文件共同部分
            List<FileReverseInst> temp = new List<FileReverseInst>(instList);
            //排序
            temp.Sort((a, b) =>
            {
                BuildInstruction a1 = a.Inst, b1 = b.Inst;
                int compare = a1.OldFilePosition.CompareTo(b1.OldFilePosition);
                if (compare == 0)
                {
                    compare = -a1.Length.CompareTo(b1.Length);
                }
                return compare;
            });

            //进链表
            LinkedList<FileReverseInst> reverseList = new LinkedList<FileReverseInst>();
            foreach (var reverse in temp)
            {
                if (reverseList.Count <= 0)
                {
                    reverseList.AddFirst(reverse);
                }
                else
                {
                    BuildInstruction prev = reverseList.Last.Value.Inst;
                    BuildInstruction cur = reverse.Inst;

                    if (cur.OldFilePosition <= prev.OldFilePosition) //过滤相等
                    {
                        continue;
                    }
                    if (cur.OldFilePosition < prev.OldFilePosition + prev.Length) //有重叠部分
                    {
                        int newLength = (cur.OldFilePosition + cur.Length) - (prev.OldFilePosition + prev.Length);
                        if (newLength <= 0) //完全包含
                        {
                            continue;
                        }
                        //调整不重叠
                        reverse.NewFilePosition += cur.Length - newLength;
                        cur.OldFilePosition = (cur.OldFilePosition + cur.Length) - newLength;
                        cur.Length = newLength;
                    }
                    reverseList.AddLast(reverse);
                }
            }

            //链表填充
            if (reverseList.Count <= 0)
            {
                //怎么可能呢闹呢新建文件吧
                return null;
            }

            //填充不存在的区块
            int totalLength = 0;
            for (var reverse = reverseList.First; reverse != null; reverse = reverse.Next) //懒得写while
            {
                BuildInstruction prev = null;
                BuildInstruction cur = reverse.Value.Inst;
                if (reverse.Previous == null) //如果是first 构造一个虚指令
                {
                    prev = new BuildInstruction(BuildType.FromOldFile);
                }
                else
                {
                    prev = reverse.Previous.Value.Inst;
                }
                int newLength = cur.OldFilePosition - (prev.OldFilePosition + prev.Length);
                if (newLength > 0) //如果中间缺失 添加一块原区段
                {
                    reverseList.AddBefore(reverse, new FileReverseInst()
                    {
                        Inst = new BuildInstruction(BuildType.FromPatcher)
                        {
                            Length = newLength
                        },
                        NewFilePosition = cur.OldFilePosition - newLength
                    });
                    totalLength += newLength;
                }
                else if (newLength < 0)
                {
                    throw new Exception("?????");
                }

                totalLength += cur.Length;
            }

            //补充尾部区块
            BuildInstruction last = reverseList.Last.Value.Inst;
            if (last.Type == BuildType.FromOldFile)
            {
                int newLength = oldFileLength - (last.OldFilePosition + last.Length);
                if (newLength > 0)
                {
                    reverseList.AddLast(new FileReverseInst()
                    {
                        Inst = new BuildInstruction(BuildType.FromPatcher)
                        {
                            Length = newLength
                        },
                        NewFilePosition = last.OldFilePosition + last.Length
                    });

                    totalLength += newLength;
                }
                else if (newLength < 0)
                {
                    throw new Exception("?????");
                }
            }

            return reverseList;
        }

        private class FileReverseInst
        {
            public int NewFilePosition { get; set; }
            public BuildInstruction Inst { get; set; }
        }

        private class FileReversePart : IComparable<FileReversePart>
        {
            public FileReversePart(PatchPart part)
            {
                this.Part = part;
            }
            public PatchPart Part { get; set; }
            public List<FileReverseInst> InstList { get; set; }

            int IComparable<FileReversePart>.CompareTo(FileReversePart other)
            {
                int comp = ((int)this.Part.Type).CompareTo((int)other.Part.Type);
                if (comp == 0)
                {
                    if (this.Part.Type == PatchType.Create)
                    {
                        if (this.Part.FileLength == 0) return -1;
                        else if (other.Part.FileLength == 0) return 1;
                    }
                    comp = this.Part.FileName.CompareTo(other.Part.FileName);
                }
                return comp;
            }
        }
    }
}

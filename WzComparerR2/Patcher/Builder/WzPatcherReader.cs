using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WzComparerR2.Patcher.Builder
{
    public class WzPatcherReader
    {
        public WzPatcherReader(Stream input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("output");
            }

            this.BaseStream = input;
            this.reader = new BinaryReader(input);
        }

        public Stream BaseStream { get; private set; }

        private BinaryReader reader;

        public PatchPart ReadPart()
        {
            PatchPart part = new PatchPart();
            int switchByte = 0;
            StringBuilder sb = new StringBuilder();

            while ((switchByte = reader.BaseStream.ReadByte()) > 2)
            {
                sb.Append((char)switchByte);
            }

            if (switchByte == -1) //失败
            {
                return null;
            }

            part.Type = (PatchType)switchByte;
            part.FileName = sb.ToString();

            switch (part.Type)
            {
                case PatchType.Create:
                    if (Path.HasExtension(part.FileName))
                    {
                        part.FileLength = reader.ReadInt32();
                        part.Checksum = reader.ReadUInt32();
                    }
                    break;

                case PatchType.Rebuild:
                    part.OldChecksum = reader.ReadUInt32();
                    part.Checksum = reader.ReadUInt32();
                    break;

                case PatchType.Delete:
                    break;
            }

            return part;
        }

        public BuildInstruction ReadInst()
        {
            BuildInstruction inst = new BuildInstruction();
            inst.Length = 0;
            inst.FillByte = 0;
            inst.OldFilePosition = 0;

            uint command = reader.ReadUInt32();

            if (command == 0)
            {
                inst.Type = BuildType.Ending;
            }
            else
            {
                switch (command >> 0x1c)
                {
                    case 0x08:
                        inst.Type = BuildType.FromPatcher;
                        inst.Length = (int)command & 0x0fffffff;
                        break;

                    case 0x0c:
                        inst.Type = BuildType.FillBytes;
                        inst.Length = (int)(command & 0x0fffff00) >> 8;
                        inst.FillByte = (byte)(command & 0xff);
                        break;

                    default:
                        inst.Type = BuildType.FromOldFile;
                        inst.Length = (int)command;
                        inst.OldFilePosition = reader.ReadInt32();
                        break;
                }
            }
            return inst;
        }

        public void ReadContent(Stream destStream, int length)
        {
            StreamUtils.CopyStream(this.BaseStream, destStream, length);
        }

        public int ReadContent(byte[] buffer, int offset, int count)
        {
            return this.BaseStream.Read(buffer, offset, count);
        }

        public int ReadInt32()
        {
            return this.reader.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return this.reader.ReadUInt32();
        }
    }
}

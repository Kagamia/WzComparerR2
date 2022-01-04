using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace WzComparerR2.Patcher.Builder
{
    public class WzPatcherWriter
    {
        public WzPatcherWriter(Stream output)
        {
            if( output == null)
            {
                throw new ArgumentNullException("output");
            }

            this.BaseStream = output;
            this.writer = new BinaryWriter(output, Encoding.ASCII);
        }

        public Stream BaseStream { get; private set; }

        private BinaryWriter writer;
        private DeflateStream deflateStream;
        private BinaryWriter deflateWriter;
        private MemoryTributary tempStream;
        private bool useTempStream;
        private long baseStreamPosition;

        public void Begin()
        {
            this.writer.Write(Encoding.ASCII.GetBytes("WzPatch\x1A"));
            this.writer.Write(2); //version

            if (this.BaseStream.CanSeek && this.BaseStream.CanRead)
            {
                this.writer.Write(0u); //crc 回头计算
                this.useTempStream = false;
                this.deflateStream = new DeflateStream(this.BaseStream, CompressionMode.Compress, true);
                this.baseStreamPosition = this.BaseStream.Position;

                //zlib header
                this.BaseStream.WriteByte(0x78);
                this.BaseStream.WriteByte(0xDA);
            }
            else
            {
                this.useTempStream = true;
                this.tempStream = new MemoryTributary();
                this.deflateStream = new DeflateStream(this.tempStream, CompressionMode.Compress, true);
                this.baseStreamPosition = 0;

                //zlib header
                this.tempStream.WriteByte(0x78);
                this.tempStream.WriteByte(0xDA);
            }

            this.deflateWriter = new BinaryWriter(this.deflateStream, Encoding.ASCII);
        }

        public void WritePart(PatchPart part)
        {
            this.deflateWriter.Write(part.FileName.ToCharArray());
            this.deflateWriter.Write((byte)part.Type); //尾部标识

            switch (part.Type)
            {
                case PatchType.Create:
                    if (Path.HasExtension(part.FileName))
                    {
                        this.deflateWriter.Write(part.FileLength);
                        this.deflateWriter.Write(part.Checksum);
                    }
                    break;

                case PatchType.Rebuild:
                    this.deflateWriter.Write(part.OldChecksum);
                    this.deflateWriter.Write(part.Checksum);
                    break;

                case PatchType.Delete:
                    break;
            }
        }

        public void WriteInst(BuildInstruction inst)
        {
            switch (inst.Type)
            {
                case BuildType.FromPatcher:
                    this.deflateWriter.Write((0x08 << 0x1c) | (inst.Length & 0x0fffffff));
                    break;

                case BuildType.FillBytes:
                    this.deflateWriter.Write((0x0c << 0x1c)
                        | ((inst.Length & 0x000fffff) << 0x08)
                        | (inst.FillByte & 0xff));
                    break;

                case BuildType.FromOldFile:
                    int flag = (inst.Length >> 0x1c);
                    if (flag == 0x08 || flag == 0x0c)
                    {
                        throw new Exception("errrrrrrrrr");
                    }
                    this.deflateWriter.Write(inst.Length);
                    this.deflateWriter.Write(inst.OldFilePosition);
                    break;

                case BuildType.Ending:
                    this.deflateWriter.Write(0);
                    break;
            }
        }

        public void WriteContent(Stream contentStream, int length)
        {
            StreamUtils.CopyStream(contentStream, this.deflateStream, length);
        }

        public void WriteContent(byte[] buffer, int offset, int count)
        {
            MemoryStream ms = new MemoryStream(buffer, offset, count);
            WriteContent(ms, (int)ms.Length);
        }

        public void End()
        {
            this.deflateStream.Flush();
            this.deflateStream.Close();
            this.deflateWriter.Close();

            if (!this.useTempStream)
            {
                long curPos = this.BaseStream.Position;
                this.BaseStream.Seek(this.baseStreamPosition, SeekOrigin.Begin);
                uint crc = CheckSum.ComputeHash(this.BaseStream, curPos - this.baseStreamPosition);
                this.BaseStream.Seek(this.baseStreamPosition - 4, SeekOrigin.Begin);
                this.writer.Write(crc);
                this.BaseStream.Seek(curPos, SeekOrigin.Begin);
            }
            else
            {
                this.tempStream.Seek(0, SeekOrigin.Begin);
                uint crc = CheckSum.ComputeHash(this.tempStream, this.tempStream.Length);
                this.writer.Write(crc);
                this.tempStream.Seek(0, SeekOrigin.Begin);
                StreamUtils.CopyStream(this.tempStream, this.BaseStream, (int)this.tempStream.Length);
                this.tempStream.Close();
                this.tempStream = null;
            }
        }
    }
}

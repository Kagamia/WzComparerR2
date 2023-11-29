using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace WzComparerR2.Patcher.Builder
{
    public class InflateStream : DeflateStream
    {
        public InflateStream(Stream stream)
            : base(stream, CompressionMode.Decompress)
        {
            position = 0;
            baseStartPosition = stream.Position;
        }

        public InflateStream(Stream stream, bool leaveOpen)
            : base(stream, CompressionMode.Decompress, leaveOpen)
        {
            position = 0;
            baseStartPosition = stream.Position;
        }

        public InflateStream(InflateStream oldInflateStream)
            : base(oldInflateStream.BaseStream, CompressionMode.Decompress)
        {
            oldInflateStream.BaseStream.Seek(oldInflateStream.BaseStartPosition, SeekOrigin.Begin);
            position = 0;
            baseStartPosition = oldInflateStream.BaseStream.Position;
        }

        long position;
        long baseStartPosition;

        public override long Position
        {
            get
            {
                return this.position;
            }
            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        public long BaseStartPosition
        {
            get { return baseStartPosition; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin != SeekOrigin.End)
            {
                if (origin == SeekOrigin.Begin)
                {
                    offset -= position;
                }

                if (offset >= 0)
                {
                    this.Skip(offset);
                    return this.position;
                }
            }
            return base.Seek(offset, origin);
        }

#if NET6_0_OR_GREATER
        // In .Net6, DeflateStream overrides ReadByte() method and does not call Read(byte[], int, int).
        // we should override it once more to calculate position.
        public override int ReadByte()
        {
            byte[] buffer = new byte[1];
            if (this.Read(buffer, 0, 1) != 0)
            {
                return buffer[0];
            }
            return -1;
        }
#endif

        public override int Read(byte[] array, int offset, int count)
        {
            int length = base.Read(array, offset, count);
            this.position += length;
            return length;
        }

        private void Skip(long length)
        {
            byte[] buffer = new byte[4096];
            while (length > 0)
            {
                int len = this.Read(buffer, 0, (int)Math.Min(length, buffer.Length));
                if (len == 0)
                {
                    break;
                }
                length -= len;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }
    }
}

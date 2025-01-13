using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace WzComparerR2.Patcher.Builder
{
    public class InflateStream : Stream
    {
        public InflateStream(Stream stream, bool buffered = false, bool leaveOpen = false)
        {
            this.BaseStream = stream;
            this.baseStartPosition = stream.Position;
            this.buffered = buffered;
            this.leaveOpen = leaveOpen;

            this.Reset();
        }

        public Stream BaseStream { get; private set; }

        private readonly long baseStartPosition;
        private readonly bool buffered;
        private readonly bool leaveOpen;
        private Stream deflateStream;
        private long position;

        public override long Position
        {
            get => this.position;
            set => this.Seek(value, SeekOrigin.Begin);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long offsetFromBegin = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => offset + this.position,
                _ => throw new NotSupportedException(),
            };

            if (offsetFromBegin < this.position)
            {
                this.Reset();
                this.Skip(offsetFromBegin);
            }
            else if (offsetFromBegin > this.position)
            {
                this.Skip(offsetFromBegin - this.position);
            }
            return this.position;
        }

#if NET6_0_OR_GREATER
        public override int Read(Span<byte> buffer)
        {
            int length = this.deflateStream.Read(buffer);
            this.position += length;
            return length;
        }
#endif

        public override int Read(byte[] array, int offset, int count)
        {
            int length = this.deflateStream.Read(array, offset, count);
            this.position += length;
            return length;
        }

        public override int ReadByte()
        {
            int b = this.deflateStream.ReadByte();
            if (b > -1)
            {
                this.position += 1;
            }
            return b;
        }

        public override void Flush()
        {
        }

        public override bool CanSeek => true;

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void WriteByte(byte value) => throw new NotSupportedException();

        public void Reset()
        {
            if (this.deflateStream != null && this.position == 0)
            {
                return;
            }

            this.BaseStream.Position = this.baseStartPosition;
            var deflateStream = new DeflateStream(this.BaseStream, CompressionMode.Decompress, true);
            if (buffered)
            {
                this.deflateStream = new BufferedStream(deflateStream);
            }
            else
            {
                this.deflateStream = deflateStream;
            }
            this.position = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.deflateStream?.Dispose();
                if (!this.leaveOpen)
                {
                    this.BaseStream?.Dispose();
                }
            }
        }

        private void Skip(long length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            if (length == 0)
            {
                return;
            }

            var pool = ArrayPool<byte>.Shared;
            byte[] buffer = pool.Rent(4096);
            while (length > 0)
            {
                int len = this.Read(buffer, 0, (int)Math.Min(length, buffer.Length));
                if (len == 0)
                {
                    break;
                }
                length -= len;
            }
            pool.Return(buffer);
        }
    }
}

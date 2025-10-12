using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.WzLib.Utilities
{
    public class ConcatenatedStream : Stream
    {
        private readonly Stream first;
        private readonly Stream second;
        private readonly bool canSeek;
        private readonly bool leaveOpen;
        private long position;
        private Stream current;

        public ConcatenatedStream(Stream first, Stream second, bool leaveOpen = false)
        {
            this.first = first ?? throw new ArgumentNullException(nameof(first));
            this.second = second ?? throw new ArgumentNullException(nameof(second));
            this.canSeek = first.CanSeek && second.CanSeek;
            this.leaveOpen = leaveOpen;
            this.current = first;
            this.position = 0;
        }

        public override bool CanRead => this.first.CanRead && this.second.CanRead;
        public override bool CanSeek => this.canSeek;
        public override bool CanWrite => false;
        public override long Length => this.first.Length + this.second.Length;
        public override long Position
        {
            get
            {
                if (!this.canSeek)
                {
                    throw new NotSupportedException();
                }
                return this.position;
            }
            set
            {
                if (!this.canSeek)
                {
                    throw new NotSupportedException();
                }
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;

            if (this.canSeek)
            {
                if (this.position < this.first.Length)
                {
                    this.first.Position = this.position;
                    int readBytes = this.first.Read(buffer, offset, count);
                    this.position += readBytes;
                    totalRead += readBytes;
                    offset += readBytes;
                    count -= readBytes;
                }

                if (count > 0 && this.position >= this.first.Length)
                {
                    this.second.Position = this.position - this.first.Length;
                    int readBytes = this.second.Read(buffer, offset, count);
                    this.position += readBytes;
                    totalRead += readBytes;
                }
            }
            else
            {
                int readBytes = this.current.Read(buffer, offset, count);
                totalRead += readBytes;
                offset += readBytes;
                count -= readBytes;
                if (count > 0 && this.current == first)
                {
                    this.current = this.second;
                    readBytes = this.current.Read(buffer, offset, count);
                    totalRead += readBytes;
                }
            }

            return totalRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!this.CanSeek)
            {
                throw new NotSupportedException();
            }

            long newPos = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => this.position + offset,
                SeekOrigin.End => this.Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin), "Invalid SeekOrigin"),
            };

            if (newPos < 0 || newPos > this.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Seek position out of bounds");

            this.position = newPos;
            return this.position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.leaveOpen)
                {
                    first.Dispose();
                    second.Dispose();
                }
            }
            this.current = null;
            base.Dispose(disposing);
        }
    }
}

using System;
using System.IO;

namespace WzComparerR2.WzLib.Utilities
{
    public class PartialStream : Stream
    {
        public PartialStream(Stream baseStream, long offset, long length, bool leaveOpen = false)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException("baseStream", "BaseStream cannot be null.");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Offset cannot be negative.");
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Length cannot be negative.");
            }
            this.baseStream = baseStream;
            this.offset = offset;
            this.length = length;
            this.leaveOpen = leaveOpen;
        }

        private Stream baseStream;
        private long offset;
        private long length;
        private bool leaveOpen;

        public Stream BaseStream
        {
            get { return baseStream; }
        }

        public override bool CanRead
        {
            get { return baseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return baseStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return baseStream.CanWrite; }
        }

        public override void Flush()
        {
            baseStream.Flush();
        }

        public override long Length
        {
            get { return this.length; }
        }

        public virtual long Offset
        {
            get { return this.offset; }
        }

        public override long Position
        {
            get
            {
                return baseStream.Position - this.offset;
            }
            set
            {
                baseStream.Position = value + this.offset;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long curPos = this.Position;
            if (curPos < 0)
                return 0;
            long maxCount = this.length - curPos;
            if (maxCount < 0)
                return 0;
            return baseStream.Read(buffer, offset, (int)Math.Min(count, maxCount));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    offset += this.offset;
                    break;
                case SeekOrigin.Current:
                    offset += this.Position;
                    break;
                case SeekOrigin.End:
                    offset += this.offset + this.length;
                    break;
                default:
                    throw new ArgumentException("Unknown SeekOrigin.", "origin");
            }
            if (offset < this.offset)
                throw new IOException("Attempt to seek front of the stream.");
            return baseStream.Seek(offset, SeekOrigin.Begin) - this.offset;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Cannot set the length of PartialStream.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.Position + count > this.length)
                throw new IOException("Cannot write out of bound.");
            baseStream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            this.Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.leaveOpen)
                {
                    baseStream.Dispose();
                }
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (this.Position < 0)
                throw new IOException("Cannot read out of bound.");
            return baseStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (this.Position + count > this.length)
                throw new IOException("Cannot write out of bound.");
            return baseStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return base.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            baseStream.EndWrite(asyncResult);
        }

        public override int ReadByte()
        {
            long curPos = this.Position;
            if (curPos >= 0 && curPos < this.length)
                return baseStream.ReadByte();
            else
                return -1;
        }

        public override void WriteByte(byte value)
        {
            if (this.Position >= 0 && this.Position < this.length)
                baseStream.WriteByte(value);
        }

        public override bool CanTimeout
        {
            get
            {
                return baseStream.CanTimeout;
            }
        }

        public override int ReadTimeout
        {
            get
            {
                return baseStream.ReadTimeout;
            }
            set
            {
                baseStream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return baseStream.WriteTimeout;
            }
            set
            {
                baseStream.WriteTimeout = value;
            }
        }
    }
}

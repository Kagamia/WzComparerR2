using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WzComparerR2.WzLib.Utilities
{
    public class ChunkedEncryptedInputStream : Stream
    {
        private Stream baseStream;
        private IWzDecrypter decrypter;
        private bool leaveOpen;
        private int nextChunkLength;
        private int keyOffset;

        public ChunkedEncryptedInputStream(Stream baseStream, IWzDecrypter decrypter, bool leaveOpen = false)
        {
            this.baseStream = baseStream;
            this.decrypter = decrypter;
            this.leaveOpen = leaveOpen;
            this.nextChunkLength = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException();
            return this.Read(buffer.AsSpan(offset, count));
        }

#if NETFRAMEWORK
        private int Read(Span<byte> buffer)
#elif NET6_0_OR_GREATER                  
        public override int Read(Span<byte> buffer)
#endif
        {
            int bytesRead = 0;

            while (buffer.Length > 0)
            {
                int readLen;
                if (this.nextChunkLength == 0)
                {
                    Span<byte> chunkLen = stackalloc byte[4];
                    readLen = this.baseStream.ReadAvailableBytes(chunkLen);
                    if (readLen == 0)
                    {
                        break;
                    }
                    else if (readLen != chunkLen.Length)
                    {
                        throw new IOException("Failed to read chunk length.");
                    }
                    this.nextChunkLength = MemoryMarshal.Read<int>(chunkLen);
                    if (this.nextChunkLength <= 0)
                    {
                        break;
                    }
                    this.keyOffset = 0;
                }

                readLen = this.baseStream.Read(buffer.Slice(0, Math.Min(buffer.Length, this.nextChunkLength)));
                if (readLen == 0)
                {
                    break;
                }
                this.decrypter?.Decrypt(buffer.Slice(0, readLen), this.keyOffset);

                this.keyOffset += readLen;
                this.nextChunkLength -= readLen;
                buffer = buffer.Slice(readLen);
                bytesRead += readLen;
            }

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.leaveOpen)
                {
                    baseStream?.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WzComparerR2.Network
{
    sealed class RingBufferStream : Stream
    {
        public RingBufferStream()
        {
            this.buffer = new LinkedList<byte[]>();
        }

        private bool isDisposed;
        private long startIndex;
        private long endIndex;
        private long readIndex;

        private readonly LinkedList<byte[]> buffer;
        private const int BlockSize = 4096;

        public override bool CanRead
        {
            get { return !this.isDisposed; }
        }

        public override bool CanSeek
        {
            get { return !this.isDisposed; }
        }

        public override bool CanWrite
        {
            get { return !this.isDisposed; }
        }

        public override long Length
        {
            get { return endIndex - startIndex; }
        }

        public override long Position
        {
            get { return readIndex - startIndex; }
            set { this.Seek(value, SeekOrigin.Begin); }
        }

        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!this.CanRead)
            {
                throw new ObjectDisposedException(nameof(RingBufferStream));
            }

            var readIndex = this.readIndex;
            var availData = this.endIndex - this.readIndex;
            if (availData < 0)
            {
                return 0;
            }

            count = (int)Math.Min(availData, count);
            int total = 0;
            LinkedListNode<byte[]> node;
            int bufferOff;
            GetBlockNode(readIndex, false, out node, out bufferOff);
            while (total < count && node != null)
            {
                var bufferBlock = node.Value;
                int copyLength = Math.Min(count - total, bufferBlock.Length - bufferOff);
                Buffer.BlockCopy(bufferBlock, bufferOff, buffer, offset + total, copyLength);
                total += copyLength;
                bufferOff += copyLength;
                if (bufferOff >= bufferBlock.Length)
                {
                    node = node.Next;
                    bufferOff = 0;
                }
            }

            this.readIndex = readIndex + total;
            return total;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!this.CanSeek)
            {
                throw new ObjectDisposedException(nameof(RingBufferStream));
            }

            long pos = 0;
            switch (origin)
            {
                case SeekOrigin.Begin: pos = offset; break;
                case SeekOrigin.Current: pos = this.Position + offset; break;
                case SeekOrigin.End: pos = this.Length + offset; break;
            }

            if (pos < 0)
            {
                throw new ArgumentException("pos can't less than zero.");
            }

            this.readIndex = this.startIndex + pos;
            return this.Position;
        }

        public override void SetLength(long value)
        {
            if (!this.CanWrite)
            {
                throw new ObjectDisposedException(nameof(RingBufferStream));
            }

            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!this.CanWrite)
            {
                throw new ObjectDisposedException(nameof(RingBufferStream));
            }

            throw new NotImplementedException();
        }

        public void Append(byte[] buffer, int offset, int count)
        {
            LinkedListNode<byte[]> node;
            int bufferOff;
            GetBlockNode(this.endIndex, true, out node, out bufferOff);
            int total = 0;
            while (total < count)
            {
                if (node == null)
                {
                    node = this.buffer.AddLast(new byte[BlockSize]);
                    bufferOff = 0;
                }
                var bufferBlock = node.Value;
                int copyLength = Math.Min(count - total, bufferBlock.Length - bufferOff);
                Buffer.BlockCopy(buffer, offset + total, bufferBlock, bufferOff, copyLength);
                total += copyLength;
                bufferOff += copyLength;
                if (bufferOff >= bufferBlock.Length)
                {
                    node = node.Next;
                    bufferOff = 0;
                }
            }
            this.endIndex += total;
        }

        public void ClearPrevious()
        {
            if (!this.CanWrite)
            {
                throw new ObjectDisposedException(nameof(RingBufferStream));
            }

            var readIndex = this.readIndex;
            LinkedListNode<byte[]> node, prev;
            int bufferOff;
            GetBlockNode(readIndex, false, out node, out bufferOff);
            if (node != null)
            {
                while ((prev = node.Previous) != null)
                {
                    this.buffer.Remove(prev);
                    this.startIndex -= prev.Value.Length;
                    this.endIndex -= prev.Value.Length;
                    this.readIndex -= prev.Value.Length;
                    this.buffer.AddLast(prev);
                }

                this.startIndex = this.readIndex;
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.isDisposed = true;
        }

        private void GetBlockNode(long pos, bool autoExpand, out LinkedListNode<byte[]> node, out int offset)
        {
            int index = (int)(pos / BlockSize);
            offset = (int)(pos % BlockSize);
            node = this.buffer.First;
            for (int i = 0; i < index && node != null; i++)
            {
                node = node.Next;
                if (autoExpand && node == null)
                {
                    node = buffer.AddLast(new byte[BlockSize]);
                }
            }
        }
    }
}

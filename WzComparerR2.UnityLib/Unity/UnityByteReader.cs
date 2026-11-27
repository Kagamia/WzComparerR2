using System;
using System.Text;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// Cursor-style reader with a switchable byte order.
    /// Unity alignment rules: string / byte vector are followed by align(4);
    /// bool fields carrying the align MetaFlag are followed by align(4) as well.
    /// </summary>
    public sealed class UnityByteReader
    {
        private readonly byte[] buffer;
        private readonly int start;
        private readonly int length;
        private int position;

        public UnityByteReader(byte[] buffer, bool littleEndian)
            : this(buffer, 0, buffer.Length, littleEndian)
        {
        }

        public UnityByteReader(byte[] buffer, int start, int length, bool littleEndian)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (start < 0 || length < 0 || start > buffer.Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            this.buffer = buffer;
            this.start = start;
            this.length = length;
            this.position = 0;
            this.IsLittleEndian = littleEndian;
        }

        public bool IsLittleEndian { get; private set; }

        /// <summary>Current position (relative to the start offset given to the constructor).</summary>
        public int Position
        {
            get { return this.position; }
        }

        public int Remaining
        {
            get { return this.length - this.position; }
        }

        public bool EndOfStream
        {
            get { return this.position >= this.length; }
        }

        public void SetLittleEndian(bool littleEndian)
        {
            this.IsLittleEndian = littleEndian;
        }

        public void Skip(int count)
        {
            this.Take(count);
        }

        /// <summary>Aligns to a 4-byte boundary (relative to the start of the buffer).</summary>
        public void Align4()
        {
            int aligned = (this.position + 3) & ~3;
            if (aligned > this.length)
            {
                throw new UnityException(string.Format(
                    "Alignment out of range: {0} bytes required, {1} bytes remaining", aligned - this.position, this.Remaining));
            }
            this.position = aligned;
        }

        public byte[] Take(int count)
        {
            if (count < 0)
            {
                throw new UnityException("The read length must not be negative: " + count);
            }
            if (this.Remaining < count)
            {
                throw new UnityException(string.Format(
                    "Data truncated: {0} bytes required, {1} bytes remaining (at offset {2})",
                    count, this.Remaining, this.position));
            }
            byte[] result = new byte[count];
            Buffer.BlockCopy(this.buffer, this.start + this.position, result, 0, count);
            this.position += count;
            return result;
        }

        public byte U8()
        {
            return this.Take(1)[0];
        }

        public bool Bool()
        {
            return this.U8() != 0;
        }

        public ushort U16()
        {
            byte[] b = this.Take(2);
            return this.IsLittleEndian
                ? (ushort)(b[0] | (b[1] << 8))
                : (ushort)((b[0] << 8) | b[1]);
        }

        public short I16()
        {
            return (short)this.U16();
        }

        public uint U32()
        {
            byte[] b = this.Take(4);
            return this.IsLittleEndian
                ? (uint)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24))
                : (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
        }

        public int I32()
        {
            return unchecked((int)this.U32());
        }

        public ulong U64()
        {
            byte[] b = this.Take(8);
            if (this.IsLittleEndian)
            {
                return (ulong)b[0]
                    | ((ulong)b[1] << 8)
                    | ((ulong)b[2] << 16)
                    | ((ulong)b[3] << 24)
                    | ((ulong)b[4] << 32)
                    | ((ulong)b[5] << 40)
                    | ((ulong)b[6] << 48)
                    | ((ulong)b[7] << 56);
            }
            return ((ulong)b[0] << 56)
                | ((ulong)b[1] << 48)
                | ((ulong)b[2] << 40)
                | ((ulong)b[3] << 32)
                | ((ulong)b[4] << 24)
                | ((ulong)b[5] << 16)
                | ((ulong)b[6] << 8)
                | (ulong)b[7];
        }

        public long I64()
        {
            return unchecked((long)this.U64());
        }

        public float F32()
        {
            byte[] b = this.Take(4);
            if (!this.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return BitConverter.ToSingle(b, 0);
        }

        /// <summary>Reads a null-terminated string.</summary>
        public string CString()
        {
            int end = this.position;
            while (end < this.length && this.buffer[this.start + end] != 0)
            {
                end++;
            }
            if (end >= this.length)
            {
                throw new UnityException(string.Format(
                    "The C string is not terminated (at offset {0})", this.position));
            }
            string value = Encoding.UTF8.GetString(this.buffer, this.start + this.position, end - this.position);
            this.position = end + 1;
            return value;
        }

        /// <summary>Unity aligned string: u32 length + bytes + align(4).</summary>
        public string AlignedString()
        {
            int len = checked((int)this.U32());
            if (len < 0 || len > this.Remaining)
            {
                throw new UnityException(string.Format(
                    "Invalid string length: {0} ({1} remaining)", len, this.Remaining));
            }
            string value = Encoding.UTF8.GetString(this.buffer, this.start + this.position, len);
            this.position += len;
            this.Align4();
            return value;
        }

        /// <summary>Unity byte vector: u32 length + bytes + align(4).</summary>
        public byte[] ByteVector()
        {
            int len = checked((int)this.U32());
            if (len < 0 || len > this.Remaining)
            {
                throw new UnityException(string.Format(
                    "Invalid byte vector length: {0} ({1} remaining)", len, this.Remaining));
            }
            byte[] value = this.Take(len);
            this.Align4();
            return value;
        }
    }
}

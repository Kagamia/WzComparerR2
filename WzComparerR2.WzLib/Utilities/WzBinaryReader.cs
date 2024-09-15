using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;

namespace WzComparerR2.WzLib.Utilities
{
    internal class WzBinaryReader
    {
        public WzBinaryReader(Stream stream, bool useStringPool)
            : this(stream, useStringPool ? new SimpleWzStringPool() : null)
        {
        }

        public WzBinaryReader(Stream stream, IWzStringPool stringPool)
        {
            this.BaseStream = stream;
            this.bReader = new BinaryReader(this.BaseStream, System.Text.Encoding.ASCII, true);
            this.stringPool = stringPool;
        }

        public Stream BaseStream { get; private set; }
        public int StringReferenceOffsetBytes { get; set; }
        private BinaryReader bReader;
        private IWzStringPool stringPool;

        public byte ReadByte()
        {
            return this.bReader.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return this.bReader.ReadSByte();
        }

        public short ReadInt16()
        {
            return this.bReader.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return this.bReader.ReadUInt16();
        }

        public int ReadCompressedInt32()
        {
            int s = this.bReader.ReadSByte();
            return (s == -128) ? this.bReader.ReadInt32() : s;
        }

        public int ReadInt32()
        {
            return this.bReader.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return this.bReader.ReadUInt32();
        }

        public long ReadCompressedInt64()
        {
            int s = this.bReader.ReadSByte();
            return (s == -128) ? this.bReader.ReadInt64() : s;
        }

        public long ReadInt64()
        {
            return this.bReader.ReadInt64();
        }

        public float ReadCompressedSingle()
        {
            float fl = this.bReader.ReadSByte();
            return (fl == -128) ? this.bReader.ReadSingle() : fl;
        }

        public double ReadDouble()
        {
            return this.bReader.ReadDouble();
        }

        public char[] ReadChars(int count)
        {
            return this.bReader.ReadChars(count);
        }

        public string ReadString(IWzDecrypter decrypter)
        {
            long currentPos = this.BaseStream.Position;

            int size = this.ReadSByte();
            if (size < 0) // read ASCII/cp1252 string
            {
                size = (size == -128) ? this.ReadInt32() : -size;

                // for net6+ we can use Stream.Read(Span<byte>) instead, the array buffer is not needed.
                var buffer = ArrayPool<byte>.Shared.Rent(size);
                try
                {
                    this.BaseStream.ReadExactly(buffer, 0, size);
                    decrypter.Decrypt(buffer, 0, size);

                    using var charBuffer = MemoryPool<byte>.Shared.Rent(size * 2);
                    Span<char> chars = MemoryMarshal.Cast<byte, char>(charBuffer.Memory.Span).Slice(0, size);
                    // TODO: SIMD optimization for net6
                    byte mask = 0xAA;
                    for (int i = 0; i < size; i++)
                    {
                        chars[i] = (char)(buffer[i] ^ mask);
                        mask++;
                    }
                    return this.stringPool != null ? this.stringPool.GetOrAdd(currentPos, chars) : chars.ToString();
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else if (size > 0) // read UTF-16LE string
            {
                if (size == 127)
                {
                    size = this.bReader.ReadInt32();
                }

                var buffer = ArrayPool<byte>.Shared.Rent(size * 2);
                try
                {
                    this.BaseStream.ReadExactly(buffer, 0, size * 2);
                    decrypter.Decrypt(buffer, 0, size * 2);

                    Span<char> chars = MemoryMarshal.Cast<byte, char>(buffer).Slice(0, size);
                    // TODO: SIMD optimization for net6
                    ushort mask = 0xAAAA;
                    for (int i = 0; i < size; i++)
                    {
                        chars[i] = (char)(chars[i] ^ mask);
                        mask++;
                    }
                    return this.stringPool != null ? this.stringPool.GetOrAdd(currentPos, chars) : chars.ToString();
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public string ReadImageObjectTypeName(IWzDecrypter decrypter)
        {
            int flag = this.bReader.ReadByte();
            switch (flag)
            {
                case 0x73:
                    return this.ReadString(decrypter);
                case 0x1B:
                    return this.ReadStringAt(this.ReadInt32() + this.StringReferenceOffsetBytes, decrypter);
                default:
                    throw new Exception($"Unexpected flag '{flag}' when reading string at {this.BaseStream.Position}.");
            }
        }

        public string ReadImageString(IWzDecrypter decrypter)
        {
            int flag = this.bReader.ReadByte();
            switch (flag)
            {
                case 0x00:
                    return this.ReadString(decrypter);
                case 0x01:
                    return this.ReadStringAt(this.ReadInt32() + this.StringReferenceOffsetBytes, decrypter);
                case 0x04: 
                    this.SkipBytes(8);
                    return null;
                default:
                    throw new Exception($"Unexpected flag '{flag}' when reading string at {this.BaseStream.Position}.");
            }
        }

        public string ReadStringAt(long offset, IWzDecrypter decrypter)
        {
            if (this.stringPool != null && this.stringPool.TryGet(offset, out string s))
            {
                return s;
            }
            long currentPos = this.BaseStream.Position;
            this.BaseStream.Position = offset;
            s = this.ReadString(decrypter);
            this.BaseStream.Position = currentPos;
            return s;
        }

        public byte[] ReadBytes(int count)
        {
            return this.bReader.ReadBytes(count);
        }

        public void SkipBytes(int count)
        {
            if (this.BaseStream.CanSeek)
            {
                this.BaseStream.Position += count;
            }
            else
            {
                var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(count, 16384));
                try
                {
                    while (count > 0)
                    {
                        int actual = this.BaseStream.Read(buffer, 0, Math.Min(count, buffer.Length));
                        if (actual == 0)
                        {
                            throw new EndOfStreamException();
                        }
                        count -= actual;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
    }
}

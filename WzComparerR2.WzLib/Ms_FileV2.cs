using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using WzComparerR2.WzLib.Cryptography;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib
{
    public class Ms_FileV2 : IMapleStoryFile, IDisposable
    {
        public Ms_FileV2(string fileName, Wz_Structure wzs)
        {
            var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this.Init(fileStream, fileName, wzs, false);
        }

        public Ms_FileV2(Stream baseStream, string originalFileName, Wz_Structure wzs, bool leaveOpen = false)
        {
            this.Init(baseStream, originalFileName, wzs, leaveOpen);
        }

        public Stream BaseStream { get; private set; }
        public Wz_Structure WzStructure { get; private set; }
        public Ms_Header Header { get; private set; }
        public List<Ms_Entry> Entries { get; private set; }

        Stream IMapleStoryFile.FileStream => this.BaseStream;
        object IMapleStoryFile.ReadLock => this.BaseStream;

        private bool leaveOpen;

        private void Init(Stream baseStream, string originalFileName, Wz_Structure wzs, bool leaveOpen)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException(nameof(baseStream));
            }
            if (originalFileName == null)
            {
                throw new ArgumentNullException(nameof(originalFileName));
            }

            this.BaseStream = baseStream;
            this.WzStructure = wzs;
            this.leaveOpen = leaveOpen;
            try
            {
                this.ReadHeader(originalFileName);
            }
            catch
            {
                if (!leaveOpen)
                {
                    baseStream.Dispose();
                }
                throw;
            }
            this.Entries = new List<Ms_Entry>(0);
        }

        private void ReadHeader(string fullFileName)
        {
            string fileName = Path.GetFileName(fullFileName).ToLower();
            this.BaseStream.Position = 0;
            using var bReader = new BinaryReader(this.BaseStream, Encoding.ASCII, true);

            // 1. random bytes
            int randByteCount = fileName.Sum(c => (int)c) % 312 + 30;
            byte[] randBytes = bReader.ReadBytes(randByteCount);
            for (int i = 0; i < randBytes.Length; ++i)
            {
                randBytes[i] = (byte)((sbyte)randBytes[i] >> 1);
            }

            // 2. check file version
            const int supportedVersion = 4;
            var version = bReader.ReadByte() ^ randBytes[0];
            if (version != supportedVersion)
                throw new Exception($"Version check failed. (expected: {supportedVersion}, actual {version})");

            // 3. encrypted chacha20 key
            int hashedSaltLen = bReader.ReadInt32();
            int saltLen = (byte)hashedSaltLen ^ randBytes[0];
            byte[] saltBytes = bReader.ReadBytes(saltLen * 2);
            char[] saltChars = new char[saltLen];
            for (int i = 0; i < saltLen; i++)
            {
                int a = randBytes[i] ^ saltBytes[i * 2];
                int b = ((a | 0x4B) << 1) - a - 75;
                saltChars[i] = (char)b;
            }
            string saltStr = new string(saltChars);

            long headerStartPos = this.BaseStream.Position;

            // 4. decrypt 8 bytes header with chacha20 cipher
            // generate chacha20 key based on filename+keySalt
            string fileNameWithSalt = fileName + saltStr;

            Span<byte> chacha20Key = stackalloc byte[ChaCha20CryptoTransform.AllowedKeyLength];
            for (int i = 0; i < chacha20Key.Length; ++i)
            {
                chacha20Key[i] = (byte)(fileNameWithSalt[i % fileNameWithSalt.Length] + i);
                chacha20Key[i] ^= chacha20KeyObscure[i];
            }

            Span<byte> emptyNonce = stackalloc byte[ChaCha20CryptoTransform.AllowedNonceLength];
            using var chacha20Cipher = new ChaCha20CryptoTransform(chacha20Key, emptyNonce, 0);
            var chacha20DecoderStream = new CryptoStream(this.BaseStream, chacha20Cipher, CryptoStreamMode.Read);
            var chacha20Reader = new BinaryReader(chacha20DecoderStream);
            int hash = chacha20Reader.ReadInt32();
            int entryCount = chacha20Reader.ReadInt32();

            ReadOnlySpan<ushort> u16SaltBytes = MemoryMarshal.Cast<byte, ushort>(saltBytes);
            // TODO: validate file hash

            // 5. skip random bytes
            long entryStartPos = headerStartPos + 8 + fileName.Select(v => (int)v * 3).Sum() % 212 + 64;
            var header = new Ms_Header(fullFileName, saltStr, fileNameWithSalt, hash, version, entryCount, headerStartPos, entryStartPos);
            this.Header = header;
        }

        public void ReadEntries()
        {
            if (this.Header == null || this.Header.EntryCount == 0 || this.Header.EntryCount == this.Entries.Count)
            {
                return;
            }
            this.Entries.Clear();
            int entryCount = this.Header.EntryCount;
            if (this.Entries.Capacity < entryCount)
            {
                this.Entries.Capacity = entryCount;
            }

            // decrypt with another snow key
            string fileNameWithSalt = this.Header.FileNameWithSalt;
            Span<byte> chacha20Key2 = stackalloc byte[ChaCha20CryptoTransform.AllowedKeyLength];
            for (int i = 0; i < chacha20Key2.Length; ++i)
            {
                chacha20Key2[i] = (byte)(i + (i % 3 + 2) * fileNameWithSalt[fileNameWithSalt.Length - 1 - i % fileNameWithSalt.Length]);
                chacha20Key2[i] ^= chacha20KeyObscure[i];
            }
            Span<byte> emptyNonce = stackalloc byte[ChaCha20CryptoTransform.AllowedNonceLength];
            this.BaseStream.Position = this.Header.EntryStartPosition;
            using var chacha20Reader = new ChaCha20Reader(this.BaseStream, chacha20Key2, emptyNonce, true);

            for (int i = 0; i < entryCount; i++)
            {
                string entryName = chacha20Reader.ReadString();
                int checkSum = chacha20Reader.ReadInt32();
                int flags = chacha20Reader.ReadInt32();
                int startPos = chacha20Reader.ReadInt32();
                int size = chacha20Reader.ReadInt32();
                int sizeAligned = chacha20Reader.ReadInt32();
                int unk1 = chacha20Reader.ReadInt32();
                int unk2 = chacha20Reader.ReadInt32();
                byte[] entryKey = chacha20Reader.ReadBytes(16);
                int unk3 = chacha20Reader.ReadInt32();
                int unk4 = chacha20Reader.ReadInt32();

                var entry = new Ms_Entry(entryName, checkSum, flags, startPos, size, sizeAligned, unk1, unk2, entryKey, unk3, unk4);
                //TODO: calcuate ms_image checksum
                this.Entries.Add(entry);
            }

            long dataStartPos = this.BaseStream.Position;
            // align to 1024 bytes
            if ((dataStartPos & 0x3ff) != 0)
            {
                dataStartPos = dataStartPos - (dataStartPos & 0x3ff) + 0x400;
            }
            this.Header.DataStartPosition = dataStartPos;
            // recalculate startPos
            foreach (var entry in this.Entries)
            {
                entry.StartPos = dataStartPos + entry.StartPos * 1024;
            }
        }

        public void GetDirTree(Wz_Node parent)
        {
            foreach (var entry in this.Entries)
            {
                Wz_Node root = parent;
                string[] fullPath = entry.Name.Split('/');
                foreach (var segment in fullPath)
                {
                    root = root.Nodes[segment] ?? root.Nodes.Add(segment);
                }

                // always override existing value if already exists?
                //if (root.Value == null)
                {
                    var msImage = new Ms_ImageV2(fullPath[fullPath.Length - 1], entry, this);
                    root.Value = msImage;
                    msImage.OwnerNode = root;
                }
            }
        }

        public void Close()
        {
            if (this.BaseStream != null)
            {
                if (!this.leaveOpen)
                {
                    this.BaseStream.Dispose();
                }
                this.BaseStream = null;
            }
        }

        void IDisposable.Dispose()
        {
            this.Close();
        }

        internal static readonly byte[] chacha20KeyObscure = {
            0x7B, 0x2F, 0x35, 0x48, 0x43, 0x95, 0x02, 0xB9,
            0xAE, 0x91, 0xA6, 0xE1, 0xD8, 0xD6, 0x24, 0xB4,
            0x33, 0x10, 0x1D, 0x3D, 0xC1, 0xBB, 0xC6, 0xF4,
            0xA5, 0xFE, 0xB3, 0x69, 0x6B, 0x56, 0xE4, 0x75
        };

        internal class ChaCha20Reader : IDisposable
        {
            private readonly Stream baseStream;
            private readonly ChaCha20CryptoTransform chacha20Cipher;

            private byte[] _buffer;
            private int _readed;
            private bool leaveOpen;

            public ChaCha20Reader(Stream baseStream, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, bool leaveOpen = false)
            {
                this.baseStream = baseStream;
                this.leaveOpen = leaveOpen;
                this.chacha20Cipher = new ChaCha20CryptoTransform(key, nonce, 0);
                this._buffer = new byte[ChaCha20CryptoTransform.ProcessBytesAtTime];
                this._readed = this._buffer.Length;
            }

            public byte[] ReadBytes(int count)
            {
                byte[] buffer = new byte[count];
                this.ReadBytes(buffer);
                return buffer;
            }

            public void ReadBytes(Span<byte> buffer)
            {
                while (buffer.Length > 0)
                {
                    if (this._readed >= this._buffer.Length)
                    {
                        this.baseStream.ReadExactly(this._buffer, 0, this._buffer.Length);
                        this.chacha20Cipher.TransformBlock(this._buffer, 0, this._buffer.Length, this._buffer, 0);
                        _readed = 0;
                    }

                    int readCount = Math.Min(buffer.Length, this._buffer.Length - this._readed);
                    this._buffer.AsSpan(this._readed, readCount).CopyTo(buffer);
                    buffer = buffer.Slice(readCount);
                    this._readed += readCount;
                }

                if (_readed >= this._buffer.Length)
                {
                    this.ResetCounter();
                }
            }

            public int ReadInt32()
            {
                Span<byte> temp = stackalloc byte[4];
                this.ReadBytes(temp);
                return MemoryMarshal.Read<int>(temp);
            }

            public string ReadString()
            {
                var strLen = this.ReadInt32();
#if NET6_0_OR_GREATER
                return string.Create(strLen, this, (strBuf, reader) =>
                {
                    reader.ReadBytes(MemoryMarshal.Cast<char, byte>(strBuf));
                });
#else
                char[] strBuf = new char[strLen];
                this.ReadBytes(MemoryMarshal.Cast<char, byte>(strBuf.AsSpan()));
                return new string(strBuf);
#endif
            }

            private void ResetCounter()
            {
                this.chacha20Cipher.State[12] = 0;
            }

            public void Dispose()
            {
                if (!this.leaveOpen)
                {
                    this.baseStream.Dispose();
                }
                this.chacha20Cipher.Dispose();
                this._buffer = null;
                this._readed = 0;
            }
        }
    }
}

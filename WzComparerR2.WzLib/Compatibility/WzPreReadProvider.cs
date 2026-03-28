using System;
using System.Collections.Generic;
using System.IO;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib.Compatibility
{
    /// <summary>
    /// Interface for pre-reading wz directory tree structure without decryption.
    /// </summary>
    internal interface IWzPreReader
    {
        bool TryPreRead(Wz_File wzFile, out WzPreReadResult result);
    }

    /// <summary>
    /// Static registry of all pre-reader implementations, ordered by priority.
    /// </summary>
    internal static class WzPreReaders
    {
        private static readonly IWzPreReader[] readers = new IWzPreReader[]
        {
            new Pkg1PreReader(),
            new Pkg2PreReader(WzFileFormat.Pkg2Kmst1196, isPkg2DirString: false),
            new Pkg2PreReader(WzFileFormat.Pkg2Kmst1198, isPkg2DirString: true),
        };

        public static IReadOnlyList<IWzPreReader> All => readers;
    }

    #region PKG1

    internal sealed class Pkg1PreReader : IWzPreReader
    {
        public bool TryPreRead(Wz_File wzFile, out WzPreReadResult result)
        {
            result = null;
            if (!wzFile.Header.IsPkg1)
                return false;

            try
            {
                wzFile.FileStream.Position = wzFile.Header.DirStartPosition;
                var reader = new WzBinaryReader(wzFile.FileStream, false);
                long dirStartPos = wzFile.FileStream.Position;
                result = new WzPreReadResult(WzFileFormat.Pkg1, new List<WzPreReadNodeInfo>(), dirStartPos, 0);
                ReadTree(reader, result);
                result.DirEndPosition = reader.BaseStream.Position;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private static void ReadTree(WzBinaryReader reader, WzPreReadResult result)
        {
            int count = reader.ReadCompressedInt32();
            int dirCount = 0;

            for (int i = 0; i < count; i++)
            {
                byte nodeType = reader.ReadByte();
                switch (nodeType)
                {
                    case 0x02:
                        reader.ReadInt32();
                        break;
                    case 0x03:
                    case 0x04:
                        if (result.FirstStringRawBytes == null)
                        {
                            result.FirstStringRawBytes = WzPreReadHelper.ReadStringRawBytes(reader, false, out var enc);
                            result.FirstStringEncoding = enc;
                        }
                        else
                        {
                            WzPreReadHelper.SkipString(reader);
                        }
                        break;
                    default:
                        throw new InvalidDataException($"Unknown type {nodeType} in dir pre-read.");
                }

                int size = reader.ReadCompressedInt32();
                reader.ReadCompressedInt32();
                uint offsetPos = (uint)reader.BaseStream.Position;
                uint hashedOffset = reader.ReadUInt32();

                result.Nodes.Add(new WzPreReadNodeInfo
                {
                    NodeType = nodeType,
                    DataLength = size,
                    HashedOffsetPosition = offsetPos,
                    HashedOffset = hashedOffset,
                });

                if (nodeType == 0x03) dirCount++;
            }

            for (int i = 0; i < dirCount; i++)
                ReadTree(reader, result);
        }
    }

    #endregion

    #region PKG2

    internal sealed class Pkg2PreReader : IWzPreReader
    {
        public Pkg2PreReader(WzFileFormat format, bool isPkg2DirString)
        {
            this.format = format;
            this.isPkg2DirString = isPkg2DirString;
        }

        private readonly WzFileFormat format;
        private readonly bool isPkg2DirString;

        public bool TryPreRead(Wz_File wzFile, out WzPreReadResult result)
        {
            result = null;
            if (!wzFile.Header.IsPkg2)
                return false;

            try
            {
                wzFile.FileStream.Position = wzFile.Header.DirStartPosition;
                var reader = new WzBinaryReader(wzFile.FileStream, false);
                long dirStartPos = wzFile.FileStream.Position;
                result = new WzPreReadResult(this.format, new List<WzPreReadNodeInfo>(), dirStartPos, 0)
                {
                    Pkg2DirEntryCounts = new List<Pkg2DirEntryCount>(),
                };
                ReadTree(reader, result, this.isPkg2DirString);
                result.DirEndPosition = reader.BaseStream.Position;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private static void ReadTree(WzBinaryReader reader, WzPreReadResult result, bool isPkg2DirString)
        {
            int encryptedEntryCount = reader.ReadCompressedInt32();
            var temp = new List<Pkg2TempEntry>();

            while (true)
            {
                byte nodeType = reader.ReadByte();
                if (nodeType == 0x03 || nodeType == 0x04)
                {
                    if (result.FirstStringRawBytes == null && temp.Count == 0)
                    {
                        result.FirstStringRawBytes = WzPreReadHelper.ReadStringRawBytes(reader, isPkg2DirString, out var enc);
                        result.FirstStringEncoding = enc;
                    }
                    else if (isPkg2DirString && result.SecondStringRawBytes == null && temp.Count == 1)
                    {
                        result.SecondStringRawBytes = WzPreReadHelper.ReadStringRawBytes(reader, false, out var enc);
                        result.SecondStringEncoding = enc;
                    }
                    else
                        WzPreReadHelper.SkipString(reader);

                    int size = reader.ReadCompressedInt32();
                    reader.ReadCompressedInt32();
                    temp.Add(new Pkg2TempEntry { NodeType = nodeType, DataLength = size });
                }
                else if (nodeType == 0x80
                    || (encryptedEntryCount >= -127 && encryptedEntryCount <= 127 && nodeType == encryptedEntryCount))
                {
                    reader.BaseStream.Position--;
                    break;
                }
                else
                {
                    throw new InvalidDataException($"Invalid node type {nodeType} in pkg2 pre-read.");
                }
            }

            int encryptedOffsetCount = reader.ReadCompressedInt32();
            if (encryptedOffsetCount != encryptedEntryCount)
                throw new InvalidDataException("Offset count does not match entry count.");

            result.Pkg2DirEntryCounts.Add(new Pkg2DirEntryCount
            {
                EncryptedEntryCount = encryptedEntryCount,
                ActualEntryCount = temp.Count,
            });

            int dirCount = 0;
            for (int i = 0; i < temp.Count; i++)
            {
                uint offsetPos = (uint)reader.BaseStream.Position;
                uint hashedOffset = reader.ReadUInt32();
                result.Nodes.Add(new WzPreReadNodeInfo
                {
                    NodeType = temp[i].NodeType,
                    DataLength = temp[i].DataLength,
                    HashedOffsetPosition = offsetPos,
                    HashedOffset = hashedOffset,
                });
                if (temp[i].NodeType == 0x03) dirCount++;
            }

            for (int i = 0; i < dirCount; i++)
                ReadTree(reader, result, isPkg2DirString);
        }

        private struct Pkg2TempEntry
        {
            public int NodeType;
            public int DataLength;
        }
    }

    #endregion

    #region Shared helpers

    internal static class WzPreReadHelper
    {
        public static byte[] ReadStringRawBytes(this WzBinaryReader reader, bool isPkg2DirString, out WzStringEncoding encoding)
        {
            sbyte lenPrefix = reader.ReadSByte();
            if (isPkg2DirString)
            {
                encoding = WzStringEncoding.UTF16;
                if (lenPrefix < 0) return reader.ReadBytes((-lenPrefix) * 2);
                if (lenPrefix == 0) return Array.Empty<byte>();
                throw new InvalidDataException("Unexpected positive length in pkg2 dir string.");
            }

            if (lenPrefix < 0)
            {
                encoding = WzStringEncoding.ASCII;
                int size = lenPrefix == -128 ? reader.ReadInt32() : -lenPrefix;
                return reader.ReadBytes(size);
            }
            if (lenPrefix > 0)
            {
                encoding = WzStringEncoding.UTF16;
                int size = lenPrefix == 127 ? reader.ReadInt32() : lenPrefix;
                return reader.ReadBytes(size * 2);
            }
            encoding = WzStringEncoding.Unknown;
            return Array.Empty<byte>();
        }

        public static void SkipString(this WzBinaryReader reader)
        {
            sbyte len = reader.ReadSByte();
            if (len < 0)
            {
                int size = len == -128 ? reader.ReadInt32() : -len;
                reader.BaseStream.Position += size;
            }
            else if (len > 0)
            {
                int size = len == 127 ? reader.ReadInt32() : len;
                reader.BaseStream.Position += size * 2;
            }
        }

        public static bool IsLegalNodeName(ReadOnlySpan<char> utf16NodeName)
        {
            if (utf16NodeName.Length == 0) return false;
            if (utf16NodeName.EndsWith(".img".AsSpan()) || utf16NodeName.EndsWith(".lua".AsSpan())) return true;
            foreach (var c in utf16NodeName)
            {
                if (!(0x20 <= c && c <= 0x7f)) return false;
            }
            return true;
        }

        public static bool IsLegalNodeName(ReadOnlySpan<byte> asciiNodeName)
        {
            if (asciiNodeName.Length == 0) return false;
            if (asciiNodeName.EndsWith(".img"u8) || asciiNodeName.EndsWith(".lua"u8)) return true;
            foreach (var c in asciiNodeName)
            {
                if (!(0x20 <= c && c <= 0x7f)) return false;
            }
            return true;
        }
    }

    #endregion

    #region Enums and result types

    public enum WzFileFormat
    {
        Pkg1,
        Pkg2Kmst1196,
        Pkg2Kmst1198,
    }

    public enum WzStringEncoding
    {
        Unknown,
        ASCII,
        UTF16,
    }

    public sealed class WzPreReadResult
    {
        public WzPreReadResult(WzFileFormat format, List<WzPreReadNodeInfo> nodes, long dirStartPosition, long dirEndPosition)
        {
            this.Format = format;
            this.Nodes = nodes;
            this.DirStartPosition = dirStartPosition;
            this.DirEndPosition = dirEndPosition;
        }

        public WzFileFormat Format { get; }
        public List<WzPreReadNodeInfo> Nodes { get; }
        public long DirStartPosition { get; }
        public long DirEndPosition { get; internal set; }
        public WzStringEncoding FirstStringEncoding { get; set; }
        public byte[] FirstStringRawBytes { get; set; }
        public WzStringEncoding SecondStringEncoding { get; set; }
        public byte[] SecondStringRawBytes { get; set; }

        /// <summary>
        /// Per-directory-level encrypted and actual entry counts for PKG2 files.
        /// </summary>
        public List<Pkg2DirEntryCount> Pkg2DirEntryCounts { get; set; }
    }

    public struct WzPreReadNodeInfo
    {
        public int NodeType;
        public int DataLength;
        public uint HashedOffsetPosition;
        public uint HashedOffset;
    }

    public struct Pkg2DirEntryCount
    {
        public int EncryptedEntryCount;
        public int ActualEntryCount;
    }

    #endregion
}

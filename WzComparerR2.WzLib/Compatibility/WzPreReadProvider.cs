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
            new Pkg2PreReader64(WzFileFormat.Pkg2Kmst1206, 353, true, true, Pkg2EntryNamePosition.AfterData),
            new Pkg2PreReader64(WzFileFormat.Pkg2Kmst1205, 163, true, true, Pkg2EntryNamePosition.AfterData),
            new Pkg2PreReader64(WzFileFormat.Pkg2Kmst1204, 200, true, true),
            new Pkg2PreReader64(WzFileFormat.Pkg2Kmst1202, 150, false, false),
            new Pkg2PreReader(WzFileFormat.Pkg2Kmst1201, true, true),
            new Pkg2PreReader(WzFileFormat.Pkg2Kmst1198, true),
            new Pkg2PreReader(WzFileFormat.Pkg2Kmst1196, false),
            new Pkg1PreReader(),
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

                if (nodeType == 0x03)
                    dirCount++;
            }

            for (int i = 0; i < dirCount; i++)
                ReadTree(reader, result);
        }
    }

    #endregion

    #region PKG2

    internal sealed class Pkg2PreReader : IWzPreReader
    {
        public Pkg2PreReader(WzFileFormat format, bool isPkg2DirString = false, bool requireRandomHeader = false)
        {
            this.rule = new Pkg2PreReadRule(format, isPkg2DirString, requireRandomHeader);
        }

        private readonly IPkg2PreReadRule rule;

        public bool TryPreRead(Wz_File wzFile, out WzPreReadResult result)
        {
            return Pkg2PreReadTreeWalker.TryPreRead(wzFile, this.rule, out result);
        }
    }

    internal sealed class Pkg2PreReader64 : IWzPreReader
    {
        public Pkg2PreReader64(WzFileFormat format, int headerSize, bool allNamesUseV2, bool encryptEntryData, Pkg2EntryNamePosition entryNamePosition = Pkg2EntryNamePosition.BeforeData)
        {
            this.rule = new Pkg2PreReadRule64(format, headerSize, allNamesUseV2, encryptEntryData, entryNamePosition);
        }

        private readonly IPkg2PreReadRule rule;

        public bool TryPreRead(Wz_File wzFile, out WzPreReadResult result)
        {
            return Pkg2PreReadTreeWalker.TryPreRead(wzFile, rule, out result);
        }
    }

    internal static class Pkg2PreReadTreeWalker
    {
        public static bool TryPreRead(Wz_File wzFile, IPkg2PreReadRule rule, out WzPreReadResult result)
        {
            result = null;
            if (!rule.CanHandle(wzFile, out var context))
                return false;

            try
            {
                wzFile.FileStream.Position = wzFile.Header.DirStartPosition;
                var reader = new WzBinaryReader(wzFile.FileStream, false);
                long dirStartPos = wzFile.FileStream.Position;
                result = new WzPreReadResult(rule.Format, new List<WzPreReadNodeInfo>(), dirStartPos, 0)
                {
                    Pkg2DirEntryCounts = new List<Pkg2DirEntryCount>(),
                };

                ReadTree(reader, result, rule, context, wzFile.FileStream.Length, true);
                result.DirEndPosition = reader.BaseStream.Position;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private static void ReadTree(WzBinaryReader reader, WzPreReadResult result, IPkg2PreReadRule rule, Pkg2PreReadContext context, long fileLen, bool isTopLevel)
        {
            var header = rule.ReadDirectoryHeader(reader, context);
            var entries = new List<Pkg2PreReadEntry>();
            long entriesStartPosition = reader.BaseStream.Position;

            if (header.IsFixedEntryCount)
            {
                for (long i = 0; i < header.FixedEntryCount; i++)
                {
                    ReadOneEntry(reader, result, rule, context, entries.Count, entries);
                }
            }
            else
            {
                while (true)
                {
                    if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    {
                        if (rule.AllowEntryBoundaryProbe)
                            break;
                        throw new EndOfStreamException("Unexpected end of PKG2 directory pre-read.");
                    }

                    long entryStartPosition = reader.BaseStream.Position;
                    byte nodeType = reader.ReadByte();
                    if (nodeType == 0x03 || nodeType == 0x04)
                    {
                        try
                        {
                            if (rule.EntryNamePosition == Pkg2EntryNamePosition.BeforeData)
                                rule.ReadEntryName(reader, result, context, entries.Count);
                            uint sizePosition = (uint)reader.BaseStream.Position;
                            int size = reader.ReadCompressedInt32();
                            if (rule.ValidateImageLength && (size < 0 || (nodeType == 0x03 && size != 0)))
                            {
                                reader.BaseStream.Position = entryStartPosition;
                                break;
                            }
                            reader.ReadCompressedInt32();
                            if (rule.EntryNamePosition == Pkg2EntryNamePosition.AfterData)
                                rule.ReadEntryName(reader, result, context, entries.Count);
                            entries.Add(new Pkg2PreReadEntry
                            {
                                NodeType = nodeType,
                                DataLength = size,
                                DataLengthPosition = sizePosition,
                                IsDataLengthEncrypted = rule.IsEntryDataEncrypted,
                                EndPosition = reader.BaseStream.Position,
                            });
                        }
                        catch when (rule.AllowEntryBoundaryProbe)
                        {
                            reader.BaseStream.Position = entryStartPosition;
                            break;
                        }
                    }
                    else if (rule.IsDirectoryTerminator(nodeType, header))
                    {
                        reader.BaseStream.Position--;
                        break;
                    }
                    else if (rule.AllowEntryBoundaryProbe)
                    {
                        reader.BaseStream.Position = entryStartPosition;
                        break;
                    }
                    else
                    {
                        throw new InvalidDataException($"Invalid node type {nodeType} in pkg2 pre-read.");
                    }
                }
            }

            int dirCount = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].NodeType == 0x03)
                    dirCount++;
            }

            if (rule.AllowEntryBoundaryProbe && isTopLevel && !header.IsFixedEntryCount)
            {
                var candidates = BuildTopLevelImageCandidates(reader, result, header, entries, entriesStartPosition, fileLen, rule.EntryBoundaryCandidateBacktrackCount);
                if (candidates.Count == 0)
                    throw new InvalidDataException("PKG2 top-level image boundary probe failed.");

                result.Candidates ??= new List<WzPreReadCandidate>();
                result.Candidates.AddRange(candidates);
                ApplyCandidate(result, candidates[0]);
                reader.BaseStream.Position = candidates[0].DirEndPosition;
                return;
            }

            if (rule.ValidateImageLength && isTopLevel && dirCount == 0)
            {
                bool matched = false;
                for (int count = entries.Count; count >= 0; count--)
                {
                    long offsetStart = count == 0 ? entriesStartPosition : entries[count - 1].EndPosition;
                    long dirEndPosition = offsetStart + count * 4L;
                    if (dirEndPosition > fileLen)
                        continue;

                    long imageDataLengthSum = 0;
                    for (int i = 0; i < count; i++)
                    {
                        if (entries[i].NodeType == 0x04)
                            imageDataLengthSum += entries[i].DataLength;
                    }

                    if (imageDataLengthSum == fileLen - dirEndPosition)
                    {
                        if (count < entries.Count)
                            entries.RemoveRange(count, entries.Count - count);
                        reader.BaseStream.Position = offsetStart;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                    throw new InvalidDataException("PKG2 top-level image length validation failed.");
            }

            rule.ValidateOffsetSection(reader, header, entries.Count);

            result.Pkg2DirEntryCounts.Add(new Pkg2DirEntryCount
            {
                EncryptedEntryCount = header.EncryptedEntryCount,
                ActualEntryCount = entries.Count,
            });

            for (int i = 0; i < entries.Count; i++)
            {
                uint offsetPos = (uint)reader.BaseStream.Position;
                uint hashedOffset = reader.ReadUInt32();
                result.Nodes.Add(new WzPreReadNodeInfo
                {
                    NodeType = entries[i].NodeType,
                    DataLength = entries[i].DataLength,
                    DataLengthPosition = entries[i].DataLengthPosition,
                    IsDataLengthEncrypted = entries[i].IsDataLengthEncrypted,
                    HashedOffsetPosition = offsetPos,
                    HashedOffset = hashedOffset,
                });
            }

            for (int i = 0; i < dirCount; i++)
                ReadTree(reader, result, rule, context, fileLen, false);
        }

        private static List<WzPreReadCandidate> BuildTopLevelImageCandidates(WzBinaryReader reader, WzPreReadResult result, Pkg2DirHeader header, List<Pkg2PreReadEntry> entries, long entriesStartPosition, long fileLen, int backtrackCount)
        {
            var candidates = new List<WzPreReadCandidate>();
            long savedPosition = reader.BaseStream.Position;
            int minCount = Math.Max(0, entries.Count - Math.Max(0, backtrackCount));

            for (int count = entries.Count; count >= minCount; count--)
            {
                long offsetStart = count == 0 ? entriesStartPosition : entries[count - 1].EndPosition;
                long dirEndPosition = offsetStart + count * 4L;
                if (dirEndPosition > fileLen)
                    continue;

                var nodes = new List<WzPreReadNodeInfo>(count);
                try
                {
                    reader.BaseStream.Position = offsetStart;
                    for (int i = 0; i < count; i++)
                    {
                        uint offsetPos = (uint)reader.BaseStream.Position;
                        uint hashedOffset = reader.ReadUInt32();
                        nodes.Add(new WzPreReadNodeInfo
                        {
                            NodeType = entries[i].NodeType,
                            DataLength = entries[i].DataLength,
                            DataLengthPosition = entries[i].DataLengthPosition,
                            IsDataLengthEncrypted = entries[i].IsDataLengthEncrypted,
                            HashedOffsetPosition = offsetPos,
                            HashedOffset = hashedOffset,
                        });
                    }
                }
                catch (EndOfStreamException)
                {
                    continue;
                }

                candidates.Add(new WzPreReadCandidate
                {
                    Nodes = nodes,
                    DirEndPosition = dirEndPosition,
                    AllowLooseDirectoryOffsets = true,
                    Pkg2DirEntryCounts = new List<Pkg2DirEntryCount>
                    {
                        new Pkg2DirEntryCount
                        {
                            EncryptedEntryCount = header.EncryptedEntryCount,
                            ActualEntryCount = count,
                        },
                    },
                });
            }

            reader.BaseStream.Position = savedPosition;
            return candidates;
        }

        private static void ApplyCandidate(WzPreReadResult result, WzPreReadCandidate candidate)
        {
            result.Nodes.Clear();
            result.Nodes.AddRange(candidate.Nodes);
            result.DirEndPosition = candidate.DirEndPosition;
            result.Pkg2DirEntryCounts = candidate.Pkg2DirEntryCounts;
        }

        private static void ReadOneEntry(WzBinaryReader reader, WzPreReadResult result, IPkg2PreReadRule rule, Pkg2PreReadContext context, int entryIndex, List<Pkg2PreReadEntry> entries)
        {
            byte nodeType = reader.ReadByte();
            if (nodeType != 0x03 && nodeType != 0x04)
            {
                throw new InvalidDataException($"Invalid node type {nodeType} in pkg2 pre-read.");
            }

            if (rule.EntryNamePosition == Pkg2EntryNamePosition.BeforeData)
                rule.ReadEntryName(reader, result, context, entryIndex);
            uint sizePosition = (uint)reader.BaseStream.Position;
            int size = reader.ReadCompressedInt32();
            reader.ReadCompressedInt32();
            if (rule.EntryNamePosition == Pkg2EntryNamePosition.AfterData)
                rule.ReadEntryName(reader, result, context, entryIndex);
            entries.Add(new Pkg2PreReadEntry
            {
                NodeType = nodeType,
                DataLength = size,
                DataLengthPosition = sizePosition,
                IsDataLengthEncrypted = rule.IsEntryDataEncrypted,
                EndPosition = reader.BaseStream.Position,
            });
        }
    }

    internal interface IPkg2PreReadRule
    {
        WzFileFormat Format { get; }
        bool AllowEntryBoundaryProbe { get; }
        int EntryBoundaryCandidateBacktrackCount { get; }
        bool ValidateImageLength { get; }
        bool IsEntryDataEncrypted { get; }
        Pkg2EntryNamePosition EntryNamePosition { get; }
        bool CanHandle(Wz_File wzFile, out Pkg2PreReadContext context);
        Pkg2DirHeader ReadDirectoryHeader(WzBinaryReader reader, Pkg2PreReadContext context);
        bool IsDirectoryTerminator(byte nodeType, Pkg2DirHeader header);
        void ReadEntryName(WzBinaryReader reader, WzPreReadResult result, Pkg2PreReadContext context, int entryIndex);
        void ValidateOffsetSection(WzBinaryReader reader, Pkg2DirHeader header, int actualEntryCount);
    }

    internal sealed class Pkg2PreReadRule : IPkg2PreReadRule
    {
        public Pkg2PreReadRule(WzFileFormat format, bool isPkg2DirString, bool requireRandomHeader)
        {
            this.Format = format;
            this.isPkg2DirString = isPkg2DirString;
            this.requireRandomHeader = requireRandomHeader;
        }

        private readonly bool isPkg2DirString;
        private readonly bool requireRandomHeader;

        public WzFileFormat Format { get; }
        public bool AllowEntryBoundaryProbe => false;
        public int EntryBoundaryCandidateBacktrackCount => 2;
        public bool ValidateImageLength => false;
        public bool IsEntryDataEncrypted => false;
        public Pkg2EntryNamePosition EntryNamePosition => Pkg2EntryNamePosition.BeforeData;

        public bool CanHandle(Wz_File wzFile, out Pkg2PreReadContext context)
        {
            context = new Pkg2PreReadContext();
            if (!wzFile.Header.IsPkg2)
                return false;

            if (wzFile.Header.HasCapabilities(Wz_Capabilities.Pkg2RandomHeader64))
                return false;

            bool hasRandomHeader = wzFile.Header.HasCapabilities(Wz_Capabilities.Pkg2RandomHeader);
            if (hasRandomHeader != this.requireRandomHeader)
                return false;

            context.IsPkg2DirString = this.isPkg2DirString;
            context.Header64 = null;
            return true;
        }

        public Pkg2DirHeader ReadDirectoryHeader(WzBinaryReader reader, Pkg2PreReadContext context)
        {
            return new Pkg2DirHeader
            {
                EncryptedEntryCount = reader.ReadCompressedInt32(),
                IsFixedEntryCount = false,
                FixedEntryCount = 0,
            };
        }

        public bool IsDirectoryTerminator(byte nodeType, Pkg2DirHeader header)
        {
            return nodeType == 0x80
                || (header.EncryptedEntryCount >= -127 && header.EncryptedEntryCount <= 127 && nodeType == header.EncryptedEntryCount);
        }

        public void ReadEntryName(WzBinaryReader reader, WzPreReadResult result, Pkg2PreReadContext context, int entryIndex)
        {
            if (result.FirstStringRawBytes == null && entryIndex == 0)
            {
                result.FirstStringRawBytes = WzPreReadHelper.ReadStringRawBytes(reader, context.IsPkg2DirString, out var enc);
                result.FirstStringEncoding = enc;
            }
            else if (context.IsPkg2DirString && result.SecondStringRawBytes == null && entryIndex == 1)
            {
                result.SecondStringRawBytes = WzPreReadHelper.ReadStringRawBytes(reader, false, out var enc);
                result.SecondStringEncoding = enc;
            }
            else
            {
                WzPreReadHelper.SkipString(reader);
            }
        }

        public void ValidateOffsetSection(WzBinaryReader reader, Pkg2DirHeader header, int actualEntryCount)
        {
            int encryptedOffsetCount = reader.ReadCompressedInt32();
            if (encryptedOffsetCount != header.EncryptedEntryCount)
                throw new InvalidDataException("Offset count does not match entry count.");
        }
    }

    internal sealed class Pkg2PreReadRule64 : IPkg2PreReadRule
    {
        public Pkg2PreReadRule64(WzFileFormat format, int headerSize, bool allNamesUseV2, bool encryptEntryData, Pkg2EntryNamePosition entryNamePosition)
        {
            this.Format = format;
            this.headerSize = headerSize;
            this.allNamesUseV2 = allNamesUseV2;
            this.encryptEntryData = encryptEntryData;
            this.EntryNamePosition = entryNamePosition;
        }

        private readonly int headerSize;
        private readonly bool allNamesUseV2;
        private readonly bool encryptEntryData;

        public WzFileFormat Format { get; }
        public bool AllowEntryBoundaryProbe => true;
        public int EntryBoundaryCandidateBacktrackCount => 2;
        public bool ValidateImageLength => !this.encryptEntryData;
        public bool IsEntryDataEncrypted => this.encryptEntryData;
        public Pkg2EntryNamePosition EntryNamePosition { get; }

        public bool CanHandle(Wz_File wzFile, out Pkg2PreReadContext context)
        {
            context = new Pkg2PreReadContext();
            if (!wzFile.Header.IsPkg2)
                return false;
            if (wzFile.Header is not Wz_Header.WzPkg2Header64 header64)
                return false;
            if (header64.HeaderSize != this.headerSize)
                return false;

            context.IsPkg2DirString = true;
            context.Header64 = header64;
            return true;
        }

        public Pkg2DirHeader ReadDirectoryHeader(WzBinaryReader reader, Pkg2PreReadContext context)
        {
            long encryptedEntryCount = reader.ReadCompressedInt64();
            return new Pkg2DirHeader
            {
                EncryptedEntryCount = encryptedEntryCount,
                IsFixedEntryCount = false,
                FixedEntryCount = 0,
            };
        }

        public bool IsDirectoryTerminator(byte nodeType, Pkg2DirHeader header)
        {
            return false;
        }

        public void ReadEntryName(WzBinaryReader reader, WzPreReadResult result, Pkg2PreReadContext context, int entryIndex)
        {
            if (result.FirstStringRawBytes == null && entryIndex == 0)
            {
                result.FirstStringRawBytes = WzPreReadHelper.ReadPkg2DirStringV2RawBytes(reader, out var enc);
                result.FirstStringEncoding = enc;
            }
            else if (this.allNamesUseV2)
            {
                WzPreReadHelper.SkipPkg2DirStringV2(reader);
            }
            else if (result.SecondStringRawBytes == null && entryIndex == 1)
            {
                result.SecondStringRawBytes = WzPreReadHelper.ReadStringRawBytes(reader, false, out var enc);
                result.SecondStringEncoding = enc;
            }
            else if (entryIndex == 0)
            {
                WzPreReadHelper.SkipPkg2DirStringV2(reader);
            }
            else
            {
                WzPreReadHelper.SkipString(reader);
            }
        }

        public void ValidateOffsetSection(WzBinaryReader reader, Pkg2DirHeader header, int actualEntryCount)
        {
            // KMST1202 has no encrypted offset-count prefix.
        }
    }

    internal struct Pkg2PreReadContext
    {
        public bool IsPkg2DirString;
        public Wz_Header.WzPkg2Header64 Header64;
    }

    internal struct Pkg2DirHeader
    {
        public long EncryptedEntryCount;
        public bool IsFixedEntryCount;
        public long FixedEntryCount;
    }

    internal struct Pkg2PreReadEntry
    {
        public int NodeType;
        public int DataLength;
        public uint DataLengthPosition;
        public bool IsDataLengthEncrypted;
        public long EndPosition;
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

        public static byte[] ReadPkg2DirStringV2RawBytes(this WzBinaryReader reader, out WzStringEncoding encoding)
        {
            int len = reader.ReadInt16();
            encoding = WzStringEncoding.UTF16;
            if (len < 0)
            {
                return reader.ReadBytes((-len) * 2);
            }
            if (len == 0)
            {
                return Array.Empty<byte>();
            }
            throw new InvalidDataException("Unexpected positive length in pkg2 dir string v2.");
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

        public static void SkipPkg2DirStringV2(this WzBinaryReader reader)
        {
            int len = reader.ReadInt16();
            if (len < 0)
            {
                reader.BaseStream.Position += (-len) * 2;
            }
            else if (len > 0)
            {
                throw new InvalidDataException("Unexpected positive length in pkg2 dir string v2.");
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
        Pkg2Kmst1201,
        Pkg2Kmst1202,
        Pkg2Kmst1204,
        Pkg2Kmst1205,
        Pkg2Kmst1206,
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
        /// Alternative directory boundary candidates produced by preread when the entry/offset split is ambiguous.
        /// </summary>
        public List<WzPreReadCandidate> Candidates { get; set; }

        /// <summary>
        /// Per-directory-level encrypted and actual entry counts for PKG2 files.
        /// </summary>
        public List<Pkg2DirEntryCount> Pkg2DirEntryCounts { get; set; }
    }

    public sealed class WzPreReadCandidate
    {
        public List<WzPreReadNodeInfo> Nodes { get; set; }
        public long DirEndPosition { get; set; }
        public bool AllowLooseDirectoryOffsets { get; set; }
        public List<Pkg2DirEntryCount> Pkg2DirEntryCounts { get; set; }
    }

    public struct WzPreReadNodeInfo
    {
        public int NodeType;
        public int DataLength;
        public uint DataLengthPosition;
        public bool IsDataLengthEncrypted;
        public uint HashedOffsetPosition;
        public uint HashedOffset;
    }

    public struct Pkg2DirEntryCount
    {
        public long EncryptedEntryCount;
        public int ActualEntryCount;
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.IO;
using K4os.Compression.LZ4;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// UnityFS bundle parsing plus random access into LZ4 blocks.
    /// <para>
    /// UnityFS header (all big-endian):
    /// "UnityFS\0" | version u32 | unityVersion cstr | unityRevision cstr
    /// | size i64 | compressedBlocksInfoSize u32 | uncompressedBlocksInfoSize u32 | flags u32
    /// </para>
    /// <para>
    /// Key flag bits: 0x3F = blocksInfo compression method (0=raw, 1=LZMA, 2/3=LZ4/LZ4HC);
    /// 0x40 = blocksInfo merged with the directory; 0x80 = blocksInfo stored at the end of the file;
    /// 0x200 = the data area after blocksInfo must be aligned to 16 bytes.
    /// </para>
    /// <para>
    /// For version >= 7 the fixed header fields must be aligned to 16 bytes before blocksInfo.
    /// blocksInfo blob (big-endian): hash[16] | blockCount u32 |
    /// blocks[(uncompressedSize u32, compressedSize u32, flags u16)] |
    /// nodeCount u32 | nodes[(offset i64, size i64, flags u32, name cstr)].
    /// </para>
    /// <para>
    /// A node's (offset, size) is relative to "the address space formed by concatenating all decompressed blocks",
    /// so a prefix sum of the uncompressed sizes maps any byte range to a small number of blocks.
    /// </para>
    /// </summary>
    public sealed class UnityFsBundle : IDisposable
    {
        /// <summary>blocksInfo is stored at the end of the file.</summary>
        private const uint FlagBlocksInfoAtEnd = 0x80;

        /// <summary>The data area after blocksInfo must be aligned to 16 bytes.</summary>
        private const uint FlagBlockInfoNeedPaddingAtStart = 0x200;

        private const uint SupportedFileVersion = 8;
        private const int MaxBlocksInfoSize = 256 * 1024 * 1024;
        private const int MaxBlockCount = 1000000;
        private const int MaxNodeCount = 1000000;
        private const uint MaxBlockSize = 512 * 1024 * 1024;
        private const int MaxReadSize = 1024 * 1024 * 1024;
        private const int BlockCacheCapacity = 8;

        private readonly IUnityDataStream source;
        private readonly bool ownsSource;
        private readonly List<BundleBlock> blocks;
        private readonly List<long> blockFileOffsets;
        private readonly List<long> blockDataPrefix;
        private readonly List<BundleNode> nodes;
        private readonly object cacheLock = new object();
        private readonly Dictionary<int, byte[]> blockCache = new Dictionary<int, byte[]>();
        private readonly List<int> blockCacheOrder = new List<int>();

        private UnityFsBundle(IUnityDataStream source, bool ownsSource)
        {
            this.source = source;
            this.ownsSource = ownsSource;
            this.blocks = new List<BundleBlock>();
            this.blockFileOffsets = new List<long>();
            this.blockDataPrefix = new List<long>();
            this.nodes = new List<BundleNode>();
            this.Parse();
        }

        /// <summary>Opens a bundle file on disk.</summary>
        public static UnityFsBundle Open(string path)
        {
            return new UnityFsBundle(new UnityFileDataStream(path), true);
        }

        /// <summary>Opens a bundle from an arbitrary random-access source.</summary>
        public static UnityFsBundle Open(IUnityDataStream source, bool ownsSource = false)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return new UnityFsBundle(source, ownsSource);
        }

        public IReadOnlyList<BundleNode> Nodes
        {
            get { return this.nodes; }
        }

        /// <summary>The version of the Unity engine that built this bundle (unityVersion in the UnityFS header).</summary>
        public string UnityVersion { get; private set; }

        /// <summary>The Unity revision that built this bundle (unityRevision in the UnityFS header).</summary>
        public string UnityRevision { get; private set; }

        /// <summary>Total size of the decompressed data area.</summary>
        public long DecompressedSize
        {
            get { return this.blockDataPrefix.Count == 0 ? 0 : this.blockDataPrefix[this.blockDataPrefix.Count - 1]; }
        }

        /// <summary>Finds a node by name (case-sensitive) and returns its index; returns -1 when not found.</summary>
        public int FindNode(string name)
        {
            for (int i = 0; i < this.nodes.Count; i++)
            {
                if (this.nodes[i].Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Reads an entire node.</summary>
        public byte[] ReadNode(int nodeIndex)
        {
            BundleNode node = this.GetNode(nodeIndex);
            if (node.Size > int.MaxValue)
            {
                throw new UnityException(string.Format("node {0} is too large to read into memory.", node.Name));
            }
            return this.ReadNodeRange(nodeIndex, 0, (int)node.Size);
        }

        /// <summary>Reads [offset, offset+length) inside a node, decompressing only the blocks it spans.</summary>
        public byte[] ReadNodeRange(int nodeIndex, long offset, int length)
        {
            BundleNode node = this.GetNode(nodeIndex);
            if (length < 0 || length > MaxReadSize)
            {
                throw new UnityException("A single read exceeds the limit: " + length);
            }
            if (offset < 0 || offset > node.Size - length)
            {
                throw new UnityException(string.Format(
                    "Read of node {0} out of range: offset={1} len={2} size={3}", node.Name, offset, length, node.Size));
            }
            return this.ReadDataRange(node.Offset + offset, length);
        }

        /// <summary>Reads [offset, offset+length) against the decompressed address space.</summary>
        public byte[] ReadDataRange(long offset, int length)
        {
            if (length == 0)
            {
                return new byte[0];
            }
            if (length < 0 || length > MaxReadSize)
            {
                throw new UnityException("A single read exceeds the limit: " + length);
            }
            int first;
            int last;
            if (!TryGetBlocksForRange(this.blockDataPrefix, offset, length, out first, out last))
            {
                throw new UnityException(string.Format(
                    "Data range out of range: offset={0} len={1}", offset, length));
            }

            byte[] output = new byte[length];
            int written = 0;
            long cursor = offset;
            long remaining = length;
            for (int blockIndex = first; blockIndex <= last; blockIndex++)
            {
                byte[] block = this.DecompressBlock(blockIndex);
                int inner = checked((int)(cursor - this.blockDataPrefix[blockIndex]));
                int available = block.Length - inner;
                if (available < 0)
                {
                    throw new UnityException("The offset inside the block is out of range.");
                }
                int take = (int)Math.Min(remaining, available);
                Buffer.BlockCopy(block, inner, output, written, take);
                cursor += take;
                written += take;
                remaining -= take;
            }
            return output;
        }

        public void Dispose()
        {
            if (this.ownsSource)
            {
                this.source.Dispose();
            }
        }

        /// <summary>
        /// Computes which blocks [offset, offset+length) falls into (returns the first and last index, both inclusive).
        /// </summary>
        internal static bool TryGetBlocksForRange(
            IReadOnlyList<long> prefix, long offset, int length, out int first, out int last)
        {
            first = -1;
            last = -1;
            if (prefix == null || prefix.Count < 2 || length <= 0)
            {
                return false;
            }
            long end = offset + length;
            if (end < offset || end > prefix[prefix.Count - 1])
            {
                return false;
            }
            first = FindPrefixIndex(prefix, offset);
            last = FindPrefixIndex(prefix, end - 1);
            return first >= 0 && last >= first;
        }

        /// <summary>The largest i such that prefix[i] &lt;= value.</summary>
        private static int FindPrefixIndex(IReadOnlyList<long> prefix, long value)
        {
            int low = 0;
            int high = prefix.Count - 1;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (prefix[mid] <= value)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return low;
        }

        internal static long AlignUp(long value, long alignment)
        {
            return ((value + alignment - 1) / alignment) * alignment;
        }

        private BundleNode GetNode(int index)
        {
            if (index < 0 || index >= this.nodes.Count)
            {
                throw new UnityException("node index out of range: " + index);
            }
            return this.nodes[index];
        }

        private void Parse()
        {
            long fileLength = this.source.Length;
            int headLength = (int)Math.Min(fileLength, 256);
            byte[] head = new byte[headLength];
            this.source.ReadAt(0, head, 0, headLength);

            UnityByteReader reader = new UnityByteReader(head, false);
            byte[] signature = reader.Take(8);
            if (!BytesEqual(signature, new byte[] { 0x55, 0x6E, 0x69, 0x74, 0x79, 0x46, 0x53, 0x00 }))
            {
                throw new UnityException("Not a UnityFS file (signature = " + ToPrintable(signature) + ")");
            }

            uint fileVersion = reader.U32();
            if (fileVersion != SupportedFileVersion)
            {
                throw new UnityException("Unsupported UnityFS version: " + fileVersion);
            }
            this.UnityVersion = reader.CString();
            this.UnityRevision = reader.CString();            long bundleSize = reader.I64();
            if (bundleSize < 0 || bundleSize > fileLength)
            {
                throw new UnityException(string.Format(
                    "Invalid bundle size: {0} (file is {1} bytes)", bundleSize, fileLength));
            }

            long compressedInfoSize = reader.U32();
            long uncompressedInfoSize = reader.U32();
            if (compressedInfoSize == 0 || compressedInfoSize > MaxBlocksInfoSize
                || uncompressedInfoSize == 0 || uncompressedInfoSize > MaxBlocksInfoSize)
            {
                throw new UnityException(string.Format(
                    "Invalid blocksInfo size: compressed={0}, uncompressed={1}",
                    compressedInfoSize, uncompressedInfoSize));
            }

            uint flags = reader.U32();
            long headerEnd = AlignUp(reader.Position, 16);
            if (headerEnd > bundleSize)
            {
                throw new UnityException("The bundle header exceeds the bundle size.");
            }

            long blocksInfoPosition = (flags & FlagBlocksInfoAtEnd) != 0
                ? bundleSize - compressedInfoSize
                : headerEnd;
            long blocksInfoEnd = blocksInfoPosition + compressedInfoSize;
            if (blocksInfoPosition < headerEnd || blocksInfoEnd > bundleSize)
            {
                throw new UnityException(string.Format(
                    "blocksInfo range out of range: {0}..{1}, bundle={2}", blocksInfoPosition, blocksInfoEnd, bundleSize));
            }

            byte[] compressedInfo = new byte[compressedInfoSize];
            this.source.ReadAt(blocksInfoPosition, compressedInfo, 0, compressedInfo.Length);
            byte[] info;
            switch (flags & 0x3F)
            {
                case 0:
                    if (compressedInfo.Length != uncompressedInfoSize)
                    {
                        throw new UnityException(string.Format(
                            "Raw blocksInfo size mismatch: {0} != {1}", compressedInfo.Length, uncompressedInfoSize));
                    }
                    info = compressedInfo;
                    break;

                case 2:
                case 3:
                    info = new byte[uncompressedInfoSize];
                    if (LZ4Codec.Decode(compressedInfo, 0, compressedInfo.Length, info, 0, info.Length) < 0)
                    {
                        throw new UnityException("LZ4 decompression failed: blocksInfo");
                    }
                    break;

                default:
                    throw new UnityException("Unsupported blocksInfo compression: " + (flags & 0x3F));
            }

            this.ParseBlocksInfo(info, out long decompressedDataSize);

            // ── data area start: the 0x200 flag requires align(16) ──
            long dataBase = (flags & FlagBlocksInfoAtEnd) != 0
                ? headerEnd
                : headerEnd + compressedInfoSize;
            long dataStart = (flags & FlagBlockInfoNeedPaddingAtStart) != 0
                ? AlignUp(dataBase, 16)
                : dataBase;

            long filePosition = dataStart;
            long decompressedPosition = 0;
            this.blockDataPrefix.Add(0);
            for (int i = 0; i < this.blocks.Count; i++)
            {
                BundleBlock block = this.blocks[i];
                this.blockFileOffsets.Add(filePosition);
                filePosition += block.CompressedSize;
                decompressedPosition += block.UncompressedSize;
                this.blockDataPrefix.Add(decompressedPosition);
            }

            long dataLimit = (flags & FlagBlocksInfoAtEnd) != 0 ? blocksInfoPosition : bundleSize;
            if (dataStart > dataLimit || filePosition > dataLimit)
            {
                throw new UnityException(string.Format(
                    "Block data area out of range: {0}..{1}, limit={2}", dataStart, filePosition, dataLimit));
            }
            if (decompressedPosition != decompressedDataSize)
            {
                throw new UnityException("The decompressed block size does not match.");
            }

            foreach (BundleNode node in this.nodes)
            {
                if (node.Offset > decompressedPosition - node.Size)
                {
                    throw new UnityException(string.Format(
                        "node {0} range {1}..{2} exceeds the decompressed data size {3}",
                        node.Name, node.Offset, node.Offset + node.Size, decompressedPosition));
                }
            }
        }

        private void ParseBlocksInfo(byte[] info, out long decompressedDataSize)
        {
            UnityByteReader reader = new UnityByteReader(info, false);
            reader.Skip(16); // hash

            uint blockCountRaw = reader.U32();
            if (blockCountRaw == 0 || blockCountRaw > MaxBlockCount
                || blockCountRaw > (uint)(reader.Remaining / 10))
            {
                throw new UnityException("Invalid block count: " + blockCountRaw);
            }
            int blockCount = (int)blockCountRaw;

            for (int i = 0; i < blockCount; i++)
            {
                uint uncompressedSize = reader.U32();
                uint compressedSize = reader.U32();
                ushort blockFlags = reader.U16();
                if (uncompressedSize == 0 || compressedSize == 0
                    || uncompressedSize > MaxBlockSize || compressedSize > MaxBlockSize)
                {
                    throw new UnityException(string.Format(
                        "Invalid block size: compressed={0}, uncompressed={1}", compressedSize, uncompressedSize));
                }
                switch (blockFlags & 0x3F)
                {
                    case 0:
                        if (compressedSize != uncompressedSize)
                        {
                            throw new UnityException(string.Format(
                                "Raw block size mismatch: {0} != {1}", compressedSize, uncompressedSize));
                        }
                        break;
                    case 2:
                    case 3:
                        break;
                    default:
                        throw new UnityException(string.Format(
                            "Unsupported block compression: {0} (only raw / LZ4 / LZ4HC are supported)", blockFlags & 0x3F));
                }
                this.blocks.Add(new BundleBlock(uncompressedSize, compressedSize, blockFlags));
            }

            uint nodeCountRaw = reader.U32();
            if (nodeCountRaw > MaxNodeCount || nodeCountRaw > (uint)(reader.Remaining / 21))
            {
                throw new UnityException("Invalid node count: " + nodeCountRaw);
            }
            int nodeCount = (int)nodeCountRaw;
            for (int i = 0; i < nodeCount; i++)
            {
                long offset = reader.I64();
                long size = reader.I64();
                if (offset < 0 || size < 0)
                {
                    throw new UnityException(string.Format(
                        "node offset/size is negative: offset={0}, size={1}", offset, size));
                }
                uint nodeFlags = reader.U32();
                string name = reader.CString();
                this.nodes.Add(new BundleNode(offset, size, nodeFlags, name));
            }

            decompressedDataSize = 0;
            foreach (BundleBlock block in this.blocks)
            {
                decompressedDataSize += block.UncompressedSize;
            }
        }

        private byte[] DecompressBlock(int index)
        {
            lock (this.cacheLock)
            {
                byte[] cached;
                if (this.blockCache.TryGetValue(index, out cached))
                {
                    this.blockCacheOrder.Remove(index);
                    this.blockCacheOrder.Add(index);
                    return cached;
                }
            }

            if (index < 0 || index >= this.blocks.Count)
            {
                throw new UnityException("block index out of range: " + index);
            }
            BundleBlock block = this.blocks[index];
            byte[] raw = new byte[block.CompressedSize];
            this.source.ReadAt(this.blockFileOffsets[index], raw, 0, raw.Length);

            byte[] data;
            switch (block.Flags & 0x3F)
            {
                case 0:
                    data = raw;
                    break;

                case 2:
                case 3:
                    data = new byte[block.UncompressedSize];
                    if (LZ4Codec.Decode(raw, 0, raw.Length, data, 0, data.Length) < 0)
                    {
                        throw new UnityException("LZ4 decompression failed: block " + index);
                    }
                    break;

                default:
                    throw new UnityException("Unsupported block compression: " + (block.Flags & 0x3F));
            }

            lock (this.cacheLock)
            {
                if (this.blockCache.ContainsKey(index))
                {
                    return this.blockCache[index];
                }
                if (this.blockCacheOrder.Count >= BlockCacheCapacity)
                {
                    int evict = this.blockCacheOrder[0];
                    this.blockCacheOrder.RemoveAt(0);
                    this.blockCache.Remove(evict);
                }
                this.blockCache[index] = data;
                this.blockCacheOrder.Add(index);
            }
            return data;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static string ToPrintable(byte[] bytes)
        {
            char[] chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i] = bytes[i] >= 0x20 && bytes[i] < 0x7F ? (char)bytes[i] : '?';
            }
            return new string(chars);
        }

        private sealed class BundleBlock
        {
            public BundleBlock(uint uncompressedSize, uint compressedSize, ushort flags)
            {
                this.UncompressedSize = uncompressedSize;
                this.CompressedSize = compressedSize;
                this.Flags = flags;
            }

            public uint UncompressedSize { get; private set; }
            public uint CompressedSize { get; private set; }
            public ushort Flags { get; private set; }
        }

        /// <summary>A node inside a bundle (a SerializedFile or an asset file such as .resS).</summary>
        public sealed class BundleNode
        {
            internal BundleNode(long offset, long size, uint flags, string name)
            {
                this.Offset = offset;
                this.Size = size;
                this.Flags = flags;
                this.Name = name;
            }

            /// <summary>Offset relative to the start of the decompressed data area.</summary>
            public long Offset { get; private set; }

            public long Size { get; private set; }

            public uint Flags { get; private set; }

            public string Name { get; private set; }

            /// <summary>node flags bit 2 (0x4): this node is a SerializedFile (otherwise an asset file such as .resS).</summary>
            public bool IsSerializedFile
            {
                get { return (this.Flags & 4) != 0; }
            }
        }
    }
}

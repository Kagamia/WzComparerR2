using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.UnityLib.Wzjs
{
    /// <summary>
    /// WZJS v5 decoder (settled by reverse engineering).
    /// <para>Input: raw MonoBehaviour payload bytes; output: a <see cref="WzjsNode"/> node tree.</para>
    /// <para>
    /// File layout (all little-endian):
    /// [0..4) nodeCount u32; [4..8) constant 8; [8..144) 12 section descriptors
    /// (count u32 + offset u32 each), [144..148) magic "WZJS", [148..152) version (4 or 5),
    /// [152..) node records nodeCount × 32 bytes, followed by the section data, the child order table
    /// and string pools A/B/C (every offset is relative to the magic position 144).
    /// </para>
    /// <para>
    /// The earliest Classic beta builds use version 4; the field layout matches 5 and only the version number differs.
    /// </para>
    /// <para>
    /// section slots: slot0 = child order table (u32 array), slot1 = u8 for type 3,
    /// slot3 = u16 for type 5, slot4 = i32 for type 6, slot5 = i64 for type 7,
    /// slot6 = 8 bytes per entry for type 8 (2×f32), slot7 = 8 bytes per entry for type 14,
    /// slot8 = f64 for type 9; slots 2/9/10/11 are empty in every sample.
    /// </para>
    /// <para>
    /// Tail of the header (idx26..35): idx26..28 = string pool A (node names), idx29..31 = pool B (node paths),
    /// idx32..34 = pool C (type 12 string values), idx35 = total payload length - 144.
    /// A string pool is a data blob plus a table of (count+1) u32 offsets; string i = blob[off[i]..off[i+1]].
    /// </para>
    /// <para>
    /// Node record (32 bytes = 8 × i32): f0 type tag, f1 name index (pool A),
    /// f2 value (section index / inline int / pool C index / -1),
    /// f3 start of the contiguous child node index range, f4 child count, f5 parent node index (-1 = root),
    /// f6 path index (pool B), f7 start of the child order table.
    /// Child indices occupy the contiguous range [f3, f3+f4); the child order table gives the display order.
    /// </para>
    /// </summary>
    public static class WzjsImage
    {
        public const int TypeSubTree = 2;
        public const int TypeUol = 12;
        public const int TypeCanvas = 18;
        public const int TypeCanvasLinked = 19;

        /// <summary>Fixed position of the magic within the payload.</summary>
        public const int MagicOffset = 144;

        /// <summary>Number of header u32 values.</summary>
        public const int HeaderU32Count = 36;

        /// <summary>Size of a node record.</summary>
        public const int RecordSize = 32;

        private const int MaxPoolStringCount = 1000000;

        public static bool IsCanvasTag(int typeTag)
        {
            return typeTag == TypeCanvas || typeTag == TypeCanvasLinked;
        }

        /// <summary>Parses the entire payload and returns the root node (the whole tree).</summary>
        public static WzjsNode Parse(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (data.Length < 152)
            {
                throw new WzjsException("The data is too short: " + data.Length + " bytes (152 required at minimum)");
            }
            if (!BytesEqual(data, MagicOffset, "WZJS"))
            {
                throw new WzjsException(string.Format(
                    "Magic mismatch: {0} at offset 144 (WZJS expected)",
                    Encoding.ASCII.GetString(data, MagicOffset, 4)));
            }
            uint version = U32At(data, 148);
            if (version != 4 && version != 5)
            {
                throw new WzjsException("Unsupported version: " + version);
            }

            uint[] header = new uint[HeaderU32Count];
            for (int i = 0; i < header.Length; i++)
            {
                header[i] = U32At(data, i * 4);
            }
            if (header[1] != 8)
            {
                throw new WzjsException(string.Format(
                    "Invalid header idx1 constant: {0} (8 expected)", header[1]));
            }
            long nodeCountLong = header[0];
            if (nodeCountLong == 0)
            {
                throw new WzjsException("nodeCount must not be 0.");
            }
            long recordBytes = nodeCountLong * RecordSize;
            if (recordBytes > data.Length - 152)
            {
                throw new WzjsException(string.Format(
                    "Data truncated: {0} bytes required at offset 152, but the file is only {1} bytes", recordBytes, data.Length));
            }
            int nodeCount = (int)nodeCountLong;

            // 12 section slots: (count, offset) = (idx2, idx3), (idx4, idx5), ...
            int[] sectionCounts = new int[12];
            long[] sectionOffsets = new long[12];
            for (int slot = 0; slot < 12; slot++)
            {
                long count = header[2 + slot * 2];
                if (count > data.Length)
                {
                    throw new WzjsException(string.Format(
                        "Invalid slot{0} count: {1} (payload {2} bytes)", slot, count, data.Length));
                }
                sectionCounts[slot] = (int)count;
                sectionOffsets[slot] = MagicOffset + (long)header[3 + slot * 2];
            }
            foreach (int slot in new[] { 2, 9, 10, 11 })
            {
                if (sectionCounts[slot] != 0)
                {
                    throw new WzjsException(string.Format(
                        "slot{0} should be empty but count = {1}", slot, sectionCounts[slot]));
                }
            }
            if ((long)header[35] + MagicOffset != data.Length)
            {
                throw new WzjsException(string.Format(
                    "Payload length mismatch: idx35={0} + 144 != {1}", header[35], data.Length));
            }

            int[] sectionEntrySizes = new int[12];
            sectionEntrySizes[0] = 4;
            sectionEntrySizes[1] = 1;
            sectionEntrySizes[3] = 2;
            sectionEntrySizes[4] = 4;
            sectionEntrySizes[5] = 8;
            sectionEntrySizes[6] = 8;
            sectionEntrySizes[7] = 8;
            sectionEntrySizes[8] = 8;
            for (int slot = 0; slot < 12; slot++)
            {
                if (sectionEntrySizes[slot] == 0 && sectionCounts[slot] != 0)
                {
                    throw new WzjsException(string.Format("slot{0} must not contain data.", slot));
                }
                long bytes = (long)sectionCounts[slot] * sectionEntrySizes[slot];
                long end = sectionOffsets[slot] + bytes;
                if (end > data.Length)
                {
                    throw new WzjsException(string.Format(
                        "Data truncated: slot{0} requires {1} bytes at offset {2}, but the file is only {3} bytes",
                        slot, bytes, sectionOffsets[slot], data.Length));
                }
            }

            // ── String pools ──
            string[] poolA = ReadPool(data, header[26], header[27], header[28]);
            string[] poolB = ReadPool(data, header[29], header[30], header[31]);
            string[] poolC = ReadPool(data, header[32], header[33], header[34]);

            // ── Child order table (slot0) ──
            int[] childTable = new int[sectionCounts[0]];
            for (int i = 0; i < childTable.Length; i++)
            {
                childTable[i] = checked((int)U32At(data, checked((int)(sectionOffsets[0] + i * 4L))));
            }

            // ── Node records ──
            long recordStart = 152;
            Func<int, int, int> record = (node, field) =>
                I32At(data, checked((int)(recordStart + (long)node * RecordSize + field * 4L)));

            // ── Child indices must be greater than the parent index (required by reverse-order tree building); validate first ──
            for (int i = 0; i < nodeCount; i++)
            {
                int childCount = record(i, 4);
                if (childCount < 0)
                {
                    throw new WzjsException(string.Format("node {0} has a negative childCount: {1}", i, childCount));
                }
                if (childCount == 0)
                {
                    continue;
                }
                int tableStart = record(i, 7);
                if (tableStart < 0 || tableStart > childTable.Length - childCount)
                {
                    throw new WzjsException(string.Format(
                        "node {0} child table [{1}..+{2}) out of range (table length {3})",
                        i, tableStart, childCount, childTable.Length));
                }
                for (int k = 0; k < childCount; k++)
                {
                    int child = childTable[tableStart + k];
                    if (child < 0 || child >= nodeCount)
                    {
                        throw new WzjsException(string.Format(
                            "node {0} child index {1} out of range (total {2})", i, child, nodeCount));
                    }
                    if (child <= i)
                    {
                        throw new WzjsException(string.Format(
                            "node {0} child index {1} is not greater than the parent node", i, child));
                    }
                    if (record(child, 5) != i)
                    {
                        throw new WzjsException(string.Format(
                            "node {0} declares child {1}, but its parent={2}", i, child, record(child, 5)));
                    }
                }
            }

            // ── Build the tree in reverse order (child indices always exceed the parent, so subtrees are complete) ──
            WzjsNode[] built = new WzjsNode[nodeCount];
            for (int i = nodeCount - 1; i >= 0; i--)
            {
                int typeTag = record(i, 0);
                int valueIndex = record(i, 2);
                int childCount = record(i, 4);
                int pathIndex = record(i, 6);
                int tableStart = record(i, 7);

                int nameIndex = record(i, 1);
                if (nameIndex < 0 || nameIndex >= poolA.Length)
                {
                    throw new WzjsException(string.Format(
                        "node {0} name index {1} out of range (pool A has {2} entries)", i, nameIndex, poolA.Length));
                }
                string name = poolA[nameIndex];

                string path = null;
                if (pathIndex >= 0)
                {
                    if (pathIndex >= poolB.Length)
                    {
                        throw new WzjsException(string.Format(
                            "node {0} path index {1} out of range (pool B has {2} entries)", i, pathIndex, poolB.Length));
                    }
                    path = poolB[pathIndex];
                }

                WzjsValue value = DecodeValue(
                    data, typeTag, valueIndex, i, sectionCounts, sectionOffsets, poolC);

                WzjsNode[] children = new WzjsNode[childCount];
                for (int k = 0; k < childCount; k++)
                {
                    int child = childTable[tableStart + k];
                    WzjsNode childNode = built[child];
                    if (childNode == null)
                    {
                        throw new WzjsException("node " + child + " is referenced twice");
                    }
                    built[child] = null;
                    children[k] = childNode;
                }

                built[i] = new WzjsNode(name, value, path, children, typeTag);
            }

            if (record(0, 5) != -1)
            {
                throw new WzjsException("node 0 is not the root node (parent != -1)");
            }
            for (int i = 1; i < nodeCount; i++)
            {
                if (built[i] != null)
                {
                    throw new WzjsException("There are nodes that are not connected to node 0.");
                }
            }
            if (built[0] == null)
            {
                throw new WzjsException("The root node is missing.");
            }
            return built[0];
        }

        private static WzjsValue DecodeValue(
            byte[] data, int typeTag, int valueIndex, int nodeIndex,
            int[] sectionCounts, long[] sectionOffsets, string[] poolC)
        {
            switch (typeTag)
            {
                case TypeSubTree:
                    return WzjsValue.SubTree;

                case TypeCanvas:
                case TypeCanvasLinked:
                    return WzjsValue.Canvas;

                case 3:
                    return WzjsValue.FromU8(ReadU8(data, sectionCounts[1], sectionOffsets[1], valueIndex));

                case 5:
                    return WzjsValue.FromU16(ReadU16(data, sectionCounts[3], sectionOffsets[3], valueIndex));

                case 6:
                    return WzjsValue.FromInt(unchecked((int)ReadU32(
                        data, sectionCounts[4], sectionOffsets[4], valueIndex)));

                case 7:
                {
                    uint lo;
                    uint hi;
                    ReadPair(data, sectionCounts[5], sectionOffsets[5], valueIndex, out lo, out hi);
                    return WzjsValue.FromLong(unchecked((long)(((ulong)hi << 32) | lo)));
                }

                case 8:
                {
                    uint a;
                    uint b;
                    ReadPair(data, sectionCounts[6], sectionOffsets[6], valueIndex, out a, out b);
                    // The format cannot distinguish 2×f32 from 2×i32 (both encodings occur under the same property name).
                    // Heuristic: if either half-word is denormal (exponent bits all zero, value non-zero), treat the pair as int bits;
                    // real game data never contains denormal floats. A full survey found only 8 int entries out of 2003.
                    bool denormal = IsDenormal(a) || IsDenormal(b);
                    return denormal
                        ? WzjsValue.FromInt2(unchecked((int)a), unchecked((int)b))
                        : WzjsValue.FromFloat2(BitConverter.ToSingle(BitConverter.GetBytes(a), 0),
                            BitConverter.ToSingle(BitConverter.GetBytes(b), 0));
                }

                case 9:
                {
                    uint lo;
                    uint hi;
                    ReadPair(data, sectionCounts[8], sectionOffsets[8], valueIndex, out lo, out hi);
                    return WzjsValue.FromDouble(BitConverter.Int64BitsToDouble(
                        unchecked((long)(((ulong)hi << 32) | lo))));
                }

                case 11:
                    return WzjsValue.FromString(ReadPoolString(poolC, valueIndex, nodeIndex));

                case TypeUol:
                    return WzjsValue.FromUol(ReadPoolString(poolC, valueIndex, nodeIndex));

                case 14:
                {
                    uint x;
                    uint y;
                    ReadPair(data, sectionCounts[7], sectionOffsets[7], valueIndex, out x, out y);
                    return WzjsValue.FromVector(
                        BitConverter.ToSingle(BitConverter.GetBytes(x), 0),
                        BitConverter.ToSingle(BitConverter.GetBytes(y), 0));
                }

                default:
                    throw new WzjsException(string.Format(
                        "Unknown type tag: {0} (node {1})", typeTag, nodeIndex));
            }
        }

        private static bool IsDenormal(uint bits)
        {
            return bits != 0 && (bits & 0x7F800000) == 0;
        }

        private static string ReadPoolString(string[] pool, int index, int nodeIndex)
        {
            if (index < 0 || index >= pool.Length)
            {
                throw new WzjsException(string.Format(
                    "node {0} pool C index {1} out of range (pool C has {2} entries)", nodeIndex, index, pool.Length));
            }
            return pool[index];
        }

        private static byte ReadU8(byte[] data, int count, long offset, int index)
        {
            if (index < 0 || index >= count)
            {
                throw new WzjsException(string.Format("slot1 index {0} out of range (count {1})", index, count));
            }
            int position = checked((int)(offset + index));
            if (position >= data.Length)
            {
                throw new WzjsException("Data truncated: slot1");
            }
            return data[position];
        }

        private static ushort ReadU16(byte[] data, int count, long offset, int index)
        {
            if (index < 0 || index >= count)
            {
                throw new WzjsException(string.Format("slot3 index {0} out of range (count {1})", index, count));
            }
            int position = checked((int)(offset + (long)index * 2));
            if (position < 0 || position > data.Length - 2)
            {
                throw new WzjsException("Data truncated: slot3");
            }
            return (ushort)(data[position] | (data[position + 1] << 8));
        }

        private static uint ReadU32(byte[] data, int count, long offset, int index)
        {
            if (index < 0 || index >= count)
            {
                throw new WzjsException(string.Format("section index {0} out of range (count {1})", index, count));
            }
            int position = checked((int)(offset + (long)index * 4));
            if (position < 0 || position > data.Length - 4)
            {
                throw new WzjsException("Data truncated: section");
            }
            return U32At(data, position);
        }

        private static void ReadPair(
            byte[] data, int count, long offset, int index, out uint first, out uint second)
        {
            if (index < 0 || index >= count)
            {
                throw new WzjsException(string.Format("section pair index {0} out of range (count {1})", index, count));
            }
            int position = checked((int)(offset + (long)index * 8));
            if (position < 0 || position > data.Length - 8)
            {
                throw new WzjsException("Data truncated: section pair");
            }
            first = U32At(data, position);
            second = U32At(data, position + 4);
        }

        /// <summary>String pool: a blob plus a table of (count+1) offsets.</summary>
        private static string[] ReadPool(byte[] data, uint count, uint blobOffset, uint tableOffset)
        {
            if (count > MaxPoolStringCount)
            {
                throw new WzjsException(string.Format(
                    "The string pool count is too large: {0} (payload {1} bytes)", count, data.Length));
            }
            long offsetsCount = (long)count + 1;
            long tableBytes = offsetsCount * 4;
            long blobPosition = MagicOffset + (long)blobOffset;
            long tablePosition = MagicOffset + (long)tableOffset;
            long tableEnd = tablePosition + tableBytes;
            if (blobPosition > data.Length || tableEnd > data.Length)
            {
                throw new WzjsException("The string pool is out of range.");
            }

            long[] offsets = new long[offsetsCount];
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i] = U32At(data, checked((int)(tablePosition + i * 4L)));
            }

            string[] output = new string[count];
            for (int i = 0; i < output.Length; i++)
            {
                long start = offsets[i];
                long end = offsets[i + 1];
                if (start > end)
                {
                    throw new WzjsException(string.Format("The string pool offsets are descending: {0} > {1}", start, end));
                }
                long absoluteStart = blobPosition + start;
                long absoluteEnd = blobPosition + end;
                if (absoluteEnd > data.Length)
                {
                    throw new WzjsException("The string pool blob is out of range.");
                }
                output[i] = Encoding.UTF8.GetString(
                    data, checked((int)absoluteStart), checked((int)(absoluteEnd - absoluteStart)));
            }
            return output;
        }

        private static uint U32At(byte[] data, int position)
        {
            if (position < 0 || position > data.Length - 4)
            {
                throw new WzjsException("Data truncated: u32 @ " + position);
            }
            return (uint)(data[position]
                | (data[position + 1] << 8)
                | (data[position + 2] << 16)
                | (data[position + 3] << 24));
        }

        private static int I32At(byte[] data, int position)
        {
            return unchecked((int)U32At(data, position));
        }

        private static bool BytesEqual(byte[] data, int position, string ascii)
        {
            if (position < 0 || position > data.Length - ascii.Length)
            {
                return false;
            }
            for (int i = 0; i < ascii.Length; i++)
            {
                if (data[position + i] != (byte)ascii[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}

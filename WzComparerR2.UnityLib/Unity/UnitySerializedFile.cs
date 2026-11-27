using System;
using System.Collections.Generic;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// SerializedFile v22 parser (header / types / objects / scriptTypes / externals).
    /// <para>
    /// v22 header layout (extension fields are always big-endian):
    /// [0..4) legacy metadataSize u32; [4..8) legacy fileSize u32; [8..12) version u32 = 22;
    /// [12..16) legacy dataOffset u32; [16] endianness u8 (0 = little) + [17..20) reserved;
    /// [20..24) metadataSize u32; [24..32) fileSize i64; [32..40) dataOffset i64;
    /// [40..48) unknown i64.
    /// </para>
    /// <para>
    /// The metadata body that follows uses the byte order specified by the endianness byte:
    /// unityVersion cstr | targetPlatform i32 | enableTypeTree u8 | types | objects |
    /// scriptTypes | externals.
    /// </para>
    /// <para>
    /// In these bundles enableTypeTree = false in practice, so types only carry basic fields such as classID and
    /// object fields are parsed entirely by the hard-coded layouts in <see cref="UnityObjectParsers"/>.
    /// </para>
    /// </summary>
    public sealed class UnitySerializedFile
    {
        public const uint SupportedVersion = 22;
        public const int HeaderSize = 48;

        private const int MaxMetadataSize = 256 * 1024 * 1024;
        private const int MaxTableEntries = 1000000;

        private readonly List<UnitySerializedType> types = new List<UnitySerializedType>();
        private readonly List<UnityObjectInfo> objects = new List<UnityObjectInfo>();
        private readonly List<UnityScriptType> scriptTypes = new List<UnityScriptType>();
        private readonly List<UnityExternal> externals = new List<UnityExternal>();

        private UnitySerializedFile()
        {
        }

        public string UnityVersion { get; private set; }

        public int TargetPlatform { get; private set; }

        public bool IsLittleEndian { get; private set; }

        /// <summary>Offset of the object data within the node (absolute object position = DataOffset + ByteStart).</summary>
        public long DataOffset { get; private set; }

        public IReadOnlyList<UnitySerializedType> Types
        {
            get { return this.types; }
        }

        public IReadOnlyList<UnityObjectInfo> Objects
        {
            get { return this.objects; }
        }

        public IReadOnlyList<UnityScriptType> ScriptTypes
        {
            get { return this.scriptTypes; }
        }

        public IReadOnlyList<UnityExternal> Externals
        {
            get { return this.externals; }
        }

        /// <summary>
        /// Reads the first 48 bytes of the node and returns the total length of the header + metadata,
        /// which the caller uses to decide how many bytes to feed to <see cref="Parse"/>.
        /// </summary>
        public static int GetMetadataLength(byte[] head)
        {
            if (head == null)
            {
                throw new ArgumentNullException(nameof(head));
            }
            if (head.Length < HeaderSize)
            {
                throw new UnityException(string.Format(
                    "SerializedFile header is too short: {0} bytes (48 required)", head.Length));
            }
            UnityByteReader reader = new UnityByteReader(head, false);
            reader.Skip(8);
            uint version = reader.U32();
            if (version != SupportedVersion)
            {
                throw new UnityException("Not a SerializedFile (version = " + version + ")");
            }
            reader.Skip(8); // legacy dataOffset + endianness + reserved
            uint metadataSize = reader.U32();
            if (metadataSize > MaxMetadataSize)
            {
                throw new UnityException(string.Format(
                    "metadataSize exceeds the limit: {0} > {1}", metadataSize, MaxMetadataSize));
            }
            return checked(HeaderSize + (int)metadataSize);
        }

        /// <summary>
        /// Parses from the bytes at the start of the node. The data must cover the header and all metadata
        /// (the caller first uses <see cref="GetMetadataLength"/> to decide how much to read).
        /// </summary>
        public static UnitySerializedFile Parse(byte[] head)
        {
            if (head == null)
            {
                throw new ArgumentNullException(nameof(head));
            }
            if (head.Length < HeaderSize)
            {
                throw new UnityException(string.Format(
                    "SerializedFile header is too short: {0} bytes (48 required)", head.Length));
            }

            UnityByteReader headerReader = new UnityByteReader(head, 0, HeaderSize, false);
            headerReader.U32(); // legacy metadataSize
            headerReader.U32(); // legacy fileSize
            uint version = headerReader.U32();
            if (version != SupportedVersion)
            {
                throw new UnityException("Not a SerializedFile (version = " + version + ")");
            }
            headerReader.U32(); // legacy dataOffset
            byte endianness = headerReader.U8();
            if (endianness > 1)
            {
                throw new UnityException("Invalid endianness value: " + endianness);
            }
            headerReader.Skip(3); // reserved
            uint metadataSize = headerReader.U32();
            if (metadataSize > MaxMetadataSize || metadataSize > head.Length - HeaderSize)
            {
                throw new UnityException(string.Format(
                    "Invalid metadataSize: {0} ({1} bytes available)", metadataSize, head.Length - HeaderSize));
            }
            long fileSize = headerReader.I64();
            if (fileSize < 0)
            {
                throw new UnityException("fileSize is negative: " + fileSize);
            }
            long dataOffset = headerReader.I64();
            if (dataOffset < 0)
            {
                throw new UnityException("dataOffset is negative: " + dataOffset);
            }
            if (fileSize != 0 && dataOffset > fileSize)
            {
                throw new UnityException(string.Format(
                    "dataOffset {0} exceeds fileSize {1}", dataOffset, fileSize));
            }
            headerReader.I64(); // unknown

            UnitySerializedFile file = new UnitySerializedFile();
            file.IsLittleEndian = endianness == 0;
            file.DataOffset = dataOffset;

            UnityByteReader reader = new UnityByteReader(head, HeaderSize, (int)metadataSize, file.IsLittleEndian);
            file.UnityVersion = reader.CString();
            file.TargetPlatform = reader.I32();
            bool enableTypeTree = reader.Bool();

            int typeCount = ReadTableCount(reader, "types", 23);
            for (int i = 0; i < typeCount; i++)
            {
                int classId = reader.I32();
                bool isStripped = reader.Bool();       // v16+
                short scriptTypeIndex = reader.I16();  // v17+
                if (classId == 114)
                {
                    reader.Skip(16); // scriptID hash (MonoBehaviour)
                }
                reader.Skip(16); // oldTypeHash
                if (enableTypeTree)
                {
                    // Older clients (such as the very early Classic beta) keep a complete type tree.
                    // This project does not use its contents, so skipping it correctly is enough.
                    SkipTypeTree(reader, version);
                }
                file.types.Add(new UnitySerializedType(classId, isStripped, scriptTypeIndex));
            }

            int objectCount = ReadTableCount(reader, "objects", 24);
            for (int i = 0; i < objectCount; i++)
            {
                reader.Align4(); // v14+
                long pathId = reader.I64();
                long byteStart = reader.I64(); // i64 in v22+
                if (byteStart < 0)
                {
                    throw new UnityException(string.Format(
                        "object {0} has a negative byteStart: {1}", pathId, byteStart));
                }
                uint byteSize = reader.U32();
                int typeIndex = reader.I32(); // index into the types table in v16+
                if (typeIndex < 0 || typeIndex >= file.Types.Count)
                {
                    throw new UnityException("object type index out of range: " + typeIndex);
                }
                UnitySerializedType type = file.types[typeIndex];
                if (fileSize != 0)
                {
                    long objectEnd = dataOffset + byteStart + byteSize;
                    if (objectEnd < dataOffset || objectEnd > fileSize)
                    {
                        throw new UnityException(string.Format(
                            "object {0} ends at {1}, which exceeds fileSize {2}", pathId, objectEnd, fileSize));
                    }
                }
                file.objects.Add(new UnityObjectInfo(
                    pathId, byteStart, byteSize, typeIndex, type.ClassId));
            }

            int scriptCount = ReadTableCount(reader, "scriptTypes", 12);
            for (int i = 0; i < scriptCount; i++)
            {
                int localSerializedFileIndex = reader.I32();
                reader.Align4();
                long localIdentifierInFile = reader.I64();
                file.scriptTypes.Add(new UnityScriptType(localSerializedFileIndex, localIdentifierInFile));
            }

            int externalCount = ReadTableCount(reader, "externals", 22);
            for (int i = 0; i < externalCount; i++)
            {
                reader.CString(); // tempEmpty (v6+)
                byte[] guid = reader.Take(16);
                int type = reader.I32();
                string pathName = reader.CString();
                file.externals.Add(new UnityExternal(guid, type, pathName));
            }
            // A userInformation cstring may follow, but this project does not need it, so it is not parsed.
            return file;
        }

        /// <summary>Filters objects by classID.</summary>
        public IEnumerable<UnityObjectInfo> ObjectsByClass(int classId)
        {
            foreach (UnityObjectInfo obj in this.Objects)
            {
                if (obj.ClassId == classId)
                {
                    yield return obj;
                }
            }
        }

        private static int ReadTableCount(UnityByteReader reader, string label, int minEntrySize)        {
            int count = reader.I32();
            if (count < 0 || count > MaxTableEntries)
            {
                throw new UnityException(string.Format("Invalid {0} count: {1}", label, count));
            }
            if (minEntrySize != 0 && count > reader.Remaining / minEntrySize)
            {
                throw new UnityException(string.Format(
                    "{0} count exceeds the remaining metadata space: {1} ({2} bytes remaining)",
                    label, count, reader.Remaining));
            }
            return count;
        }

        /// <summary>
        /// Skips a single type tree blob (SerializedFile v13+ layout):
        /// nodeCount u32 | stringBufferSize u32 | nodes[nodeCount × 32] | stringBuffer |
        /// (v21+) refTypes[refCount × 12].
        /// </summary>
        private static void SkipTypeTree(UnityByteReader reader, uint version)
        {
            const int TypeTreeNodeSize = 32;
            const int MaxTypeTreeNodes = 200000;

            int nodeCount = reader.I32();
            if (nodeCount < 0 || nodeCount > MaxTypeTreeNodes)
            {
                throw new UnityException("Invalid type tree node count: " + nodeCount);
            }
            int stringBufferSize = reader.I32();
            if (stringBufferSize < 0)
            {
                throw new UnityException("Invalid type tree string buffer size: " + stringBufferSize);
            }
            long blobSize = (long)nodeCount * TypeTreeNodeSize + stringBufferSize;
            if (blobSize > reader.Remaining)
            {
                throw new UnityException(string.Format(
                    "The type tree exceeds the remaining metadata space: {0} > {1}", blobSize, reader.Remaining));
            }
            reader.Skip((int)blobSize);

            if (version >= 21)
            {
                int refCount = reader.I32();
                if (refCount < 0 || refCount > MaxTableEntries)
                {
                    throw new UnityException("Invalid type tree refType count: " + refCount);
                }
                long refBytes = (long)refCount * 12;
                if (refBytes > reader.Remaining)
                {
                    throw new UnityException("The type tree refType exceeds the remaining metadata space.");
                }
                reader.Skip((int)refBytes);
            }
        }
    }

    public sealed class UnitySerializedType
    {
        internal UnitySerializedType(int classId, bool isStripped, short scriptTypeIndex)
        {
            this.ClassId = classId;
            this.IsStripped = isStripped;
            this.ScriptTypeIndex = scriptTypeIndex;
        }

        public int ClassId { get; private set; }
        public bool IsStripped { get; private set; }
        public short ScriptTypeIndex { get; private set; }
    }

    public sealed class UnityObjectInfo
    {
        internal UnityObjectInfo(long pathId, long byteStart, uint byteSize, int typeIndex, int classId)
        {
            this.PathId = pathId;
            this.ByteStart = byteStart;
            this.ByteSize = byteSize;
            this.TypeIndex = typeIndex;
            this.ClassId = classId;
        }

        public long PathId { get; private set; }

        /// <summary>Offset relative to dataOffset.</summary>
        public long ByteStart { get; private set; }

        public uint ByteSize { get; private set; }

        /// <summary>Index into the types table.</summary>
        public int TypeIndex { get; private set; }

        public int ClassId { get; private set; }
    }

    public sealed class UnityScriptType
    {
        internal UnityScriptType(int localSerializedFileIndex, long localIdentifierInFile)
        {
            this.LocalSerializedFileIndex = localSerializedFileIndex;
            this.LocalIdentifierInFile = localIdentifierInFile;
        }

        public int LocalSerializedFileIndex { get; private set; }
        public long LocalIdentifierInFile { get; private set; }
    }

    public sealed class UnityExternal
    {
        internal UnityExternal(byte[] guid, int type, string pathName)
        {
            this.Guid = guid;
            this.Type = type;
            this.PathName = pathName;
        }

        public byte[] Guid { get; private set; }
        public int Type { get; private set; }
        public string PathName { get; private set; }
    }
}

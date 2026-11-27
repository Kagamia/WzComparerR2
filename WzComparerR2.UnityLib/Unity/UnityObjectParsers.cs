using System;
using System.Collections.Generic;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// Unity classID constants.
    /// </summary>
    public static class UnityClassIds
    {
        public const int Texture2D = 28;
        public const int TextAsset = 49;
        public const int AudioClip = 83;
        public const int MonoBehaviour = 114;
        public const int MonoScript = 115;
        public const int PlayerSettings = 129;
        public const int BuildSettings = 141;
        public const int AssetBundle = 142;
        public const int Sprite = 213;
        public const int SpriteAtlas = 687078895;
    }

    /// <summary>
    /// Hard-coded object layouts for Unity 6000.3.16f1.
    /// <para>
    /// The field order follows AssetRipper/TypeTreeDumps
    /// <c>StructsDump/release/6000.3.16f1.dump</c> (the official typetree dump).
    /// Note that the 6000.3 Texture2D has no <c>m_ForcedFallbackFormat</c> /
    /// <c>m_DownscaleFallback</c>, and <c>m_IsAlphaChannelOptional</c> comes right after
    /// <c>m_Name</c>.
    /// </para>
    /// <para>
    /// Every parse function here takes the complete bytes of a single object (byte_size long).
    /// </para>
    /// </summary>
    public static class UnityObjectParsers
    {
        // ── TextAsset(49) ──────────────────────────────────────────────

        public static UnityTextAsset ParseTextAsset(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();
            int length = checked((int)reader.U32());
            if (length < 0 || length > reader.Remaining)
            {
                throw new UnityException(string.Format(
                    "TextAsset {0} has an invalid m_Script length: {1}", name, length));
            }
            return new UnityTextAsset(name, reader.Take(length));
        }

        // ── AudioClip(83) ──────────────────────────────────────────────

        /// <summary>
        /// m_Name → 4×i32 → f32 → bool+align → i32 → 3×bool+align →
        /// StreamedResource(m_Source aligned string, m_Offset u64, m_Size u64).
        /// </summary>
        public static UnityAudioClipInfo ParseAudioClip(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();
            reader.I32(); // m_LoadType
            int channels = reader.I32();
            int frequency = reader.I32();
            reader.I32(); // m_BitsPerSample
            float length = reader.F32();
            reader.Bool(); // m_IsTrackerFormat
            reader.Align4();
            reader.I32(); // m_SubsoundIndex
            reader.Bool(); // m_PreloadAudioData
            reader.Bool(); // m_LoadInBackground
            reader.Bool(); // m_Legacy3D
            reader.Align4();
            string source = reader.AlignedString();
            ulong resourceOffset = reader.U64();
            ulong resourceSize = reader.U64();
            if (channels <= 0 || channels > 8 || frequency <= 0)
            {
                throw new UnityException(string.Format(
                    "AudioClip {0} has invalid parameters: channels={1} frequency={2}", name, channels, frequency));
            }
            string resourceNode = string.Empty;
            if (!string.IsNullOrEmpty(source))
            {
                string[] parts = source.Split('/');
                resourceNode = parts[parts.Length - 1];
            }
            if (resourceNode.Length == 0)
            {
                throw new UnityException("AudioClip " + name + " m_Source is empty");
            }
            return new UnityAudioClipInfo(
                name, channels, frequency, length, resourceNode, resourceOffset, resourceSize);
        }

        // ── MonoScript(115) ────────────────────────────────────────────

        public static UnityMonoScript ParseMonoScript(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();
            reader.I32(); // m_ExecutionOrder
            reader.Skip(16); // m_PropertiesHash
            string className = reader.AlignedString();
            string @namespace = reader.AlignedString();
            string assemblyName = reader.AlignedString();
            return new UnityMonoScript(name, className, @namespace, assemblyName);
        }

        // ── AssetBundle(142) ───────────────────────────────────────────

        /// <summary>Parses only up to m_Container (the later m_MainAsset / m_SceneHashes and so on are unused).</summary>
        public static UnityAssetBundleInfo ParseAssetBundle(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();

            int preloadCount = reader.I32();
            if (preloadCount < 0 || preloadCount > 1000000 || preloadCount > reader.Remaining / 12)
            {
                throw new UnityException("Invalid m_PreloadTable count: " + preloadCount);
            }
            for (int i = 0; i < preloadCount; i++)
            {
                ReadPPtr(reader);
            }

            int containerCount = reader.I32();
            if (containerCount < 0 || containerCount > 1000000 || containerCount > reader.Remaining / 24)
            {
                throw new UnityException("Invalid m_Container count: " + containerCount);
            }
            List<UnityContainerEntry> container = new List<UnityContainerEntry>(containerCount);
            for (int i = 0; i < containerCount; i++)
            {
                string path = reader.AlignedString();
                int preloadIndex = reader.I32();
                int preloadSize = reader.I32();
                UnityPPtr asset = ReadPPtr(reader);
                container.Add(new UnityContainerEntry(path, preloadIndex, preloadSize, asset));
            }
            return new UnityAssetBundleInfo(name, container);
        }

        // ── Texture2D(28) ──────────────────────────────────────────────

        public static UnityTexture2DInfo ParseTexture2D(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();
            reader.Bool(); // m_IsAlphaChannelOptional (MetaFlag align)
            reader.Align4();
            int width = reader.I32();
            int height = reader.I32();
            if (width <= 0 || height <= 0 || width > 32768 || height > 32768)
            {
                throw new UnityException(string.Format(
                    "Texture2D {0} has an invalid size: {1}x{2}", name, width, height));
            }
            uint completeImageSize = reader.U32();
            int mipsStripped = reader.I32();
            int textureFormat = reader.I32();
            int mipCount = reader.I32();
            reader.Bool(); // m_IsReadable
            reader.Bool(); // m_IsPreProcessed
            reader.Bool(); // m_IgnoreMipmapLimit (MetaFlag align)
            reader.Align4();
            reader.AlignedString(); // m_MipmapLimitGroupName
            reader.Bool(); // m_StreamingMipmaps (MetaFlag align)
            reader.Align4();
            reader.I32(); // m_StreamingMipmapsPriority
            reader.I32(); // m_ImageCount
            int textureDimension = reader.I32();

            // GLTextureSettings
            reader.I32(); // m_FilterMode
            reader.I32(); // m_Aniso
            reader.F32(); // m_MipBias
            reader.I32(); // m_WrapU
            reader.I32(); // m_WrapV
            reader.I32(); // m_WrapW
            reader.I32(); // m_LightmapFormat
            reader.I32(); // m_ColorSpace
            reader.ByteVector(); // m_PlatformBlob

            // image data (TypelessData: u32 size + raw bytes)
            uint imageDataSize = reader.U32();
            int imageDataOffset = reader.Position;
            if (imageDataSize > reader.Remaining)
            {
                throw new UnityException(string.Format(
                    "Texture2D {0} has an invalid image data length: {1} ({2} remaining)",
                    name, imageDataSize, reader.Remaining));
            }
            reader.Skip((int)imageDataSize);
            reader.Align4(); // MetaFlag 0x4001: align after

            // m_StreamData
            ulong streamOffset = reader.U64();
            uint streamSize = reader.U32();
            string streamPath = reader.AlignedString();

            return new UnityTexture2DInfo(
                name, width, height, completeImageSize, mipsStripped, textureFormat, mipCount,
                textureDimension, imageDataSize, imageDataOffset, streamOffset, streamSize, streamPath);
        }

        // ── Sprite(213) ────────────────────────────────────────────────

        public static UnitySpriteInfo ParseSprite(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = reader.AlignedString();
            float rectX = reader.F32();
            float rectY = reader.F32();
            float rectWidth = reader.F32();
            float rectHeight = reader.F32();
            reader.Skip(8); // m_Offset Vector2f
            reader.Skip(16); // m_Border Vector4f
            reader.Skip(4); // m_PixelsToUnits
            float pivotX = reader.F32();
            float pivotY = reader.F32();
            reader.Skip(4); // m_Extrude
            reader.U8(); // m_IsPolygon
            reader.Align4();
            byte[] renderDataKeyGuid = reader.Take(16); // m_RenderDataKey.guid
            long renderDataKeyFileId = reader.I64(); // m_RenderDataKey.fileID
            int tagCount = reader.I32();
            if (tagCount < 0 || tagCount > 4096)
            {
                throw new UnityException(string.Format(
                    "Sprite {0} has an invalid m_AtlasTags count: {1}", name, tagCount));
            }
            for (int i = 0; i < tagCount; i++)
            {
                reader.AlignedString();
            }
            UnityPPtr spriteAtlas = ReadPPtr(reader); // m_SpriteAtlas
            UnityPPtr texture = ReadPPtr(reader); // m_RD.texture
            return new UnitySpriteInfo(
                name, new UnityRectF(rectX, rectY, rectWidth, rectHeight), texture, pivotX, pivotY,
                spriteAtlas, renderDataKeyGuid, renderDataKeyFileId);
        }

        // ── MonoBehaviour(114) ─────────────────────────────────────────

        /// <summary>
        /// Parses only the shared header fields; all bytes after m_Name are the custom payload
        /// (WZJS v5 assets), kept as-is for the caller to decode.
        /// </summary>
        public static UnityMonoBehaviourInfo ParseMonoBehaviour(byte[] data, bool littleEndian)
        {
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            UnityPPtr gameObject = ReadPPtr(reader);
            bool enabled = reader.Bool();
            reader.Align4();
            UnityPPtr script = ReadPPtr(reader);
            string name = reader.AlignedString();
            return new UnityMonoBehaviourInfo(gameObject, enabled, script, name, reader.Position);
        }

        private static UnityPPtr ReadPPtr(UnityByteReader reader)
        {
            return new UnityPPtr(reader.I32(), reader.I64());
        }
    }

    public struct UnityPPtr
    {
        public UnityPPtr(int fileId, long pathId)
        {
            this.FileId = fileId;
            this.PathId = pathId;
        }

        public int FileId { get; private set; }
        public long PathId { get; private set; }

        public override string ToString()
        {
            return string.Format("PPtr({0}, {1})", this.FileId, this.PathId);
        }
    }

    public struct UnityRectF
    {
        public UnityRectF(float x, float y, float width, float height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        public float X { get; private set; }
        public float Y { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
    }

    public sealed class UnityTextAsset
    {
        internal UnityTextAsset(string name, byte[] script)
        {
            this.Name = name;
            this.Script = script;
        }

        public string Name { get; private set; }

        /// <summary>Raw m_Script contents (these bundles store plain UTF-8 JSON).</summary>
        public byte[] Script { get; private set; }
    }

    public sealed class UnityAudioClipInfo
    {
        internal UnityAudioClipInfo(
            string name, int channels, int frequency, float length,
            string resourceNode, ulong resourceOffset, ulong resourceSize)
        {
            this.Name = name;
            this.Channels = channels;
            this.Frequency = frequency;
            this.Length = length;
            this.ResourceNode = resourceNode;
            this.ResourceOffset = resourceOffset;
            this.ResourceSize = resourceSize;
        }

        public string Name { get; private set; }
        public int Channels { get; private set; }
        public int Frequency { get; private set; }

        /// <summary>Length in seconds.</summary>
        public float Length { get; private set; }

        /// <summary>Name of the resource node that m_Resource points to (the last segment of m_Source).</summary>
        public string ResourceNode { get; private set; }

        /// <summary>Offset of the audio data inside the resource node.</summary>
        public ulong ResourceOffset { get; private set; }

        public ulong ResourceSize { get; private set; }
    }

    public sealed class UnityMonoScript
    {
        internal UnityMonoScript(string name, string className, string @namespace, string assemblyName)
        {
            this.Name = name;
            this.ClassName = className;
            this.Namespace = @namespace;
            this.AssemblyName = assemblyName;
        }

        public string Name { get; private set; }
        public string ClassName { get; private set; }
        public string Namespace { get; private set; }
        public string AssemblyName { get; private set; }
    }

    public sealed class UnityContainerEntry
    {
        internal UnityContainerEntry(string path, int preloadIndex, int preloadSize, UnityPPtr asset)
        {
            this.Path = path;
            this.PreloadIndex = preloadIndex;
            this.PreloadSize = preloadSize;
            this.Asset = asset;
        }

        /// <summary>Logical asset path (for example <c>assets/.../blank.json</c>).</summary>
        public string Path { get; private set; }

        public int PreloadIndex { get; private set; }
        public int PreloadSize { get; private set; }
        public UnityPPtr Asset { get; private set; }
    }

    public sealed class UnityAssetBundleInfo
    {
        internal UnityAssetBundleInfo(string name, IReadOnlyList<UnityContainerEntry> container)
        {
            this.Name = name;
            this.Container = container;
        }

        public string Name { get; private set; }
        public IReadOnlyList<UnityContainerEntry> Container { get; private set; }
    }

    public sealed class UnityTexture2DInfo
    {
        internal UnityTexture2DInfo(
            string name, int width, int height, uint completeImageSize, int mipsStripped,
            int textureFormat, int mipCount, int textureDimension, uint imageDataSize,
            int imageDataOffset, ulong streamOffset, uint streamSize, string streamPath)
        {
            this.Name = name;
            this.Width = width;
            this.Height = height;
            this.CompleteImageSize = completeImageSize;
            this.MipsStripped = mipsStripped;
            this.TextureFormat = textureFormat;
            this.MipCount = mipCount;
            this.TextureDimension = textureDimension;
            this.ImageDataSize = imageDataSize;
            this.ImageDataOffset = imageDataOffset;
            this.StreamOffset = streamOffset;
            this.StreamSize = streamSize;
            this.StreamPath = streamPath;
        }

        public string Name { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public uint CompleteImageSize { get; private set; }
        public int MipsStripped { get; private set; }
        public int TextureFormat { get; private set; }
        public int MipCount { get; private set; }
        public int TextureDimension { get; private set; }

        /// <summary>Size of the embedded image data (0 when streamed).</summary>
        public uint ImageDataSize { get; private set; }

        /// <summary>Exact start offset of the embedded image data inside the Texture2D object.</summary>
        public int ImageDataOffset { get; private set; }

        /// <summary>m_StreamData: the pixels' (offset, size, path) within the .resS node.</summary>
        public ulong StreamOffset { get; private set; }

        public uint StreamSize { get; private set; }
        public string StreamPath { get; private set; }
    }

    public sealed class UnitySpriteInfo
    {
        internal UnitySpriteInfo(string name, UnityRectF rect, UnityPPtr texture, float pivotX, float pivotY)
            : this(name, rect, texture, pivotX, pivotY, default(UnityPPtr), null, 0)
        {
        }

        internal UnitySpriteInfo(
            string name, UnityRectF rect, UnityPPtr texture, float pivotX, float pivotY,
            UnityPPtr spriteAtlas, byte[] renderDataKeyGuid, long renderDataKeyFileId)
        {
            this.Name = name;
            this.Rect = rect;
            this.Texture = texture;
            this.PivotX = pivotX;
            this.PivotY = pivotY;
            this.SpriteAtlas = spriteAtlas;
            this.RenderDataKeyGuid = renderDataKeyGuid;
            this.RenderDataKeyFileId = renderDataKeyFileId;
        }

        public string Name { get; private set; }

        /// <summary>m_Rect: (x, y, width, height).</summary>
        public UnityRectF Rect { get; private set; }

        /// <summary>m_RD.texture (the Texture2D that holds the pixels). It is 0 for atlas-packed sprites (see <see cref="SpriteAtlas"/>).</summary>
        public UnityPPtr Texture { get; private set; }

        /// <summary>m_SpriteAtlas. Non-zero only for atlas-packed sprites.</summary>
        public UnityPPtr SpriteAtlas { get; private set; }

        /// <summary>m_RenderDataKey.guid (the key used to look up the render data inside the <see cref="SpriteAtlas"/>).</summary>
        public byte[] RenderDataKeyGuid { get; private set; }

        /// <summary>m_RenderDataKey.fileID.</summary>
        public long RenderDataKeyFileId { get; private set; }

        /// <summary>m_Pivot.X (0 = left edge of the image, 1 = right edge).</summary>
        public float PivotX { get; private set; }

        /// <summary>m_Pivot.Y (0 = bottom edge of the image, 1 = top edge).</summary>
        public float PivotY { get; private set; }

        /// <summary>
        /// Gets the coordinates that correspond to the WZ <c>origin</c> (the reference point measured from the top-left of the image).
        /// Unity's pivot is anchored at the bottom-left, so Y is flipped.
        /// </summary>
        public void GetWzOrigin(out int x, out int y)
        {
            GetWzOrigin(this.Rect, out x, out y);
        }

        /// <summary>Gets the WZ <c>origin</c> for the specified rectangle (for example the crop rectangle of an atlas-packed sprite).</summary>
        public void GetWzOrigin(UnityRectF rect, out int x, out int y)
        {
            x = (int)Math.Round(rect.Width * this.PivotX, MidpointRounding.AwayFromZero);
            y = (int)Math.Round(rect.Height * (1 - this.PivotY), MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// SpriteAtlas(687078895). Sprites packed into an atlas have no <c>m_RD.texture</c>;
    /// the actual texture and crop rectangle are looked up here instead.
    /// </summary>
    public sealed class UnitySpriteAtlasInfo
    {
        private const int RenderDataOffset = 24;   // immediately after m_RenderDataKey (guid 16 + fileID 8)
        private const int TextureRectOffset = 48;  // immediately after texture PPtr(12) + alphaTexture PPtr(12)

        private readonly byte[] data;
        private readonly bool littleEndian;
        private readonly List<UnityPPtr> sprites;

        private UnitySpriteAtlasInfo(string name, byte[] data, bool littleEndian, List<UnityPPtr> sprites)
        {
            this.Name = name;
            this.data = data;
            this.littleEndian = littleEndian;
            this.sprites = sprites;
        }

        public string Name { get; private set; }

        /// <summary>m_PackedSprites (the sprites packed into the atlas, in packing order).</summary>
        public IReadOnlyList<UnityPPtr> Sprites
        {
            get { return this.sprites; }
        }

        /// <summary>
        /// Reads the name and <c>m_PackedSprites</c> from the bytes of an atlas object
        /// (the render data is looked up lazily through <see cref="TryGetRenderData"/>).
        /// </summary>
        public static UnitySpriteAtlasInfo Parse(byte[] data, bool littleEndian)
        {
            if (data == null || data.Length < 4)
            {
                return null;
            }
            UnityByteReader reader = new UnityByteReader(data, littleEndian);
            string name = null;
            List<UnityPPtr> sprites = new List<UnityPPtr>();
            try
            {
                name = reader.AlignedString();
                if (reader.Remaining >= 4)
                {
                    int count = reader.I32();
                    if (count > 0 && count <= 100000)
                    {
                        for (int i = 0; i < count && reader.Remaining >= 12; i++)
                        {
                            int fileId = reader.I32();
                            long pathId = reader.I64();
                            if (pathId != 0)
                            {
                                sprites.Add(new UnityPPtr(fileId, pathId));
                            }
                        }
                    }
                }
            }
            catch (UnityException)
            {
                // A corrupted payload is treated as name-only / an empty list
            }
            return new UnitySpriteAtlasInfo(name, data, littleEndian, sprites);
        }

        /// <summary>
        /// Gets the atlas texture and crop rectangle from a sprite's <c>m_RenderDataKey.guid</c>.
        /// The render data is laid out as [guid 16][key 8][texture 12][alphaTexture 12][rect 16]…
        /// </summary>
        public bool TryGetRenderData(byte[] guid, out UnityPPtr texture, out UnityRectF textureRect)
        {
            texture = default(UnityPPtr);
            textureRect = default(UnityRectF);
            if (guid == null || guid.Length != 16 || this.data == null)
            {
                return false;
            }

            int index = IndexOf(this.data, guid);
            while (index >= 0)
            {
                int start = index + RenderDataOffset;
                if (start + TextureRectOffset + 16 <= this.data.Length)
                {
                    UnityByteReader reader = new UnityByteReader(
                        this.data, start, this.data.Length - start, this.littleEndian);
                    UnityPPtr candidate = new UnityPPtr(reader.I32(), reader.I64());
                    reader.Skip(12); // m_RD.alphaTexture
                    float x = reader.F32();
                    float y = reader.F32();
                    float width = reader.F32();
                    float height = reader.F32();
                    if (candidate.PathId != 0 && width > 0 && height > 0)
                    {
                        texture = candidate;
                        textureRect = new UnityRectF(x, y, width, height);
                        return true;
                    }
                }
                index = index + 1 < this.data.Length ? IndexOf(this.data, guid, index + 1) : -1;
            }
            return false;
        }

        private static int IndexOf(byte[] haystack, byte[] needle, int startIndex = 0)
        {
            int last = haystack.Length - needle.Length;
            for (int i = startIndex; i <= last; i++)
            {
                int j = 0;
                while (j < needle.Length && haystack[i + j] == needle[j])
                {
                    j++;
                }
                if (j == needle.Length)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public sealed class UnityMonoBehaviourInfo
    {
        internal UnityMonoBehaviourInfo(UnityPPtr gameObject, bool enabled, UnityPPtr script, string name, int headerLength)
        {
            this.GameObject = gameObject;
            this.Enabled = enabled;
            this.Script = script;
            this.Name = name;
            this.HeaderLength = headerLength;
        }

        public UnityPPtr GameObject { get; private set; }
        public bool Enabled { get; private set; }
        public UnityPPtr Script { get; private set; }
        public string Name { get; private set; }

        /// <summary>Number of bytes occupied by the header; payload = <c>data[HeaderLength..]</c>.</summary>
        public int HeaderLength { get; private set; }
    }
}

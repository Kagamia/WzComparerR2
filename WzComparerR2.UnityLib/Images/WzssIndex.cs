using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Images
{
    /// <summary>A sprite rect in the WZSS index (coordinates are within a single page).</summary>
    public struct WzssRect : IEquatable<WzssRect>
    {
        public WzssRect(int x, int y, int width, int height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool Equals(WzssRect other)
        {
            return this.X == other.X && this.Y == other.Y
                && this.Width == other.Width && this.Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is WzssRect && this.Equals((WzssRect)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = this.X;
                hash = (hash * 397) ^ this.Y;
                hash = (hash * 397) ^ this.Width;
                hash = (hash * 397) ^ this.Height;
                return hash;
            }
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2}x{3})", this.X, this.Y, this.Width, this.Height);
        }
    }

    /// <summary>Full position of a frame in the atlas: which page + the rect within that page.</summary>
    public struct WzssSprite : IEquatable<WzssSprite>
    {
        public WzssSprite(int page, WzssRect rect)
        {
            this.Page = page;
            this.Rect = rect;
        }

        public int Page { get; private set; }
        public WzssRect Rect { get; private set; }

        public bool Equals(WzssSprite other)
        {
            return this.Page == other.Page && this.Rect.Equals(other.Rect);
        }

        public override bool Equals(object obj)
        {
            return obj is WzssSprite && this.Equals((WzssSprite)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Page * 397) ^ this.Rect.GetHashCode();
            }
        }
    }

    /// <summary>UOL → sprite frame relation explicitly stored in WZSS.</summary>
    public sealed class WzssUolLink
    {
        internal WzssUolLink(string uol, int? frameIndex)
        {
            this.Uol = uol;
            this.FrameIndex = frameIndex;
        }

        public string Uol { get; private set; }

        /// <summary><c>null</c> is the empty-image sentinel that WZSS expresses as frame_count.</summary>
        public int? FrameIndex { get; private set; }
    }

    /// <summary>One atlas page (decoded RGBA, still in Unity's bottom-up order).</summary>
    public sealed class AtlasPage
    {
        public AtlasPage(int index, int width, int height, byte[] rgba)
        {
            if (rgba == null)
            {
                throw new ArgumentNullException(nameof(rgba));
            }
            this.Index = index;
            this.Width = width;
            this.Height = height;
            this.Rgba = rgba;
        }

        /// <summary>Page number from the <c>_N</c> filename suffix, i.e. the page index in the frame records.</summary>
        public int Index { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] Rgba { get; private set; }
    }

    /// <summary>
    /// WZSS index parsing and atlas cropping.
    /// <para>
    /// The WZSS header is 20 u32 values; all offsets are relative to the magic position:
    /// [0] atlas page count; [1] constant 8; [2..5) UOL string pool (count / blobOff / tableOff);
    /// [5..7) sprite pivot (count / off, 8B/entry); [7..9) sprite rect (count / off, 16B/entry
    /// = u32 x,y,w,h); [9..11) frame (count / off, 20B/entry; the 3rd i32 is the WZSS atlas page
    /// number, the 4th is the rect index); [11..13) link (count / off, 16B/entry = slot /
    /// UOL string index (-1) / ? / frame index); [13..16) node key pool; [16..19) per-frame
    /// 64-hex pool; [19] path length, followed by the path string (= img logical path).
    /// </para>
    /// <para>
    /// From magic + 8 comes a page map of <c>page count</c> u32 values: entry i is the atlas
    /// filename <c>_N</c> page number for "WZSS page i". The two numberings often differ, so
    /// without this mapping entire pages crop the wrong image.
    /// </para>
    /// </summary>
    public sealed class WzssIndex
    {
        private const int MaxRects = 1000000;
        private const int MaxFrames = 1000000;
        private const int MaxLinks = 1000000;
        private const int MaxStrings = 1000000;

        private readonly List<WzssRect> rects;
        private readonly List<WzssUolLink> uolLinks;
        private readonly string[] nodeKeys;
        private readonly FrameEntry[] frames;
        private readonly List<KeyValuePair<int, int?>> aliasFrames;
        private readonly int[] pageMap;

        private WzssIndex(
            string path, List<WzssRect> rects, List<WzssUolLink> uolLinks, string[] nodeKeys,
            FrameEntry[] frames, List<KeyValuePair<int, int?>> aliasFrames, int[] pageMap)
        {
            this.Path = path;
            this.rects = rects;
            this.uolLinks = uolLinks;
            this.nodeKeys = nodeKeys;
            this.frames = frames;
            this.aliasFrames = aliasFrames;
            this.pageMap = pageMap;
        }

        public string Path { get; private set; }

        public IReadOnlyList<WzssRect> Rects
        {
            get { return this.rects; }
        }

        public IReadOnlyList<WzssUolLink> UolLinks
        {
            get { return this.uolLinks; }
        }

        /// <summary>
        /// Node key = <c>Guid(MD5(UTF8("&lt;img path&gt;/&lt;node relative path&gt;"))).ToString("N")</c>.
        /// The .NET <see cref="Guid(byte[])"/> convention of reversing the first 4/2/2 bytes matches the Unity data exactly.
        /// </summary>
        public static string ComputeNodeKey(string fullPath)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] digest = md5.ComputeHash(Encoding.UTF8.GetBytes(fullPath));
                return new Guid(digest).ToString("N");
            }
        }

        /// <summary>Locates the frame exactly from a path relative to the img. Returns false when the key is not found.</summary>
        public bool TryGetFrameIndexForNode(string innerPath, out int frameIndex)
        {
            frameIndex = -1;
            int slot;
            if (!this.TryFindSlot(ComputeNodeKey(this.Path + "/" + innerPath), out slot))
            {
                return false;
            }
            if (slot < this.frames.Length)
            {
                frameIndex = slot;
                return true;
            }
            for (int i = 0; i < this.aliasFrames.Count; i++)
            {
                if (this.aliasFrames[i].Key == slot)
                {
                    if (this.aliasFrames[i].Value.HasValue)
                    {
                        frameIndex = this.aliasFrames[i].Value.Value;
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        /// <summary>frame → atlas page (converted to the <c>_N</c> filename page number) + rect within that page.</summary>
        public bool TryGetSprite(int frameIndex, out WzssSprite sprite)
        {
            sprite = default(WzssSprite);
            if (frameIndex < 0 || frameIndex >= this.frames.Length)
            {
                return false;
            }
            FrameEntry entry = this.frames[frameIndex];
            if (entry.Page < 0 || entry.Page >= this.pageMap.Length
                || entry.Rect < 0 || entry.Rect >= this.rects.Count)
            {
                return false;
            }
            sprite = new WzssSprite(this.pageMap[entry.Page], this.rects[entry.Rect]);
            return true;
        }

        /// <summary>Crops the sprite from its atlas page (bottom-up RGBA) using the rect, then flips it upright.</summary>
        public static UnityRgbaImage CutSprite(IReadOnlyList<AtlasPage> atlas, WzssSprite sprite)
        {
            AtlasPage page = null;
            for (int i = 0; i < atlas.Count; i++)
            {
                if (atlas[i].Index == sprite.Page)
                {
                    page = atlas[i];
                    break;
                }
            }
            if (page == null)
            {
                throw new UnityException(string.Format(
                    "The atlas is missing page {0} (of {1})", sprite.Page, atlas.Count));
            }
            WzssRect rect = sprite.Rect;
            byte[] cropped = UnityTexture.CropRgba(
                page.Width, page.Height, page.Rgba, rect.X, rect.Y, rect.Width, rect.Height);
            UnityTexture.FlipVertical(cropped, rect.Width, rect.Height);
            return new UnityRgbaImage(rect.Width, rect.Height, cropped);
        }

        /// <summary>Parses the WZSS index MonoBehaviour payload (Unity header already stripped).</summary>
        public static WzssIndex Parse(byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            if (payload.Length < 80)
            {
                throw new UnityException("The WZSS payload is too short.");
            }
            Func<int, uint> header = index => (uint)(payload[index * 4]
                | (payload[index * 4 + 1] << 8)
                | (payload[index * 4 + 2] << 16)
                | (payload[index * 4 + 3] << 24));

            // Before the magic sit the path and a per-page GUID list (44 bytes each), which can grow past 1 KB with many pages.
            long scanLength = 128L + header(19) + (long)header(0) * 64;
            int scan = (int)Math.Min(scanLength, payload.Length);
            int magic = -1;
            for (int i = 0; i + 4 <= scan; i++)
            {
                if (payload[i] == (byte)'W' && payload[i + 1] == (byte)'Z'
                    && payload[i + 2] == (byte)'S' && payload[i + 3] == (byte)'S')
                {
                    magic = i;
                    break;
                }
            }
            if (magic < 0)
            {
                throw new UnityException("The WZSS magic is missing.");
            }

            long rectCount = header(7);
            if (rectCount > MaxRects)
            {
                throw new UnityException(string.Format("Invalid WZSS rect count: {0}", rectCount));
            }
            long rectStart = magic + (long)header(8);
            long rectEnd = rectStart + rectCount * 16;
            if (rectStart < 0 || rectEnd > payload.Length)
            {
                throw new UnityException(string.Format(
                    "WZSS rect area out of range: count={0} off={1} len={2}", rectCount, header(8), payload.Length));
            }
            List<WzssRect> rects = new List<WzssRect>((int)rectCount);
            for (int i = 0; i < rectCount; i++)
            {
                int position = checked((int)(rectStart + (long)i * 16));
                rects.Add(new WzssRect(
                    ReadI32(payload, position),
                    ReadI32(payload, position + 4),
                    ReadI32(payload, position + 8),
                    ReadI32(payload, position + 12)));
            }

            int[] pageMap = ParsePageMap(payload, magic, header);
            FrameEntry[] frames = ParseFrames(payload, magic, header, rects.Count, pageMap.Length);
            List<WzssUolLink> uolLinks;
            List<KeyValuePair<int, int?>> aliasFrames;
            ParseUolLinks(payload, magic, header, frames.Length, out uolLinks, out aliasFrames);
            string[] nodeKeys = ReadStringPool(
                payload, magic, header(13), header(14), header(15));

            long pathEnd = 80L + header(19);
            string path = pathEnd <= payload.Length
                ? Encoding.UTF8.GetString(payload, 80, (int)(pathEnd - 80))
                : string.Empty;

            return new WzssIndex(path, rects, uolLinks, nodeKeys, frames, aliasFrames, pageMap);
        }

        private bool TryFindSlot(string key, out int slot)
        {
            slot = Array.BinarySearch(this.nodeKeys, key, StringComparer.Ordinal);
            if (slot >= 0)
            {
                return true;
            }
            for (int i = 0; i < this.nodeKeys.Length; i++)
            {
                if (string.Equals(this.nodeKeys[i], key, StringComparison.Ordinal))
                {
                    slot = i;
                    return true;
                }
            }
            return false;
        }

        private static void ParseUolLinks(
            byte[] payload, int magic, Func<int, uint> header, int frameCount,
            out List<WzssUolLink> links, out List<KeyValuePair<int, int?>> aliases)
        {
            string[] strings = ReadStringPool(payload, magic, header(2), header(3), header(4));

            long recordCount = header(11);
            if (recordCount > MaxLinks)
            {
                throw new UnityException(string.Format("Invalid WZSS link count: {0}", recordCount));
            }
            long recordStart = magic + (long)header(12);
            long recordEnd = recordStart + recordCount * 16;
            if (recordStart < 0 || recordEnd > payload.Length)
            {
                throw new UnityException(string.Format(
                    "WZSS link area out of range: count={0} len={1}", recordCount, payload.Length));
            }

            links = new List<WzssUolLink>();
            aliases = new List<KeyValuePair<int, int?>>((int)recordCount);
            for (int i = 0; i < recordCount; i++)
            {
                int position = checked((int)(recordStart + (long)i * 16));
                int slot = ReadI32(payload, position);
                int stringIndex = ReadI32(payload, position + 4);
                int frameIndex = ReadI32(payload, position + 12);
                // frame_count itself is the empty-image sentinel; only values beyond it are out of range.
                if (frameIndex < 0 || frameIndex > frameCount)
                {
                    throw new UnityException(string.Format(
                        "WZSS link frame index out of range: {0}/{1}", frameIndex, frameCount));
                }
                int? frame = frameIndex < frameCount ? frameIndex : (int?)null;
                if (slot >= 0)
                {
                    aliases.Add(new KeyValuePair<int, int?>(slot, frame));
                }
                // -1 marks a duplicate-image record; only non-negative values reference the UOL string pool above.
                if (stringIndex < 0)
                {
                    continue;
                }
                if (stringIndex >= strings.Length)
                {
                    throw new UnityException(string.Format(
                        "WZSS link string index out of range: {0}/{1}", stringIndex, strings.Length));
                }
                links.Add(new WzssUolLink(strings[stringIndex], frame));
            }
        }

        private static int[] ParsePageMap(byte[] payload, int magic, Func<int, uint> header)
        {
            long count = header(0);
            if (count == 0 || count > 4096)
            {
                throw new UnityException("Invalid WZSS atlas page count: " + count);
            }
            long start = magic + 8L;
            long end = start + count * 4;
            if (end > payload.Length)
            {
                throw new UnityException(string.Format(
                    "WZSS page table out of range: count={0} len={1}", count, payload.Length));
            }
            int[] map = new int[count];
            for (int i = 0; i < map.Length; i++)
            {
                int position = checked((int)(start + i * 4L));
                int page = ReadI32(payload, position);
                if (page < 0 || page >= count)
                {
                    throw new UnityException(string.Format(
                        "WZSS page table entry out of range: {0}/{1}", page, count));
                }
                map[i] = page;
            }
            return map;
        }

        private static FrameEntry[] ParseFrames(
            byte[] payload, int magic, Func<int, uint> header, int rectCount, int pageCount)
        {
            long frameCount = header(9);
            if (frameCount > MaxFrames)
            {
                throw new UnityException("Invalid WZSS frame count: " + frameCount);
            }
            long frameStart = magic + (long)header(10);
            long frameEnd = frameStart + frameCount * 20;
            if (frameStart < 0 || frameEnd > payload.Length)
            {
                throw new UnityException(string.Format(
                    "WZSS frame area out of range: count={0} len={1}", frameCount, payload.Length));
            }

            FrameEntry[] frames = new FrameEntry[frameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                int position = checked((int)(frameStart + (long)i * 20));
                // The 3rd i32 of WzSpriteFrame is the atlas page index and the 4th is the sprite rect index.
                int page = ReadI32(payload, position + 8);
                int rectIndex = ReadI32(payload, position + 12);
                if (rectIndex < 0 || rectIndex >= rectCount)
                {
                    throw new UnityException(string.Format(
                        "WZSS frame rect index out of range: {0}/{1}", rectIndex, rectCount));
                }
                if (page < 0 || page >= pageCount)
                {
                    throw new UnityException(string.Format(
                        "WZSS frame atlas page index out of range: {0}/{1}", page, pageCount));
                }
                frames[i] = new FrameEntry(page, rectIndex);
            }
            return frames;
        }

        private static string[] ReadStringPool(
            byte[] payload, int magic, uint count, uint blobOffset, uint tableOffset)
        {
            if (count > MaxStrings)
            {
                throw new UnityException("Invalid WZSS string count: " + count);
            }
            long tableStart = magic + (long)tableOffset;
            long tableBytes = ((long)count + 1) * 4;
            long tableEnd = tableStart + tableBytes;
            if (tableStart < 0 || tableEnd > payload.Length)
            {
                throw new UnityException("WZSS string table out of range.");
            }
            long blobStart = magic + (long)blobOffset;

            long[] offsets = new long[count + 1];
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i] = (uint)ReadI32(payload, checked((int)(tableStart + i * 4L)));
            }

            string[] strings = new string[count];
            for (int i = 0; i < strings.Length; i++)
            {
                if (offsets[i] > offsets[i + 1])
                {
                    throw new UnityException("The WZSS string table offsets are not ascending.");
                }
                long start = blobStart + offsets[i];
                long end = blobStart + offsets[i + 1];
                if (start < 0 || end > payload.Length)
                {
                    throw new UnityException("WZSS string blob out of range.");
                }
                strings[i] = Encoding.UTF8.GetString(payload, (int)start, (int)(end - start));
            }
            return strings;
        }

        private static int ReadI32(byte[] data, int position)
        {
            if (position < 0 || position > data.Length - 4)
            {
                throw new UnityException("The WZSS data is truncated.");
            }
            return data[position]
                | (data[position + 1] << 8)
                | (data[position + 2] << 16)
                | (data[position + 3] << 24);
        }

        private struct FrameEntry
        {
            public FrameEntry(int page, int rect)
            {
                this.Page = page;
                this.Rect = rect;
            }

            /// <summary>Atlas page index (the 3rd i32).</summary>
            public int Page { get; private set; }

            /// <summary>Sprite rect index (the 4th i32).</summary>
            public int Rect { get; private set; }
        }
    }
}

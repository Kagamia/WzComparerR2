using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Images
{
    /// <summary>
    /// MonoBehaviour payload of the newer SpriteSheet <c>.asset</c>.
    /// <para>
    /// The order is img path, list of atlas page GUIDs, actual sprites (path / page / rect / pivot),
    /// aliases (path → sprite index, pixel-identical) and overrides (path → sprite index + pivot,
    /// sharing pixels but with a different origin, e.g. Mob's <c>jump/0</c> reuses the <c>move/…</c> image).
    /// Strings use Unity's 4-byte aligned format.
    /// </para>
    /// </summary>
    public sealed class SpriteSheetAsset
    {
        private const int MaxItems = 1000000;
        private const int MaxStringLength = 1000000;

        private readonly List<WzssSprite> sprites;
        private readonly Dictionary<string, int?> paths;

        private SpriteSheetAsset(string path, List<WzssSprite> sprites, Dictionary<string, int?> paths)
        {
            this.Path = path;
            this.sprites = sprites;
            this.paths = paths;
        }

        public string Path { get; private set; }

        public bool TryGetSpriteForNode(string innerPath, out WzssSprite sprite)
        {
            sprite = default(WzssSprite);
            int? index;
            if (innerPath == null || !this.paths.TryGetValue(innerPath, out index) || !index.HasValue)
            {
                return false;
            }
            if (index.Value < 0 || index.Value >= this.sprites.Count)
            {
                return false;
            }
            sprite = this.sprites[index.Value];
            return true;
        }

        public bool TryGetFirstSprite(out WzssSprite sprite)
        {
            if (this.sprites.Count > 0)
            {
                sprite = this.sprites[0];
                return true;
            }
            sprite = default(WzssSprite);
            return false;
        }

        /// <summary>All sprite names registered in the index (including aliases and overrides).</summary>
        public IEnumerable<string> EnumerateNames()
        {
            return this.paths.Keys;
        }

        public static SpriteSheetAsset Parse(byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            Cursor cursor = new Cursor(payload);
            string path = cursor.ReadString();

            int pageCount = cursor.ReadCount();
            for (int i = 0; i < pageCount; i++)
            {
                cursor.ReadString(); // atlas GUID
                cursor.Take(8);      // reserved fields of the page
            }

            int spriteCount = cursor.ReadCount();
            List<WzssSprite> sprites = new List<WzssSprite>(spriteCount);
            Dictionary<string, int?> paths = new Dictionary<string, int?>(spriteCount, StringComparer.Ordinal);
            for (int i = 0; i < spriteCount; i++)
            {
                string name = cursor.ReadString();
                int page = cursor.ReadInt32();
                if (page < 0 || page >= pageCount)
                {
                    throw new UnityException(string.Format(
                        "SpriteSheet asset page index out of range: {0}/{1}", page, pageCount));
                }
                WzssRect rect = new WzssRect(
                    cursor.ReadPixel(true), cursor.ReadPixel(true),
                    cursor.ReadPixel(false), cursor.ReadPixel(false));
                cursor.Take(8); // pivot (2 × f32), unused for cropping
                sprites.Add(new WzssSprite(page, rect));
                paths[name] = i;
            }

            int aliasCount = cursor.ReadCount();
            for (int i = 0; i < aliasCount; i++)
            {
                string name = cursor.ReadString();
                int index = cursor.ReadInt32();
                // spriteCount is the empty-image sentinel, matching the existing WZSS convention.
                if (index < 0 || index > spriteCount)
                {
                    throw new UnityException(string.Format(
                        "SpriteSheet asset alias index out of range: {0}/{1}", index, spriteCount));
                }
                paths[name] = index != spriteCount ? index : (int?)null;
            }

            // An override list that only replaces the pivot follows the aliases.
            // It is skipped for data without this trailing section.
            if (cursor.Remaining >= 4)
            {
                int overrideCount = cursor.ReadCount();
                for (int i = 0; i < overrideCount; i++)
                {
                    string name = cursor.ReadString();
                    int index = cursor.ReadInt32();
                    cursor.Take(8); // pivot (2 × f32); unused because rendering uses the origin from the WZJS side
                    if (index < 0 || index > spriteCount)
                    {
                        throw new UnityException(string.Format(
                            "SpriteSheet asset override index out of range: {0}/{1}", index, spriteCount));
                    }
                    paths[name] = index != spriteCount ? index : (int?)null;
                }
            }

            return new SpriteSheetAsset(path, sprites, paths);
        }

        private sealed class Cursor
        {
            private readonly byte[] bytes;
            private int position;

            public Cursor(byte[] bytes)
            {
                this.bytes = bytes;
            }

            public int Remaining
            {
                get { return this.bytes.Length - this.position; }
            }

            public byte[] Take(int count)
            {
                if (count < 0 || this.position > this.bytes.Length - count)
                {
                    throw new UnityException("SpriteSheet asset data out of range.");
                }
                byte[] result = new byte[count];
                Buffer.BlockCopy(this.bytes, this.position, result, 0, count);
                this.position += count;
                return result;
            }

            public int ReadInt32()
            {
                byte[] b = this.Take(4);
                return b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24);
            }

            public int ReadPixel(bool allowZero)
            {
                float value = BitConverter.ToSingle(this.Take(4), 0);
                float rounded = (float)Math.Round(value);
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0
                    || (!allowZero && value == 0)
                    || Math.Abs(value - rounded) > 0.001
                    || rounded > int.MaxValue)
                {
                    throw new UnityException(string.Format(
                        "SpriteSheet asset rect is not made of valid integer pixels: {0}", value));
                }
                return (int)rounded;
            }

            public int ReadCount()
            {
                int count = this.ReadInt32();
                if (count < 0 || count > MaxItems)
                {
                    throw new UnityException("Too many SpriteSheet asset entries: " + count);
                }
                return count;
            }

            public string ReadString()
            {
                int length = this.ReadInt32();
                if (length < 0 || length > MaxStringLength)
                {
                    throw new UnityException("The SpriteSheet asset string is too long: " + length);
                }
                string value = Encoding.UTF8.GetString(this.Take(length), 0, length);
                int padding = (4 - length % 4) % 4;
                this.Take(padding);
                return value;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using WzComparerR2.UnityLib.Classic;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Images
{
    /// <summary>One index and its atlas pages for a spritesheet.</summary>
    public sealed class UnitySheetEntry
    {
        public UnitySheetEntry()
        {
            this.AtlasPages = new List<KeyValuePair<int, UnityEntryLocation>>();
        }

        /// <summary>Container entry order does not guarantee the index precedes the atlas, so the index may not be seen yet during construction.</summary>
        public UnityEntryLocation IndexLocation { get; set; }

        /// <summary>Newer atlases store paths and crop data in .asset; older ones still use WZSS.</summary>
        public bool IndexIsAsset { get; set; }

        /// <summary>(page index, Texture2D object location), sorted by page number.</summary>
        public List<KeyValuePair<int, UnityEntryLocation>> AtlasPages { get; private set; }
    }

    /// <summary>Origin measured from the image's top-left corner, equivalent to WZ's <c>origin</c>.</summary>
    public struct UnitySpriteOrigin
    {
        public UnitySpriteOrigin(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; private set; }
        public int Y { get; private set; }
    }

    /// <summary>A sprite in a spriteset: Sprite object location + its Texture2D object location + origin.</summary>
    public sealed class UnitySetSpriteEntry
    {
        public UnitySetSpriteEntry(UnityEntryLocation spriteLocation, UnityEntryLocation textureLocation, UnitySpriteOrigin origin)
            : this(spriteLocation, textureLocation, origin, null)
        {
        }

        public UnitySetSpriteEntry(
            UnityEntryLocation spriteLocation, UnityEntryLocation textureLocation, UnitySpriteOrigin origin,
            UnityRectF? cropRect)
        {
            this.SpriteLocation = spriteLocation;
            this.TextureLocation = textureLocation;
            this.Origin = origin;
            this.CropRect = cropRect;
        }

        public UnityEntryLocation SpriteLocation { get; private set; }
        public UnityEntryLocation TextureLocation { get; private set; }

        /// <summary>WZ-compatible origin derived from the Sprite's m_Pivot.</summary>
        public UnitySpriteOrigin Origin { get; private set; }

        /// <summary>
        /// Crop rectangle of an atlas-packed sprite (one without <c>m_RD.texture</c> that references a SpriteAtlas).
        /// Null for regular sprites (the Sprite's own m_Rect is used).
        /// </summary>
        public UnityRectF? CropRect { get; private set; }
    }

    /// <summary>One entry in a spriteset .asset payload (name + PPtr to a Sprite).</summary>
    public struct UnitySetAssetEntry
    {
        public UnitySetAssetEntry(string name, int fileId, long pathId)
        {
            this.Name = name;
            this.FileId = fileId;
            this.PathId = pathId;
        }

        /// <summary>Name (empty string for unnamed lists).</summary>
        public string Name { get; private set; }

        /// <summary>PPtr fileID (non-zero when it points to another SerializedFile).</summary>
        public int FileId { get; private set; }

        /// <summary>Sprite pathID.</summary>
        public long PathId { get; private set; }
    }

    /// <summary>
    /// Image index: logical path → image source.
    /// <list type="bullet">
    /// <item>spritesheet_*: the <c>.wzspritesheet</c> in the container (legacy WZSS) or
    /// <c>.asset</c> (newer index object) + <c>_N.png</c> (atlas page Texture2D).</item>
    /// <item>spriteset_*: the <c>.asset</c> in the container (sprite list MonoBehaviour) +
    /// Sprite(213) objects + Texture2D.</item>
    /// </list>
    /// </summary>
    public sealed class UnityImageIndex
    {
        private const string SheetPrefix = "assets/wzassets/spritesheet/";
        private const string SetPrefix = "assets/wzassets/spriteset/";
        private const string AtlasPrefix = "assets/wzassets/atlas/";

        public UnityImageIndex()
        {
            this.SheetsByPath = new Dictionary<string, Dictionary<string, UnitySheetEntry>>(StringComparer.Ordinal);
            this.SetAssets = new Dictionary<string, Dictionary<string, UnitySetSpriteEntry>>(StringComparer.Ordinal);
            this.AtlasAssets = new Dictionary<string, Dictionary<string, UnitySetSpriteEntry>>(StringComparer.Ordinal);
            this.SetPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Logical path → locale → that locale's index and atlas pages.</summary>
        public Dictionary<string, Dictionary<string, UnitySheetEntry>> SheetsByPath { get; private set; }

        /// <summary>Asset path (prefix and extension stripped) → sprite list (name → sprite).</summary>
        public Dictionary<string, Dictionary<string, UnitySetSpriteEntry>> SetAssets { get; private set; }

        /// <summary>
        /// SpriteAtlas (<c>Assets/WzAssets/Atlas/&lt;name&gt;.spriteatlasv2</c>) → sprite list.
        /// Keys use the WZ form <c>UI/&lt;atlas name&gt;</c>, which appears in the tree as <c>UI/&lt;atlas name&gt;.img</c>.
        /// </summary>
        public Dictionary<string, Dictionary<string, UnitySetSpriteEntry>> AtlasAssets { get; private set; }

        /// <summary>Looks up sprites from either a spriteset or an atlas.</summary>
        public Dictionary<string, UnitySetSpriteEntry> FindSpriteAsset(string key)
        {
            if (key == null)
            {
                return null;
            }
            Dictionary<string, UnitySetSpriteEntry> asset;
            if (this.SetAssets.TryGetValue(key, out asset))
            {
                return asset;
            }
            if (this.AtlasAssets.TryGetValue(key, out asset))
            {
                return asset;
            }
            return null;
        }

        /// <summary>
        /// First segment of a spriteset key (region code, e.g. <c>Common</c>, <c>CN</c>, <c>TW</c>).
        /// Corresponds to the <c>SpriteSet/&lt;region&gt;/…</c> directory in the WZ assets.
        /// </summary>
        public HashSet<string> SetPrefixes { get; private set; }

        /// <summary>
        /// Collects image sources from the SerializedFile and AssetBundle container already parsed by Catalog.
        /// <paramref name="objectLocations"/> is a cross-bundle pathID → location map (used to resolve objects in other files, such as atlas references).
        /// </summary>
        internal void CollectFromContainer(
            UnityFsBundle bundle, int bundleIndex, string bundleStem, int nodeIndex,
            UnitySerializedFile serializedFile, IReadOnlyList<UnityContainerEntry> container,
            Dictionary<long, UnityObjectInfo> objectsByPathId,
            Dictionary<long, UnityEntryLocation> objectLocations,
            Func<UnityEntryLocation, byte[]> readEntryBytes)
        {
            // Early beta bundle names are spritesheetgroup_*/spritesetgroup_* (Addressables
            // group prefixes), so a filename prefix match alone is not enough; the container path decides.
            bool isSheet = bundleStem.StartsWith("spritesheet", StringComparison.Ordinal)
                || ContainsPathPrefix(container, SheetPrefix);
            bool isSet = bundleStem.StartsWith("spriteset", StringComparison.Ordinal)
                || ContainsPathPrefix(container, SetPrefix);
            bool isAtlas = bundleStem.StartsWith("atlas", StringComparison.Ordinal)
                || ContainsPathPrefix(container, AtlasPrefix);
            if (!isSheet && !isSet && !isAtlas)
            {
                return;
            }

            Func<long, UnityEntryLocation> locationOf = pathId =>
            {
                UnityObjectInfo obj;
                if (!objectsByPathId.TryGetValue(pathId, out obj))
                {
                    return null;
                }
                long absolute = serializedFile.DataOffset + obj.ByteStart;
                if (absolute < serializedFile.DataOffset)
                {
                    return null;
                }
                return new UnityEntryLocation(
                    bundleIndex, nodeIndex, absolute, obj.ByteSize, obj.ClassId);
            };

            if (isSheet)
            {
                this.CollectSheet(container, locationOf);
            }
            if (isSet)
            {
                this.CollectSet(
                    bundle, bundleIndex, nodeIndex, serializedFile, container, locationOf,
                    objectLocations, readEntryBytes);
            }
            if (isAtlas)
            {
                this.CollectAtlas(
                    bundle, bundleIndex, nodeIndex, serializedFile, container, locationOf,
                    objectLocations, readEntryBytes);
            }
        }

        /// <summary>Whether the container has a path with the given prefix (case-insensitive; index keys are lowercase).</summary>
        private static bool ContainsPathPrefix(
            IReadOnlyList<UnityContainerEntry> container, string prefix)
        {
            foreach (UnityContainerEntry entry in container)
            {
                string path = entry.Path;
                if (path != null && path.Length >= prefix.Length
                    && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Prefers the current locale, then the common atlas, then any variant, so a wrong region selection does not make every image disappear.</summary>
        public static UnitySheetEntry SelectLocalized(
            Dictionary<string, UnitySheetEntry> variants, string locale)
        {
            if (variants == null || variants.Count == 0)
            {
                return null;
            }
            if (variants.Count == 1)
            {
                foreach (UnitySheetEntry only in variants.Values)
                {
                    return only;
                }
            }
            UnitySheetEntry found;
            if (locale != null && variants.TryGetValue(locale.ToUpperInvariant(), out found))
            {
                return found;
            }
            if (variants.TryGetValue("COMMON", out found))
            {
                return found;
            }
            string firstKey = null;
            foreach (string key in variants.Keys)
            {
                if (firstKey == null || string.CompareOrdinal(key, firstKey) < 0)
                {
                    firstKey = key;
                }
            }
            return firstKey == null ? null : variants[firstKey];
        }

        private void CollectSheet(
            IReadOnlyList<UnityContainerEntry> container, Func<long, UnityEntryLocation> locationOf)
        {
            foreach (UnityContainerEntry entry in container)
            {
                string locale;
                string rest;
                if (!TrySplitSheetPath(entry.Path, out locale, out rest))
                {
                    continue;
                }

                string key;
                bool isAsset;
                int page;
                if (TryStripSuffix(rest, ".wzspritesheet", out key))
                {
                    isAsset = false;
                }
                else if (TryStripSuffix(rest, ".asset", out key))
                {
                    isAsset = true;
                }
                else if (TrySplitAtlasPage(rest, out key, out page))
                {
                    UnityEntryLocation pageLocation = locationOf(entry.Asset.PathId);
                    if (pageLocation == null)
                    {
                        continue;
                    }
                    UnitySheetEntry pageEntry = this.GetOrCreateSheet(key, locale);
                    bool exists = false;
                    foreach (KeyValuePair<int, UnityEntryLocation> item in pageEntry.AtlasPages)
                    {
                        if (item.Key == page)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        pageEntry.AtlasPages.Add(new KeyValuePair<int, UnityEntryLocation>(page, pageLocation));
                        pageEntry.AtlasPages.Sort((a, b) => a.Key.CompareTo(b.Key));
                    }
                    continue;
                }
                else
                {
                    continue;
                }

                UnityEntryLocation indexLocation = locationOf(entry.Asset.PathId);
                if (indexLocation == null)
                {
                    continue;
                }
                UnitySheetEntry sheet = this.GetOrCreateSheet(key, locale);
                sheet.IndexLocation = indexLocation;
                sheet.IndexIsAsset = isAsset;
            }
        }

        private void CollectSet(
            UnityFsBundle bundle, int bundleIndex, int nodeIndex, UnitySerializedFile serializedFile,
            IReadOnlyList<UnityContainerEntry> container, Func<long, UnityEntryLocation> locationOf,
            Dictionary<long, UnityEntryLocation> objectLocations,
            Func<UnityEntryLocation, byte[]> readEntryBytes)
        {
            // .asset path → (name → sprite PPtr)
            Dictionary<string, Dictionary<string, UnitySetAssetEntry>> assetMap =
                new Dictionary<string, Dictionary<string, UnitySetAssetEntry>>(StringComparer.Ordinal);
            foreach (UnityContainerEntry entry in container)
            {
                string lower = entry.Path.ToLowerInvariant();
                if (!lower.StartsWith(SetPrefix, StringComparison.Ordinal) || !lower.EndsWith(".asset", StringComparison.Ordinal))
                {
                    continue;
                }
                string key = entry.Path.Substring(SetPrefix.Length, entry.Path.Length - SetPrefix.Length - ".asset".Length);
                UnityEntryLocation assetLocation = locationOf(entry.Asset.PathId);
                if (assetLocation == null)
                {
                    continue;
                }
                byte[] data = bundle.ReadNodeRange(
                    nodeIndex, assetLocation.AbsoluteOffset, (int)assetLocation.ByteSize);
                UnityMonoBehaviourInfo mono;
                try
                {
                    mono = UnityObjectParsers.ParseMonoBehaviour(data, serializedFile.IsLittleEndian);
                }
                catch (UnityException)
                {
                    continue;
                }
                if (mono.HeaderLength > data.Length)
                {
                    continue;
                }
                byte[] payload = new byte[data.Length - mono.HeaderLength];
                Buffer.BlockCopy(data, mono.HeaderLength, payload, 0, payload.Length);

                Dictionary<string, UnitySetAssetEntry> sprites;
                if (!assetMap.TryGetValue(key, out sprites))
                {
                    sprites = new Dictionary<string, UnitySetAssetEntry>(StringComparer.Ordinal);
                    assetMap[key] = sprites;
                }
                foreach (UnitySetAssetEntry item in ParseSetAssetPayload(payload))
                {
                    // Unnamed frame lists have an empty name for every entry, so the pid is used as a temporary key
                    // to prevent overwrites (the final name is filled in from the sprite side).
                    string entryKey = string.IsNullOrEmpty(item.Name)
                        ? UnnamedEntryKeyPrefix + item.PathId.ToString()
                        : item.Name;
                    sprites[entryKey] = item;
                }
            }

            // Resolve within the same file first, then fall back to the cross-bundle index
            // (a spriteset .asset may reference Sprites in other files, such as atlas bundles).
            Func<long, UnityEntryLocation> resolve =
                pathId => this.ResolveLocation(pathId, locationOf, objectLocations);
            Func<UnityEntryLocation, byte[]> readBytes =
                location => this.ReadLocationBytes(location, bundle, bundleIndex, nodeIndex, readEntryBytes);

            // Cache for atlas-packed sprites (the SpriteAtlas bytes are read only once)
            Dictionary<long, UnitySpriteAtlasInfo> atlasCache = new Dictionary<long, UnitySpriteAtlasInfo>();

            // sprite pid → UnitySetSpriteEntry (parse the Sprite to get the texture pid)
            foreach (KeyValuePair<string, Dictionary<string, UnitySetAssetEntry>> asset in assetMap)
            {
                Dictionary<string, UnitySetSpriteEntry> output =
                    new Dictionary<string, UnitySetSpriteEntry>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, UnitySetAssetEntry> item in asset.Value)
                {
                    UnityEntryLocation spriteLocation = resolve(item.Value.PathId);
                    if (spriteLocation == null || spriteLocation.ClassId != UnityClassIds.Sprite)
                    {
                        continue;
                    }
                    bool sameFile = spriteLocation.BundleIndex == bundleIndex && spriteLocation.NodeIndex == nodeIndex;
                    bool littleEndian = sameFile ? serializedFile.IsLittleEndian : true;
                    byte[] data;
                    try
                    {
                        data = readBytes(spriteLocation);
                    }
                    catch (Exception ex) when (ex is UnityException || ex is IOException)
                    {
                        continue;
                    }
                    if (data == null)
                    {
                        continue;
                    }
                    UnitySpriteInfo sprite;
                    try
                    {
                        sprite = UnityObjectParsers.ParseSprite(data, littleEndian);
                    }
                    catch (UnityException)
                    {
                        continue;
                    }

                    UnityPPtr texturePtr = sprite.Texture;
                    UnityRectF? cropRect = null;
                    if (texturePtr.PathId == 0 && sprite.SpriteAtlas.PathId != 0)
                    {
                        // Atlas-packed sprite: look up the actual texture and crop rect from the SpriteAtlas render data.
                        UnitySpriteAtlasInfo atlas = this.GetSpriteAtlas(sprite.SpriteAtlas.PathId, resolve, readBytes, atlasCache);
                        if (atlas == null
                            || !atlas.TryGetRenderData(sprite.RenderDataKeyGuid, out texturePtr, out UnityRectF atlasRect))
                        {
                            continue;
                        }
                        cropRect = atlasRect;
                    }
                    if (texturePtr.PathId == 0)
                    {
                        continue;
                    }
                    UnityEntryLocation textureLocation = resolve(texturePtr.PathId);
                    if (textureLocation == null || textureLocation.ClassId != UnityClassIds.Texture2D)
                    {
                        continue;
                    }

                    // Unnamed frame lists (e.g. portal) are filled in with the sprite name ("0", "1", ...).
                    string spriteName = item.Key;
                    if (string.IsNullOrEmpty(spriteName) || spriteName.StartsWith(UnnamedEntryKeyPrefix, StringComparison.Ordinal))
                    {
                        spriteName = sprite.Name;
                    }
                    if (string.IsNullOrEmpty(spriteName))
                    {
                        spriteName = output.Count.ToString();
                    }
                    if (output.ContainsKey(spriteName))
                    {
                        spriteName = spriteName + "_" + output.Count.ToString();
                    }
                    int originX, originY;
                    sprite.GetWzOrigin(cropRect ?? sprite.Rect, out originX, out originY);
                    output[spriteName] = new UnitySetSpriteEntry(
                        spriteLocation, textureLocation, new UnitySpriteOrigin(originX, originY), cropRect);
                }
                if (output.Count > 0)
                {
                    this.SetAssets[asset.Key] = output;
                    int slash = asset.Key.IndexOf('/');
                    if (slash > 0)
                    {
                        this.SetPrefixes.Add(asset.Key.Substring(0, slash));
                    }
                }
            }
        }

        /// <summary>Reads the bytes of SpriteAtlas(687078895) and caches the <see cref="UnitySpriteAtlasInfo"/>.</summary>
        private UnitySpriteAtlasInfo GetSpriteAtlas(
            long atlasPathId, Func<long, UnityEntryLocation> resolve,
            Func<UnityEntryLocation, byte[]> readBytes, Dictionary<long, UnitySpriteAtlasInfo> cache)
        {
            UnitySpriteAtlasInfo atlas;
            if (cache.TryGetValue(atlasPathId, out atlas))
            {
                return atlas;
            }
            atlas = null;
            UnityEntryLocation location = resolve(atlasPathId);
            if (location != null && location.ClassId == UnityClassIds.SpriteAtlas && readBytes != null)
            {
                try
                {
                    byte[] data = readBytes(location);
                    if (data != null)
                    {
                        atlas = UnitySpriteAtlasInfo.Parse(data, true);
                    }
                }
                catch (Exception ex) when (ex is UnityException || ex is IOException || ex is ArgumentException)
                {
                    atlas = null;
                }
            }
            cache[atlasPathId] = atlas;
            return atlas;
        }

        /// <summary>
        /// Collects a SpriteAtlas (<c>Assets/WzAssets/Atlas/&lt;name&gt;.spriteatlasv2</c>) as <c>UI/&lt;atlas name&gt;</c>.
        /// Child nodes are the sprites packed into the atlas, named by sprite name (duplicates are disambiguated with <c>_N</c>).
        /// </summary>
        private void CollectAtlas(
            UnityFsBundle bundle, int bundleIndex, int nodeIndex, UnitySerializedFile serializedFile,
            IReadOnlyList<UnityContainerEntry> container, Func<long, UnityEntryLocation> locationOf,
            Dictionary<long, UnityEntryLocation> objectLocations,
            Func<UnityEntryLocation, byte[]> readEntryBytes)
        {
            Func<long, UnityEntryLocation> resolve =
                pathId => this.ResolveLocation(pathId, locationOf, objectLocations);
            Func<UnityEntryLocation, byte[]> readBytes =
                location => this.ReadLocationBytes(location, bundle, bundleIndex, nodeIndex, readEntryBytes);

            foreach (UnityContainerEntry entry in container)
            {
                string lower = entry.Path.ToLowerInvariant();
                if (!lower.StartsWith(AtlasPrefix, StringComparison.Ordinal))
                {
                    continue;
                }
                if (!lower.EndsWith(".spriteatlasv2", StringComparison.Ordinal)
                    && !lower.EndsWith(".spriteatlas", StringComparison.Ordinal))
                {
                    continue;
                }

                string fileName = entry.Path.Substring(entry.Path.LastIndexOf('/') + 1);
                int dot = fileName.IndexOf('.');
                string atlasName = dot > 0 ? fileName.Substring(0, dot) : fileName;
                if (string.IsNullOrEmpty(atlasName))
                {
                    continue;
                }

                UnityEntryLocation atlasLocation = resolve(entry.Asset.PathId);
                if (atlasLocation == null)
                {
                    continue;
                }

                UnitySpriteAtlasInfo atlas;
                try
                {
                    byte[] atlasData = readBytes(atlasLocation);
                    atlas = atlasData != null ? UnitySpriteAtlasInfo.Parse(atlasData, true) : null;
                }
                catch (Exception ex) when (ex is UnityException || ex is IOException || ex is ArgumentException)
                {
                    atlas = null;
                }
                if (atlas == null || atlas.Sprites.Count == 0)
                {
                    continue;
                }

                Dictionary<string, UnitySetSpriteEntry> output =
                    new Dictionary<string, UnitySetSpriteEntry>(StringComparer.Ordinal);
                for (int i = 0; i < atlas.Sprites.Count; i++)
                {
                    UnityPPtr spritePtr = atlas.Sprites[i];
                    UnityEntryLocation spriteLocation = resolve(spritePtr.PathId);
                    if (spriteLocation == null || spriteLocation.ClassId != UnityClassIds.Sprite)
                    {
                        continue;
                    }
                    bool sameFile = spriteLocation.BundleIndex == bundleIndex && spriteLocation.NodeIndex == nodeIndex;
                    byte[] data;
                    UnitySpriteInfo sprite;
                    try
                    {
                        data = readBytes(spriteLocation);
                        if (data == null)
                        {
                            continue;
                        }
                        sprite = UnityObjectParsers.ParseSprite(data, sameFile ? serializedFile.IsLittleEndian : true);
                    }
                    catch (Exception ex) when (ex is UnityException || ex is IOException)
                    {
                        continue;
                    }

                    UnityPPtr texturePtr = sprite.Texture;
                    UnityRectF? cropRect = null;
                    if (texturePtr.PathId == 0)
                    {
                        if (!atlas.TryGetRenderData(sprite.RenderDataKeyGuid, out texturePtr, out UnityRectF atlasRect))
                        {
                            continue;
                        }
                        cropRect = atlasRect;
                    }
                    if (texturePtr.PathId == 0)
                    {
                        continue;
                    }
                    UnityEntryLocation textureLocation = resolve(texturePtr.PathId);
                    if (textureLocation == null || textureLocation.ClassId != UnityClassIds.Texture2D)
                    {
                        continue;
                    }

                    string spriteName = sprite.Name;
                    if (string.IsNullOrEmpty(spriteName))
                    {
                        spriteName = i.ToString();
                    }
                    if (output.ContainsKey(spriteName))
                    {
                        spriteName = spriteName + "_" + output.Count.ToString();
                    }
                    int originX, originY;
                    sprite.GetWzOrigin(cropRect ?? sprite.Rect, out originX, out originY);
                    output[spriteName] = new UnitySetSpriteEntry(
                        spriteLocation, textureLocation, new UnitySpriteOrigin(originX, originY), cropRect);
                }

                if (output.Count > 0)
                {
                    this.AtlasAssets["UI/" + atlasName] = output;
                }
            }
        }

        private UnityEntryLocation ResolveLocation(
            long pathId, Func<long, UnityEntryLocation> locationOf,
            Dictionary<long, UnityEntryLocation> objectLocations)
        {
            UnityEntryLocation location = locationOf(pathId);
            if (location != null || objectLocations == null)
            {
                return location;
            }
            return objectLocations.TryGetValue(pathId, out location) ? location : null;
        }

        private byte[] ReadLocationBytes(
            UnityEntryLocation location, UnityFsBundle bundle, int bundleIndex, int nodeIndex,
            Func<UnityEntryLocation, byte[]> readEntryBytes)
        {
            if (location.BundleIndex == bundleIndex && location.NodeIndex == nodeIndex)
            {
                return bundle.ReadNodeRange(nodeIndex, location.AbsoluteOffset, (int)location.ByteSize);
            }
            return readEntryBytes != null ? readEntryBytes(location) : null;
        }

        private UnitySheetEntry GetOrCreateSheet(string key, string locale)
        {
            Dictionary<string, UnitySheetEntry> variants;
            if (!this.SheetsByPath.TryGetValue(key, out variants))
            {
                variants = new Dictionary<string, UnitySheetEntry>(StringComparer.Ordinal);
                this.SheetsByPath[key] = variants;
            }
            string code = locale.ToUpperInvariant();
            UnitySheetEntry entry;
            if (!variants.TryGetValue(code, out entry))
            {
                entry = new UnitySheetEntry();
                variants[code] = entry;
            }
            return entry;
        }

        private static bool TrySplitSheetPath(string path, out string locale, out string rest)
        {
            locale = null;
            rest = null;
            if (path == null || path.Length <= SheetPrefix.Length)
            {
                return false;
            }
            if (string.Compare(path, 0, SheetPrefix, 0, SheetPrefix.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }
            string tail = path.Substring(SheetPrefix.Length);
            int slash = tail.IndexOf('/');
            if (slash <= 0 || slash == tail.Length - 1)
            {
                return false;
            }
            locale = tail.Substring(0, slash);
            rest = tail.Substring(slash + 1);
            return true;
        }

        private static bool TryStripSuffix(string value, string suffix, out string result)
        {
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                result = value.Substring(0, value.Length - suffix.Length);
                return true;
            }
            result = null;
            return false;
        }

        /// <summary>"login_2.png" → ("login", 2); returns false without an _N suffix.</summary>
        private static bool TrySplitAtlasPage(string value, out string key, out int page)
        {
            key = null;
            page = 0;
            if (!TryStripSuffix(value, ".png", out string baseName))
            {
                return false;
            }
            int underscore = baseName.LastIndexOf('_');
            if (underscore <= 0 || underscore == baseName.Length - 1)
            {
                return false;
            }
            if (!int.TryParse(baseName.Substring(underscore + 1), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out page) || page < 0)
            {
                return false;
            }
            key = baseName.Substring(0, underscore);
            return true;
        }

        /// <summary>
        /// spriteset .asset payload: (count, (string, pathID)×n) or (count, PPtr×n);
        /// there is also a layout with no count prefix, just consecutive PPtrs. Returns (name, sprite pathID, fileID);
        /// unnamed entries get "".
        /// </summary>
        internal static List<UnitySetAssetEntry> ParseSetAssetPayload(byte[] payload)
        {
            List<UnitySetAssetEntry> output = new List<UnitySetAssetEntry>();

            // Newer frame lists (e.g. Map/MapHelper/portal/game/pv) use the layout
            // [name count i64 = 0][count i64][(pathId i64 + 16 bytes) × count].
            // They carry no names, so sprite names are filled in by CollectSet.
            List<UnitySetAssetEntry> framedEntries;
            if (TryParseFramedEntries(payload, out framedEntries))
            {
                return framedEntries;
            }

            uint count = U32At(payload, 0);
            if (count > 100000)
            {
                return output;
            }

            if (IsNamedEntries(payload))
            {
                // Named entries come in an older layout whose PPtr has no fileID and a newer layout
                // with (fileID i32, pathID i64) (e.g. Map/MapHelper/mark in 2.0.6).
                // The newer layout is tried first, falling back to the older one if the entry count does not match.
                List<UnitySetAssetEntry> withFileId = ParseNamedEntries(payload, count, true, out int endWithFileId);
                if (withFileId.Count == count && endWithFileId == payload.Length)
                {
                    return withFileId;
                }
                List<UnitySetAssetEntry> withoutFileId = ParseNamedEntries(payload, count, false, out int endWithoutFileId);
                if (withoutFileId.Count == count && endWithoutFileId == payload.Length)
                {
                    return withoutFileId;
                }
                return withoutFileId.Count > withFileId.Count ? withoutFileId : withFileId;
            }

            // A count of 0 can mean a genuinely empty set, or no count prefix at all with
            // the PPtrs at the start. The latter (and payloads whose first i32 is a fileID) are parsed as bare PPtrs.
            if (count == 0 || IsBarePPtrLayout(payload))
            {
                return ParseBarePPtrs(payload);
            }

            int position = 4;
            for (uint i = 0; i < count; i++)
            {
                // PPtr: fileID i32 + pathID i64 (null entries are skipped and parsing continues)
                if (position > payload.Length - 12)
                {
                    break;
                }
                int fileId = I32At(payload, position);
                long pathId = I64At(payload, position + 4);
                position += 12;
                if (pathId == 0)
                {
                    continue;
                }
                output.Add(new UnitySetAssetEntry(string.Empty, fileId, pathId));
            }
            return output;
        }

        /// <summary>
        /// Whether the payload is a series of PPtrs from the start (no count prefix, or the count is actually the PPtr's fileID).
        /// For a spriteset that references a Sprite inside an atlas, the first i32 is the fileID (≠ 0).
        /// </summary>
        private static bool IsBarePPtrLayout(byte[] payload)
        {
            if (payload == null || payload.Length < 12)
            {
                return false;
            }
            long firstPathId = I64At(payload, 4);
            return firstPathId != 0 && (uint)firstPathId > 100000;
        }


        /// <summary>
        /// Reads the unnamed frame list layout (<c>[name count i64 = 0][count i64][(pathId i64 + 16 bytes) × count]</c>).
        /// Returns false when it does not apply (legacy bare PPtrs or named entries).
        /// </summary>
        private static bool TryParseFramedEntries(byte[] payload, out List<UnitySetAssetEntry> entries)
        {
            entries = null;
            if (payload == null || payload.Length < FramedHeaderLength + FramedEntryStride)
            {
                return false;
            }
            if (I64At(payload, 0) != 0)
            {
                return false; // named entries use a different format
            }
            long count = I64At(payload, 8);
            if (count <= 0 || count > 100000)
            {
                return false;
            }
            if (FramedHeaderLength + count * FramedEntryStride != payload.Length)
            {
                return false;
            }

            List<UnitySetAssetEntry> output = new List<UnitySetAssetEntry>((int)count);
            for (int i = 0; i < count; i++)
            {
                long pathId = I64At(payload, FramedHeaderLength + i * FramedEntryStride);
                if (pathId == 0)
                {
                    continue;
                }
                output.Add(new UnitySetAssetEntry(string.Empty, 0, pathId));
            }
            if (output.Count == 0)
            {
                return false;
            }
            entries = output;
            return true;
        }

        /// <summary>Header length of framed entries (name count i64 + count i64).</summary>
        private const int FramedHeaderLength = 16;

        /// <summary>Temporary key prefix for unnamed entries (used only within <see cref="CollectSet"/>).</summary>
        private const string UnnamedEntryKeyPrefix = "#pid:";

        /// <summary>Size of one entry in an unnamed frame list (pathId i64 + 16 bytes of extra data).</summary>
        private const int FramedEntryStride = 24;

        /// <summary>Whether the payload holds entries in "name length + name" form.</summary>
        private static bool IsNamedEntries(byte[] payload)
        {
            uint declaredLength = U32At(payload, 4);
            if (declaredLength == 0 || declaredLength >= 512)
            {
                return false;
            }
            long end = 8L + declaredLength;
            if (end > payload.Length)
            {
                return false;
            }
            for (int i = 8; i < end; i++)
            {
                byte c = payload[i];
                if (!((c >= 0x21 && c <= 0x7E) || c == 0x20))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Reads named entries. <paramref name="includeFileId"/> indicates whether a fileID (i32) precedes each PPtr.
        /// <paramref name="end"/> is the position reached after reading (used for layout detection).
        /// </summary>
        private static List<UnitySetAssetEntry> ParseNamedEntries(byte[] payload, uint count, bool includeFileId, out int end)
        {
            List<UnitySetAssetEntry> output = new List<UnitySetAssetEntry>();
            int stride = includeFileId ? 12 : 8;
            int position = 4;
            for (uint i = 0; i < count; i++)
            {
                if (position > payload.Length - 4)
                {
                    break;
                }
                int length = (int)U32At(payload, position);
                position += 4;
                if (length < 0 || position > payload.Length - length)
                {
                    break;
                }
                string name = System.Text.Encoding.UTF8.GetString(payload, position, length);
                position = (position + length + 3) & ~3;
                if (position > payload.Length - stride)
                {
                    break;
                }
                int fileId = 0;
                if (includeFileId)
                {
                    fileId = I32At(payload, position);
                    position += 4;
                }
                long pathId = I64At(payload, position);
                position += 8;
                if (pathId == 0)
                {
                    break;
                }
                output.Add(new UnitySetAssetEntry(name, fileId, pathId));
            }
            end = position;
            return output;
        }

        /// <summary>
        /// Bare PPtr series with no count prefix: (fileID i32, pathID i64)×n.
        /// Stops at a pathID of 0 (or at the end of the data).
        /// </summary>
        private static List<UnitySetAssetEntry> ParseBarePPtrs(byte[] payload)
        {
            List<UnitySetAssetEntry> output = new List<UnitySetAssetEntry>();
            int position = 0;
            while (position <= payload.Length - 12)
            {
                int fileId = I32At(payload, position);
                long pathId = I64At(payload, position + 4);
                if (pathId == 0)
                {
                    break;
                }
                output.Add(new UnitySetAssetEntry(string.Empty, fileId, pathId));
                position += 12;
            }
            return output;
        }

        private static uint U32At(byte[] data, int position)
        {
            if (position < 0 || position > data.Length - 4)
            {
                return 0;
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

        private static long I64At(byte[] data, int position)
        {
            if (position < 0 || position > data.Length - 8)
            {
                return 0;
            }
            return data[position]
                | ((long)data[position + 1] << 8)
                | ((long)data[position + 2] << 16)
                | ((long)data[position + 3] << 24)
                | ((long)data[position + 4] << 32)
                | ((long)data[position + 5] << 40)
                | ((long)data[position + 6] << 48)
                | ((long)data[position + 7] << 56);
        }
    }
}


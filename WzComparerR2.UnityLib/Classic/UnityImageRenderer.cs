using System;
using System.Collections.Generic;
using WzComparerR2.UnityLib.Images;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.UnityLib.Wzjs;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Image lookup and rendering: WZJS Canvas / UOL nodes → PNG.
    /// <para>
    /// A spritesheet consists of a WZSS (older) or SpriteSheet <c>.asset</c> (newer) index plus the atlas page
    /// Texture2Ds; a spriteset consists of a sprite list MonoBehaviour plus Sprite objects.
    /// Only the atlas page containing the target sprite is decoded.
    /// </para>
    /// </summary>
    public sealed class UnityImageRenderer
    {
        private const int ImageCacheCapacity = 64;
        private const long ImageCacheBytes = 64L * 1024 * 1024;
        private const long PagePixelCacheBytes = 128L * 1024 * 1024;

        private readonly ClassicCatalog catalog;
        private readonly IReadOnlyList<UnityFsBundle> bundles;
        private readonly Func<string, WzjsNode> treeProvider;
        private readonly object syncRoot = new object();
        private readonly Dictionary<string, byte[]> imageCache = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        private readonly List<string> imageCacheOrder = new List<string>();
        private long imageCacheBytes;
        private readonly Dictionary<string, byte[]> pagePixelCache = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        private readonly List<string> pagePixelCacheOrder = new List<string>();
        private long pagePixelCacheBytes;
        private const int SheetCacheCapacity = 48;
        private readonly Dictionary<string, object> sheetCache = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly List<string> sheetCacheOrder = new List<string>();

        /// <summary>Upper bound when scanning sibling imgs for a sheet (avoids building indexes in huge directories).</summary>
        private const int MaxCrossSheetSiblings = 256;
        private readonly object crossSheetLock = new object();
        private readonly Dictionary<string, Dictionary<string, string>> crossSheetIndex =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        public UnityImageRenderer(
            ClassicCatalog catalog, IReadOnlyList<UnityFsBundle> bundles,
            Func<string, WzjsNode> treeProvider, string locale)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }
            if (bundles == null)
            {
                throw new ArgumentNullException(nameof(bundles));
            }
            if (treeProvider == null)
            {
                throw new ArgumentNullException(nameof(treeProvider));
            }
            this.catalog = catalog;
            this.bundles = bundles;
            this.treeProvider = treeProvider;
            this.Locale = locale;
        }

        public string Locale { get; set; }

        /// <summary>Clears the rendering caches (should be called after switching locales).</summary>
        public void ClearCache()
        {
            lock (this.syncRoot)
            {
                this.imageCache.Clear();
                this.imageCacheOrder.Clear();
                this.imageCacheBytes = 0;
            }
            lock (this.pagePixelCache)
            {
                this.pagePixelCache.Clear();
                this.pagePixelCacheOrder.Clear();
                this.pagePixelCacheBytes = 0;
            }
            lock (this.sheetCache)
            {
                this.sheetCache.Clear();
                this.sheetCacheOrder.Clear();
            }
            lock (this.crossSheetLock)
            {
                this.crossSheetIndex.Clear();
            }
        }

        /// <summary>
        /// Parses spritesheet index data (reusing the cache when available). A single spritesheet usually contains many sprites,
        /// so re-parsing per canvas would make batch processing very slow.
        /// </summary>
        private T GetParsedSheet<T>(UnityEntryLocation location, Func<byte[], T> parser) where T : class
        {
            string key = string.Format("{0}:{1}:{2}:{3}",
                location.BundleIndex, location.NodeIndex, location.AbsoluteOffset, location.ByteSize);
            lock (this.sheetCache)
            {
                object cached;
                if (this.sheetCache.TryGetValue(key, out cached))
                {
                    this.sheetCacheOrder.Remove(key);
                    this.sheetCacheOrder.Add(key);
                    return cached as T;
                }
            }

            byte[] payload = this.ReadEntry(location);
            UnityMonoBehaviourInfo mono = UnityObjectParsers.ParseMonoBehaviour(payload, true);
            if (mono.HeaderLength > payload.Length)
            {
                throw new UnityException("The SpriteSheet MonoBehaviour header is out of range.");
            }
            T parsed = parser(Slice(payload, mono.HeaderLength));

            lock (this.sheetCache)
            {
                this.sheetCache[key] = parsed;
                this.sheetCacheOrder.Add(key);
                while (this.sheetCacheOrder.Count > SheetCacheCapacity)
                {
                    string evict = this.sheetCacheOrder[0];
                    this.sheetCacheOrder.RemoveAt(0);
                    this.sheetCache.Remove(evict);
                }
            }
            return parsed;
        }

        /// <summary>
        /// Decodes the RGBA pixels of an atlas page (reusing the cache when available). The same page is shared by many canvases,
        /// so without a cache every canvas would re-decode the whole page and batch processing (such as the comparer) would be very slow.
        /// </summary>
        private byte[] GetAtlasPixels(UnityEntryLocation location, UnityTexture2DInfo texture, byte[] data)
        {
            string key = string.Format("{0}:{1}:{2}:{3}",
                location.BundleIndex, location.NodeIndex, location.AbsoluteOffset, location.ByteSize);
            lock (this.pagePixelCache)
            {
                byte[] cached;
                if (this.pagePixelCache.TryGetValue(key, out cached))
                {
                    this.pagePixelCacheOrder.Remove(key);
                    this.pagePixelCacheOrder.Add(key);
                    return cached;
                }
            }

            UnityFsBundle bundle = this.bundles[location.BundleIndex];
            byte[] rgba = UnityTexture.FetchPixels(bundle, texture, data);
            if (rgba == null || rgba.Length > PagePixelCacheBytes)
            {
                return rgba;
            }

            lock (this.pagePixelCache)
            {
                byte[] existing;
                if (this.pagePixelCache.TryGetValue(key, out existing))
                {
                    return existing;
                }
                while (this.pagePixelCacheOrder.Count > 0
                    && this.pagePixelCacheBytes + rgba.Length > PagePixelCacheBytes)
                {
                    string evict = this.pagePixelCacheOrder[0];
                    this.pagePixelCacheOrder.RemoveAt(0);
                    byte[] removed;
                    if (this.pagePixelCache.TryGetValue(evict, out removed))
                    {
                        this.pagePixelCacheBytes -= removed.Length;
                        this.pagePixelCache.Remove(evict);
                    }
                }
                this.pagePixelCache[key] = rgba;
                this.pagePixelCacheOrder.Add(key);
                this.pagePixelCacheBytes += rgba.Length;
            }
            return rgba;
        }

        /// <summary>Reads the object bytes that <see cref="UnityEntryLocation"/> points to.</summary>
        public byte[] ReadEntry(UnityEntryLocation location)
        {
            this.ValidateLocation(location);
            UnityFsBundle bundle = this.bundles[location.BundleIndex];
            return bundle.ReadNodeRange(location.NodeIndex, location.AbsoluteOffset, (int)location.ByteSize);
        }

        /// <summary>Gets the image (PNG bytes) of a canvas or uol node. Returns null when no image is found.</summary>
        public byte[] GetImage(string path)
        {
            byte[] cached = this.TryGetCachedImage(path);
            if (cached != null)
            {
                return cached;
            }
            byte[] image = this.GetImageUncached(path);
            if (image != null)
            {
                return this.CacheImage(path, image);
            }
            return null;
        }

        /// <summary>
        /// Provides the whole atlas page as a preview when the exact frame cannot be resolved from the WZSS node key.
        /// Only verified Canvas or UOL inner nodes are accepted, so an img root or a plain SubTree never shows an atlas by mistake.
        /// </summary>
        public byte[] GetCanvasAtlasFallback(string path)
        {
            string imageKey;
            string inner;
            if (!this.catalog.TrySplitImagePrefix(path, out imageKey, out inner) || inner.Length == 0)
            {
                return null;
            }
            WzjsNode node = this.treeProvider(imageKey).Find(inner);
            if (node == null || !node.IsImageCapable)
            {
                return null;
            }

            List<string> candidates = new List<string>();
            candidates.Add(imageKey + "/" + inner);
            int slash = imageKey.LastIndexOf('/');
            if (slash >= 0)
            {
                candidates.Add(imageKey.Substring(0, slash) + "/" + inner);
            }
            candidates.Add(imageKey);

            foreach (string candidate in candidates)
            {
                UnitySheetEntry entry = this.GetSheet(candidate);
                if (entry == null || entry.AtlasPages.Count == 0)
                {
                    continue;
                }
                return this.RenderAtlasPage(entry.AtlasPages[0].Value);
            }
            return null;
        }

        private byte[] GetImageUncached(string path)
        {
            // Spriteset assets (TW/UI/..., Common/UI/...) are not in catalog.Entries,
            // so match SetAssets keys by "region prefix + prefix match". Locale first, then Common.
            foreach (SetAssetLookup lookup in this.GetSetAssetLookups(path))
            {
                Dictionary<string, UnitySetSpriteEntry> asset;
                asset = this.catalog.Image.FindSpriteAsset(lookup.Key);
                if (asset == null)
                {
                    continue;
                }
                UnitySetSpriteEntry spriteEntry = PickSetSprite(asset, lookup.Inner);
                if (spriteEntry != null)
                {
                    return this.RenderSetSprite(spriteEntry);
                }
            }

            string imageKey;
            string inner;
            if (!this.catalog.TrySplitImagePrefix(path, out imageKey, out inner))
            {
                return null;
            }

            // Images belong only to Canvas and UOL (UOL also has its own slot in the WZSS node key pool).
            // Narrow it down through the WZJS tree before looking up the index.
            if (inner.Length > 0)
            {
                WzjsNode node = this.treeProvider(imageKey).Find(inner);
                if (node == null || !node.IsImageCapable)
                {
                    return null;
                }
            }

            List<string> candidates = new List<string>();
            if (inner.Length > 0)
            {
                candidates.Add(imageKey + "/" + inner);
                int slash = imageKey.LastIndexOf('/');
                if (slash >= 0)
                {
                    candidates.Add(imageKey.Substring(0, slash) + "/" + inner);
                }
            }
            candidates.Add(imageKey);

            foreach (string candidate in candidates)
            {
                Dictionary<string, UnitySheetEntry> variants;
                if (!this.catalog.Image.SheetsByPath.TryGetValue(candidate, out variants))
                {
                    continue;
                }
                UnitySheetEntry entry = UnityImageIndex.SelectLocalized(variants, this.Locale);
                if (entry == null)
                {
                    continue;
                }
                // A regional variant (such as the CN variant of Map/Back/blueCave2) contains only the sprites changed
                // in that region, so all other nodes fall back to the COMMON index.
                UnitySheetEntry fallback = null;
                UnitySheetEntry common;
                if (variants.TryGetValue("COMMON", out common) && !ReferenceEquals(common, entry))
                {
                    fallback = common;
                }
                bool perImage = string.Equals(candidate, imageKey, StringComparison.Ordinal);
                byte[] sheetImage = this.RenderSheet(entry, fallback, candidate, imageKey, inner, perImage);
                if (sheetImage != null)
                {
                    return sheetImage;
                }
                // When this img's sheet has no such sprite, try the other candidates and sibling sheets.
            }

            Dictionary<string, UnitySetSpriteEntry> setAsset = this.catalog.Image.FindSpriteAsset(imageKey);
            if (setAsset != null)
            {
                UnitySetSpriteEntry spriteEntry = PickSetSprite(setAsset, inner);
                if (spriteEntry != null)
                {
                    return this.RenderSetSprite(spriteEntry);
                }
            }

            // The Classic World client sometimes keeps only some of an img's sprites in another img's atlas
            // (for example, Character/PetEquip/01802000.img/5000036/angry/0 exists only in the 01802006 sheet).
            // When the img's own sheet does not contain them, resolve from sibling sheets in the same parent directory.
            return this.TryRenderFromSiblingSheet(imageKey, inner);
        }

        /// <summary>
        /// Resolves a sprite from a sibling img's spritesheet (null when not found).
        /// </summary>
        private byte[] TryRenderFromSiblingSheet(string imageKey, string inner)
        {
            if (string.IsNullOrEmpty(imageKey) || string.IsNullOrEmpty(inner))
            {
                return null;
            }
            int slash = imageKey.LastIndexOf('/');
            if (slash <= 0)
            {
                return null;
            }
            string sheetKey;
            if (!this.GetSiblingSheetIndex(imageKey.Substring(0, slash)).TryGetValue(inner, out sheetKey)
                || string.Equals(sheetKey, imageKey, StringComparison.Ordinal))
            {
                return null;
            }
            Dictionary<string, UnitySheetEntry> variants;
            if (!this.catalog.Image.SheetsByPath.TryGetValue(sheetKey, out variants))
            {
                return null;
            }
            UnitySheetEntry entry = UnityImageIndex.SelectLocalized(variants, this.Locale);
            if (entry == null)
            {
                return null;
            }
            try
            {
                return this.RenderSheetEntry(entry, sheetKey, imageKey, inner, perImage: true);
            }
            catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException)
            {
                // A broken sheet must not prevent searching the other sibling sheets.
                return null;
            }
        }

        /// <summary>
        /// Returns a "sprite name → sheet key" index spanning the sibling img sheets directly under the parent directory (built on first use).
        /// </summary>
        private Dictionary<string, string> GetSiblingSheetIndex(string parentPath)
        {
            lock (this.crossSheetLock)
            {
                Dictionary<string, string> index;
                if (this.crossSheetIndex.TryGetValue(parentPath, out index))
                {
                    return index;
                }

                index = new Dictionary<string, string>(StringComparer.Ordinal);
                string prefix = parentPath + "/";
                List<string> sheetKeys = new List<string>();
                foreach (string key in this.catalog.Image.SheetsByPath.Keys)
                {
                    if (key.Length <= prefix.Length || !key.StartsWith(prefix, StringComparison.Ordinal)
                        || key.IndexOf('/', prefix.Length) >= 0)
                    {
                        continue; // direct children only
                    }
                    sheetKeys.Add(key);
                }

                if (sheetKeys.Count <= MaxCrossSheetSiblings)
                {
                    sheetKeys.Sort(StringComparer.Ordinal);
                    foreach (string sheetKey in sheetKeys)
                    {
                        UnitySheetEntry entry = UnityImageIndex.SelectLocalized(
                            this.catalog.Image.SheetsByPath[sheetKey], this.Locale);
                        if (entry == null || entry.IndexLocation == null || !entry.IndexIsAsset)
                        {
                            continue;
                        }
                        SpriteSheetAsset asset;
                        try
                        {
                            asset = this.GetParsedSheet(entry.IndexLocation, SpriteSheetAsset.Parse);
                        }
                        catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException)
                        {
                            continue;
                        }
                        if (asset == null)
                        {
                            continue;
                        }
                        foreach (string name in asset.EnumerateNames())
                        {
                            if (!index.ContainsKey(name))
                            {
                                index[name] = sheetKey;
                            }
                        }
                    }
                }

                this.crossSheetIndex[parentPath] = index;
                return index;
            }
        }

        /// <summary>Search criteria for one spriteset lookup: (asset index key, sprite name).</summary>
        private struct SetAssetLookup
        {
            public SetAssetLookup(string key, string inner)
            {
                this.Key = key;
                this.Inner = inner;
            }

            public string Key { get; private set; }
            public string Inner { get; private set; }
        }

        /// <summary>
        /// Enumerates candidate index keys for a spriteset: first the path itself (with <c>Common/…</c> or <c>TW/…</c> prefixes),
        /// then with prefixes added in the order "current locale → Common". Region prefixes never fall back to another locale,
        /// which prevents images from the wrong region being shown.
        /// </summary>
        private List<SetAssetLookup> GetSetAssetLookups(string path)
        {
            List<SetAssetLookup> lookups = new List<SetAssetLookup>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            CollectSetAssetLookups(lookups, seen, path);

            string localePrefix = FindSetPrefix(this.Locale);
            if (localePrefix != null)
            {
                CollectSetAssetLookups(lookups, seen, localePrefix + "/" + path);
            }
            string commonPrefix = FindSetPrefix("Common");
            if (commonPrefix != null)
            {
                CollectSetAssetLookups(lookups, seen, commonPrefix + "/" + path);
            }
            return lookups;
        }

        private string FindSetPrefix(string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                return null;
            }
            foreach (string prefix in this.catalog.Image.SetPrefixes)
            {
                if (string.Equals(prefix, locale, StringComparison.OrdinalIgnoreCase))
                {
                    return prefix;
                }
            }
            return null;
        }

        private void CollectSetAssetLookups(
            List<SetAssetLookup> lookups, HashSet<string> seen, string candidatePath)
        {
            string key;
            string inner;
            if (!TrySplitSetAssetPrefix(this.catalog, candidatePath, out key, out inner))
            {
                return;
            }
            if (seen.Add(key + "\n" + inner))
            {
                lookups.Add(new SetAssetLookup(key, inner));
            }
        }

        private UnitySheetEntry GetSheet(string key)
        {
            Dictionary<string, UnitySheetEntry> variants;
            if (!this.catalog.Image.SheetsByPath.TryGetValue(key, out variants))
            {
                return null;
            }
            return UnityImageIndex.SelectLocalized(variants, this.Locale);
        }

        /// <summary>spritesheet → PNG (frame cropping; when the mapping is unknown only the img root can emit the whole page).</summary>
        private byte[] RenderSheet(
            UnitySheetEntry entry, UnitySheetEntry fallback, string sheetKey,
            string imageKey, string inner, bool perImage)
        {
            byte[] image = this.RenderSheetEntry(entry, sheetKey, imageKey, inner, perImage);
            if (image == null && fallback != null && inner.Length > 0)
            {
                // A regional variant contains only the sprites changed in that region, so missing nodes take their image from the COMMON variant.
                image = this.RenderSheetEntry(fallback, sheetKey, imageKey, inner, perImage);
            }
            // Returns null when the atlas has no sprite for this node (the data set simply has no image; this is not an error).
            return image;
        }

        /// <summary>
        /// Frame candidates for a node path. The WZJS tree may contain regional marker subtrees such as <c>…/CN/0</c>,
        /// but atlas node keys do not include regional segments; some nodes are also single-frame directories (<c>…/3</c> ↔ <c>…/3/0</c>).
        /// </summary>
        private static List<string> GetFrameInnerCandidates(string inner)
        {
            List<string> candidates = new List<string>(4);
            if (inner.Length == 0)
            {
                candidates.Add(inner);
                return candidates;
            }

            candidates.Add(inner);
            candidates.Add(inner + "/0");
            if (inner.EndsWith("/0", StringComparison.Ordinal))
            {
                candidates.Add(inner.Substring(0, inner.Length - 2));
            }

            // Also add variants with the region segment (CN/TW/KR/JP/COMMON) removed.
            int count = candidates.Count;
            for (int i = 0; i < count; i++)
            {
                string stripped = StripRegionSegments(candidates[i]);
                if (stripped != null && !candidates.Contains(stripped))
                {
                    candidates.Add(stripped);
                }
            }
            return candidates;
        }

        private static string StripRegionSegments(string inner)
        {
            string[] segments = inner.Split('/');
            List<string> kept = new List<string>(segments.Length);
            bool changed = false;
            foreach (string segment in segments)
            {
                if (IsRegionSegment(segment))
                {
                    changed = true;
                    continue;
                }
                kept.Add(segment);
            }
            if (!changed || kept.Count == 0)
            {
                return null;
            }
            return string.Join("/", kept);
        }

        private static bool IsRegionSegment(string segment)
        {
            return string.Equals(segment, "CN", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, "TW", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, "KR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, "JP", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, "COMMON", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Emits a PNG from a single atlas variant. Returns null when there is no matching frame.</summary>
        private byte[] RenderSheetEntry(
            UnitySheetEntry entry, string sheetKey, string imageKey, string inner, bool perImage)
        {
            if (entry.IndexLocation == null)
            {
                throw new UnityException("The spritesheet has no image index.");
            }

            WzssSprite? sprite;
            if (entry.IndexIsAsset)
            {
                SpriteSheetAsset asset = this.GetParsedSheet(entry.IndexLocation, SpriteSheetAsset.Parse);
                if (!string.Equals(asset.Path, sheetKey, StringComparison.Ordinal))
                {
                    throw new UnityException(string.Format(
                        "The SpriteSheet asset path does not match: {0} != {1}", asset.Path, sheetKey));
                }
                WzssSprite found = default(WzssSprite);
                bool foundSprite = false;
                if (inner.Length > 0)
                {
                    foreach (string candidateInner in GetFrameInnerCandidates(inner))
                    {
                        if (asset.TryGetSpriteForNode(candidateInner, out found))
                        {
                            foundSprite = true;
                            break;
                        }
                    }
                }
                if (foundSprite)
                {
                    sprite = found;
                }
                else if (!perImage && inner.Length > 0 && asset.TryGetFirstSprite(out found))
                {
                    sprite = found;
                }
                else
                {
                    sprite = null;
                }
            }
            else
            {
                WzssIndex wzss = this.GetParsedSheet(entry.IndexLocation, WzssIndex.Parse);
                // The older WZSS format locates frames by node key; a few UOLs need reverse lookup.
                int frame = -1;
                bool hasFrame = false;
                if (inner.Length > 0)
                {
                    foreach (string candidateInner in GetFrameInnerCandidates(inner))
                    {
                        if (wzss.TryGetFrameIndexForNode(candidateInner, out frame))
                        {
                            hasFrame = true;
                            break;
                        }
                    }
                }
                if (!hasFrame)
                {
                    if (perImage && inner.Length > 0)
                    {
                        int inferred;
                        if (TryInferFrameFromUol(this.treeProvider(imageKey), inner, wzss.UolLinks, out inferred))
                        {
                            frame = inferred;
                            hasFrame = true;
                        }
                    }
                    else if (wzss.Rects.Count > 0)
                    {
                        frame = 0;
                        hasFrame = true;
                    }
                }
                WzssSprite wzssSprite;
                sprite = hasFrame && wzss.TryGetSprite(frame, out wzssSprite) ? wzssSprite : (WzssSprite?)null;
            }

            // Decode only the atlas page containing the target sprite.
            int? wantedPage = sprite.HasValue ? sprite.Value.Page : (int?)null;
            List<AtlasPage> pages = new List<AtlasPage>();
            foreach (KeyValuePair<int, UnityEntryLocation> page in entry.AtlasPages)
            {
                bool needed = wantedPage.HasValue
                    ? page.Key == wantedPage.Value
                    // Without a frame, only the img root uses the first page.
                    : inner.Length == 0 && pages.Count == 0;
                if (!needed)
                {
                    continue;
                }
                byte[] data = this.ReadEntry(page.Value);
                UnityTexture2DInfo texture = UnityObjectParsers.ParseTexture2D(data, true);
                byte[] rgba = this.GetAtlasPixels(page.Value, texture, data);
                pages.Add(new AtlasPage(page.Key, texture.Width, texture.Height, rgba));
            }

            if (sprite.HasValue)
            {
                return WzssIndex.CutSprite(pages, sprite.Value).ToPng();
            }
            if (inner.Length == 0)
            {
                if (pages.Count == 0)
                {
                    throw new UnityException("The spritesheet has no atlas page.");
                }
                AtlasPage page = pages[0];
                // Pixels in the cache cannot be flipped in place, so copy them first.
                byte[] pixels = (byte[])page.Rgba.Clone();
                UnityTexture.FlipVertical(pixels, page.Width, page.Height);
                return UnityTexture.ToPng(page.Width, page.Height, pixels);
            }
            return null;
        }

        private byte[] RenderAtlasPage(UnityEntryLocation location)
        {
            byte[] data = this.ReadEntry(location);
            UnityTexture2DInfo texture = UnityObjectParsers.ParseTexture2D(data, true);
            byte[] rgba = (byte[])this.GetAtlasPixels(location, texture, data).Clone();
            UnityTexture.FlipVertical(rgba, texture.Width, texture.Height);
            return UnityTexture.ToPng(texture.Width, texture.Height, rgba);
        }

        /// <summary>spriteset sprite → PNG (cropped by m_Rect or by the atlas packing rectangle).</summary>
        private byte[] RenderSetSprite(UnitySetSpriteEntry entry)
        {
            byte[] spriteData = this.ReadEntry(entry.SpriteLocation);
            UnitySpriteInfo sprite = UnityObjectParsers.ParseSprite(spriteData, true);
            UnityEntryLocation location = entry.TextureLocation;
            byte[] data = this.ReadEntry(location);
            UnityTexture2DInfo texture = UnityObjectParsers.ParseTexture2D(data, true);

            int x;
            int y;
            int width;
            int height;
            SpriteRectToPixels(entry.CropRect ?? sprite.Rect, out x, out y, out width, out height);
            byte[] rgba = this.GetAtlasPixels(location, texture, data);
            byte[] cropped = UnityTexture.CropRgba(texture.Width, texture.Height, rgba, x, y, width, height);
            UnityTexture.FlipVertical(cropped, width, height);
            return UnityTexture.ToPng(width, height, cropped);
        }

        private void ValidateLocation(UnityEntryLocation location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }
            if (location.BundleIndex < 0 || location.BundleIndex >= this.bundles.Count)
            {
                throw new UnityException(string.Format(
                    "bundle index {0} is out of range (total {1})", location.BundleIndex, this.bundles.Count));
            }
            UnityFsBundle bundle = this.bundles[location.BundleIndex];
            if (location.NodeIndex < 0 || location.NodeIndex >= bundle.Nodes.Count)
            {
                throw new UnityException(string.Format(
                    "node index {0} is out of range (total {1})", location.NodeIndex, bundle.Nodes.Count));
            }
            UnityFsBundle.BundleNode node = bundle.Nodes[location.NodeIndex];
            long end = location.AbsoluteOffset + location.ByteSize;
            if (location.AbsoluteOffset < 0 || end > node.Size)
            {
                throw new UnityException(string.Format(
                    "The object range {0}..{1} exceeds the {2} bytes of the node.",
                    location.AbsoluteOffset, end, node.Size));
            }
        }

        private byte[] TryGetCachedImage(string path)
        {
            lock (this.syncRoot)
            {
                byte[] image;
                if (!this.imageCache.TryGetValue(path, out image))
                {
                    return null;
                }
                this.imageCacheOrder.Remove(path);
                this.imageCacheOrder.Add(path);
                return image;
            }
        }

        private byte[] CacheImage(string path, byte[] image)
        {
            if (image.Length > ImageCacheBytes)
            {
                return image;
            }
            lock (this.syncRoot)
            {
                while (this.imageCacheOrder.Count > 0
                    && (this.imageCacheOrder.Count >= ImageCacheCapacity
                        || this.imageCacheBytes + image.Length > ImageCacheBytes))
                {
                    string evict = this.imageCacheOrder[0];
                    this.imageCacheOrder.RemoveAt(0);
                    byte[] removed;
                    if (this.imageCache.TryGetValue(evict, out removed))
                    {
                        this.imageCache.Remove(evict);
                        this.imageCacheBytes -= removed.Length;
                    }
                }
                byte[] previous;
                if (this.imageCache.TryGetValue(path, out previous))
                {
                    this.imageCacheBytes -= previous.Length;
                    this.imageCacheOrder.Remove(path);
                }
                this.imageCache[path] = image;
                this.imageCacheBytes += image.Length;
                this.imageCacheOrder.Add(path);
                return image;
            }
        }

        private static byte[] Slice(byte[] data, int offset)
        {
            if (offset < 0 || offset > data.Length)
            {
                throw new UnityException("The payload offset is out of range.");
            }
            byte[] result = new byte[data.Length - offset];
            Buffer.BlockCopy(data, offset, result, 0, result.Length);
            return result;
        }

        private static bool TrySplitSetAssetPrefix(
            ClassicCatalog catalog, string path, out string key, out string inner)
        {
            key = null;
            inner = null;
            if (path == null)
            {
                return false;
            }
            string current = path;
            while (true)
            {
                if (catalog.Image.FindSpriteAsset(current) != null)
                {
                    key = current;
                    string rest = path.Substring(current.Length);
                    if (rest.StartsWith("/", StringComparison.Ordinal))
                    {
                        rest = rest.Substring(1);
                    }
                    inner = rest;
                    return true;
                }
                int slash = current.LastIndexOf('/');
                if (slash < 0)
                {
                    return false;
                }
                current = current.Substring(0, slash);
            }
        }

        private static UnitySetSpriteEntry PickSetSprite(
            Dictionary<string, UnitySetSpriteEntry> asset, string inner)
        {
            if (string.IsNullOrEmpty(inner))
            {
                foreach (UnitySetSpriteEntry entry in asset.Values)
                {
                    return entry;
                }
                return null;
            }
            UnitySetSpriteEntry found;
            if (asset.TryGetValue(inner, out found))
            {
                return found;
            }
            string suffix = "/" + inner;
            foreach (KeyValuePair<string, UnitySetSpriteEntry> item in asset)
            {
                if (item.Key.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return item.Value;
                }
            }
            int slash = inner.LastIndexOf('/');
            string last = slash < 0 ? inner : inner.Substring(slash + 1);
            if (asset.TryGetValue(last, out found))
            {
                return found;
            }
            foreach (KeyValuePair<string, UnitySetSpriteEntry> item in asset)
            {
                int itemSlash = item.Key.LastIndexOf('/');
                string itemLast = itemSlash < 0 ? item.Key : item.Key.Substring(itemSlash + 1);
                if (string.Equals(itemLast, last, StringComparison.Ordinal))
                {
                    return item.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// The direct Canvas key in WZSS is a game-specific hash, and rects are sorted by that hash, so the WZJSON DFS
        /// order cannot be matched directly. If the same original UOL consistently points to a single frame in WZSS,
        /// the target Canvas can be resolved from WZJSON, yielding a verifiable exact mapping.
        /// </summary>
        private static bool TryInferFrameFromUol(
            WzjsNode root, string target, IReadOnlyList<WzssUolLink> links, out int frame)
        {
            frame = -1;
            Dictionary<string, SortedSet<int?>> framesByUol =
                new Dictionary<string, SortedSet<int?>>(StringComparer.Ordinal);
            foreach (WzssUolLink link in links)
            {
                SortedSet<int?> frames;
                if (!framesByUol.TryGetValue(link.Uol, out frames))
                {
                    frames = new SortedSet<int?>();
                    framesByUol[link.Uol] = frames;
                }
                frames.Add(link.FrameIndex);
            }

            SortedSet<int> candidates = new SortedSet<int>();
            // The WZJSON root is the asset itself; inner starts from the root's children and does not include the root name.
            foreach (WzjsNode child in root.Children)
            {
                WalkUol(child, string.Empty, target, framesByUol, candidates);
            }
            if (candidates.Count != 1)
            {
                return false;
            }
            foreach (int candidate in candidates)
            {
                frame = candidate;
            }
            return true;
        }

        private static void WalkUol(
            WzjsNode node, string path, string target,
            Dictionary<string, SortedSet<int?>> framesByUol, SortedSet<int> candidates)
        {
            string here = path.Length == 0 ? node.Name : path + "/" + node.Name;
            if (node.Value.Kind == WzjsValueKind.Uol)
            {
                string uol = node.Value.Text;
                if (string.Equals(ResolveUolTarget(here, uol), target, StringComparison.Ordinal))
                {
                    SortedSet<int?> frames;
                    if (framesByUol.TryGetValue(uol, out frames) && frames.Count == 1)
                    {
                        foreach (int? frame in frames)
                        {
                            if (frame.HasValue)
                            {
                                candidates.Add(frame.Value);
                            }
                        }
                    }
                }
            }
            foreach (WzjsNode child in node.Children)
            {
                WalkUol(child, here, target, framesByUol, candidates);
            }
        }

        /// <summary>UOLs are relative to the parent node of that UOL property.</summary>
        internal static string ResolveUolTarget(string source, string uol)
        {
            List<string> segments = new List<string>();
            foreach (string part in source.Split('/'))
            {
                if (part.Length > 0)
                {
                    segments.Add(part);
                }
            }
            if (segments.Count == 0)
            {
                return null;
            }
            segments.RemoveAt(segments.Count - 1);
            foreach (string part in uol.Split('/'))
            {
                if (part.Length == 0 || part == ".")
                {
                    continue;
                }
                if (part == "..")
                {
                    if (segments.Count == 0)
                    {
                        return null;
                    }
                    segments.RemoveAt(segments.Count - 1);
                }
                else
                {
                    segments.Add(part);
                }
            }
            return string.Join("/", segments);
        }

        internal static void SpriteRectToPixels(UnityRectF rect, out int x, out int y, out int width, out int height)
        {
            x = ExactPixel(rect.X, "x", true);
            y = ExactPixel(rect.Y, "y", true);
            width = ExactPixel(rect.Width, "width", false);
            height = ExactPixel(rect.Height, "height", false);
        }

        private static int ExactPixel(float value, string label, bool allowZero)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || (!allowZero && value == 0))
            {
                throw new UnityException(string.Format("Invalid sprite rect {0}: {1}", label, value));
            }
            float rounded = (float)Math.Round(value);
            if (Math.Abs(value - rounded) > 0.001 || rounded > int.MaxValue)
            {
                throw new UnityException(string.Format(
                    "Sprite rect {0} is not made of valid integer pixels: {1}", label, value));
            }
            return (int)rounded;
        }
    }
}


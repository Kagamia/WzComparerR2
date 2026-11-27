using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.UnityLib.Audio;
using WzComparerR2.UnityLib.Images;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.UnityLib.Wzjs;

namespace WzComparerR2.UnityLib.Classic
{
    public enum ClassicChildKind
    {
        Category,
        Image,
        Node,
    }

    /// <summary>One result of <see cref="ClassicWzSource.ListChildren"/>.</summary>
    public sealed class ClassicChildEntry
    {
        internal ClassicChildEntry(
            ClassicChildKind kind, string name, string path,
            int imageCount, int classId, int typeTag, bool hasChildren)
        {
            this.Kind = kind;
            this.Name = name;
            this.Path = path;
            this.ImageCount = imageCount;
            this.ClassId = classId;
            this.TypeTag = typeTag;
            this.HasChildren = hasChildren;
        }

        public ClassicChildKind Kind { get; private set; }
        public string Name { get; private set; }
        public string Path { get; private set; }

        /// <summary>Directory node: total number of imgs beneath it.</summary>
        public int ImageCount { get; private set; }

        /// <summary>Img node: Unity classID (114 = WZJS, 49 = TextAsset, 83 = AudioClip).</summary>
        public int ClassId { get; private set; }

        /// <summary>Type tag of a node inside the WZJS tree.</summary>
        public int TypeTag { get; private set; }

        public bool HasChildren { get; private set; }
    }

    public struct ClassicNodeChild
    {
        public ClassicNodeChild(string name, int typeTag, bool hasChildren)
        {
            this.Name = name;
            this.TypeTag = typeTag;
            this.HasChildren = hasChildren;
        }

        public string Name { get; private set; }
        public int TypeTag { get; private set; }
        public bool HasChildren { get; private set; }
    }

    /// <summary>Detailed content of a single node (for the right-hand pane).</summary>
    public sealed class ClassicNodeDetail
    {
        internal ClassicNodeDetail(
            string path, string name, int typeTag, WzjsValue value, string nodePath,
            IReadOnlyList<ClassicNodeChild> children, string textAsset)
        {
            this.Path = path;
            this.Name = name;
            this.TypeTag = typeTag;
            this.Value = value;
            this.NodePath = nodePath;
            this.Children = children;
            this.TextAsset = textAsset;
        }

        public string Path { get; private set; }
        public string Name { get; private set; }
        public int TypeTag { get; private set; }
        public WzjsValue Value { get; private set; }

        /// <summary>WZJS pool B path (relative path inside the img).</summary>
        public string NodePath { get; private set; }

        public IReadOnlyList<ClassicNodeChild> Children { get; private set; }

        /// <summary>Plain JSON of a TextAsset (class 49); null for WZJS nodes.</summary>
        public string TextAsset { get; private set; }
    }

    /// <summary>
    /// An opened Classic World data set: the catalog, the bundle handles, and a cache of decoded WZJS trees.
    /// <para>
    /// Public logical paths: everything before the img is a directory (produced by grouping index keys), the
    /// entry whose key matches is the img, and the segments after the img are navigated through the WZJS tree
    /// (such as <c>Character/00020116/angry/0/face</c>).
    /// </para>
    /// <para>
    /// Image sources (WZSS / SpriteSheet asset / spriteset) are decoded into PNGs by the <c>unity::texture</c>
    /// pipeline; AudioClips only have their metadata parsed, and FSB5 → Ogg repacking is outside this project.
    /// </para>
    /// </summary>
    public sealed partial class ClassicWzSource : IDisposable
    {
        private const int TreeCacheCapacity = 32;

        private static readonly string[] PreferredLocales = new[] { "TW", "CN", "JP", "KR" };

        private readonly List<UnityFsBundle> bundles;
        private readonly object treeCacheLock = new object();
        private readonly Dictionary<string, WzjsNode> treeCache = new Dictionary<string, WzjsNode>(StringComparer.Ordinal);
        private readonly List<string> treeCacheOrder = new List<string>();
        private readonly Dictionary<string, UnityWzImage> imageCache = new Dictionary<string, UnityWzImage>(StringComparer.Ordinal);
        private readonly Dictionary<string, UnitySoundImage> soundImageCache = new Dictionary<string, UnitySoundImage>(StringComparer.Ordinal);
        private readonly Dictionary<string, byte[]> soundCache = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        private readonly List<string> soundCacheOrder = new List<string>();
        private long soundCacheBytes;
        private const long SoundCacheByteLimit = 32L * 1024 * 1024;
        private readonly UnityImageRenderer renderer;
        private readonly object stringTablesLock = new object();
        private ClassicStringLinkerTables stringTables;
        private bool stringTablesUnavailable;
        private string locale;
        private long localeGeneration;

        private ClassicWzSource(ClassicCatalog catalog, List<UnityFsBundle> bundles, string locale, IReadOnlyList<string> locales)
        {
            this.Catalog = catalog;
            catalog.RebuildSpritesetEntries(locale);
            this.bundles = bundles;
            this.locale = locale;
            this.Locales = locales;
            this.renderer = new UnityImageRenderer(catalog, bundles, this.GetTree, locale);
        }

        public ClassicCatalog Catalog { get; private set; }

        public IReadOnlyList<UnityFsBundle> Bundles
        {
            get { return this.bundles; }
        }

        /// <summary>Locale codes detected in the data set (TW/CN/JP/KR…), with preferred ones first.</summary>
        public IReadOnlyList<string> Locales { get; private set; }

        /// <summary>Client information (name / version / engine version). Each field is null when it cannot be parsed.</summary>
        public UnityClientInfo ClientInfo { get; private set; }

        /// <summary>Currently active locale code (the second segment of String / Etc asset paths).</summary>
        public string Locale
        {
            get { return this.locale; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (string.Equals(this.locale, value, StringComparison.Ordinal))
                {
                    return;
                }
                this.locale = value;
                this.localeGeneration++;
                this.renderer.Locale = value;
                this.renderer.ClearCache();
                this.Catalog.RebuildSpritesetEntries(value);
                lock (this.stringTablesLock)
                {
                    // String tables depend on the locale, so rebuild them.
                    this.stringTables = null;
                    this.stringTablesUnavailable = false;
                }
            }
        }

        /// <summary>
        /// String tables that organize Unity's <c>String/&lt;region&gt;/*</c> into the WZ layout
        /// (such as <c>String/Consume.img</c>). Built on first access and discarded when the locale changes.
        /// </summary>
        public ClassicStringLinkerTables StringTables
        {
            get
            {
                ClassicStringLinkerTables tables = this.stringTables;
                if (tables != null)
                {
                    return tables;
                }
                lock (this.stringTablesLock)
                {
                    if (this.stringTables == null)
                    {
                        this.stringTables = ClassicStringLinkerTables.Build(this);
                    }
                    return this.stringTables;
                }
            }
        }

        /// <summary>
        /// Number of locale switches. Node-level caches (such as the PNGs already decoded by <see cref="UnityCanvas"/>)
        /// use this to decide whether they need to be decoded again.
        /// </summary>
        public long LocaleGeneration
        {
            get { return this.localeGeneration; }
        }

        /// <summary>Opens a data directory: scans every .bundle file to build the index, then opens them one by one.</summary>
        public static ClassicWzSource Open(string rootDirectory)
        {
            ClassicCatalog catalog = ClassicCatalog.Build(rootDirectory);
            List<UnityFsBundle> bundles = new List<UnityFsBundle>(catalog.Bundles.Count);
            try
            {
                foreach (ClassicBundleMeta meta in catalog.Bundles)
                {
                    bundles.Add(UnityFsBundle.Open(
                        System.IO.Path.Combine(catalog.RootPath, meta.Name + ".bundle")));
                }
            }
            catch
            {
                foreach (UnityFsBundle bundle in bundles)
                {
                    bundle.Dispose();
                }
                throw;
            }

            List<string> locales = DetectLocales(catalog);
            ClassicWzSource source = new ClassicWzSource(catalog, bundles, locales[0], locales);
            source.ClientInfo = UnityClientInfo.Resolve(
                catalog.RootPath,
                bundles.Count > 0 ? bundles[0].UnityRevision : null);
            return source;
        }

        public void Dispose()
        {
            foreach (UnityFsBundle bundle in this.bundles)
            {
                bundle.Dispose();
            }
        }

        /// <summary>Builds a string table path for the current locale, such as <c>String/TW/Item</c>.</summary>
        public string StringPath(string leaf)
        {
            return "String/" + this.locale + "/" + leaf;
        }

        /// <summary>Builds an Etc asset path for the current locale, such as <c>Etc/TW/Commodity</c>.</summary>
        public string EtcPath(string leaf)
        {
            return "Etc/" + this.locale + "/" + leaf;
        }

        /// <summary>Reads the object bytes that <see cref="UnityEntryLocation"/> points to.</summary>
        public byte[] ReadEntry(UnityEntryLocation location)
        {
            return this.renderer.ReadEntry(location);
        }

        /// <summary>Lists the immediate children of path ("" = top level).</summary>
        public IReadOnlyList<ClassicChildEntry> ListChildren(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            List<ClassicChildEntry> output = new List<ClassicChildEntry>();

            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(path, out location))
            {
                string ignoredKey;
                string ignoredInner;
                if (!this.Catalog.TrySplitImagePrefix(path, out ignoredKey, out ignoredInner))
                {
                    foreach (ClassicIndexChild child in this.Catalog.ListIndexChildren(path))
                    {
                        if (child.IsImage)
                        {
                            int classId = 0;
                            UnityEntryLocation imageLocation;
                            if (this.Catalog.Entries.TryGetValue(child.Path, out imageLocation))
                            {
                                classId = imageLocation.ClassId;
                            }
                            output.Add(new ClassicChildEntry(
                                ClassicChildKind.Image, child.Name, child.Path, child.ImageCount, classId, 0, false));
                        }
                        else
                        {
                            output.Add(new ClassicChildEntry(
                                ClassicChildKind.Category, child.Name, child.Path, child.ImageCount, 0, 0, false));
                        }
                    }
                    return output;
                }
            }

            // img or inside an img
            string imageKey;
            string inner;
            if (!this.Catalog.TrySplitImagePrefix(path, out imageKey, out inner))
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
            }
            if (this.IsSpriteEntry(imageKey))
            {
                // A sprite in a spriteset corresponds to a WZ canvas: a leaf node with no children.
                if (inner.Length > 0)
                {
                    throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
                }
                return output;
            }
            if (this.Catalog.IsSoundImage(imageKey))
            {
                UnitySoundImage soundImage = this.GetSoundImage(imageKey);
                WzLib.Wz_Node soundNode = soundImage == null ? null : this.FindSoundNode(soundImage.AsWzNode(), inner);
                if (soundNode == null)
                {
                    throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
                }
                string soundBase = inner.Length == 0 ? imageKey : imageKey + "/" + inner;
                foreach (WzLib.Wz_Node child in soundNode.Nodes)
                {
                    output.Add(new ClassicChildEntry(
                        ClassicChildKind.Node, child.Text, soundBase + "/" + child.Text,
                        0, UnityClassIds.AudioClip, 0, child.Nodes.Count > 0));
                }
                return output;
            }
            UnityEntryLocation imageLocation2;
            if (this.Catalog.Entries.TryGetValue(imageKey, out imageLocation2)
                && (imageLocation2.ClassId == UnityClassIds.TextAsset || imageLocation2.ClassId == UnityClassIds.AudioClip))
            {
                return output; // TextAssets / AudioClips have no child nodes
            }

            WzjsNode root = this.GetTree(imageKey);
            WzjsNode node = inner.Length == 0
                ? root
                : root.Find(inner);
            if (node == null)
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
            }
            string basePath = inner.Length == 0 ? imageKey : imageKey + "/" + inner;

            List<WzjsNode> children = new List<WzjsNode>(node.Children);
            children.Sort((a, b) => ClassicCatalog.CompareNodeNames(a.Name, b.Name));
            foreach (WzjsNode child in children)
            {
                output.Add(new ClassicChildEntry(
                    ClassicChildKind.Node, child.Name, basePath + "/" + child.Name,
                    0, UnityClassIds.MonoBehaviour, child.TypeTag, child.Children.Count > 0));
            }
            return output;
        }

        /// <summary>Gets the detailed content of a single node (for the right-hand pane).</summary>
        public ClassicNodeDetail GetNode(string path)
        {
            string imageKey;
            string inner;
            if (!this.Catalog.TrySplitImagePrefix(path, out imageKey, out inner))
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
            }
            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + imageKey);
            }

            if (location.ClassId == UnityClassIds.TextAsset)
            {
                if (inner.Length > 0)
                {
                    throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
                }
                UnityTextAsset textAsset = this.ReadTextAsset(imageKey);
                return new ClassicNodeDetail(
                    path, textAsset.Name, UnityClassIds.TextAsset, WzjsValue.SubTree, null,
                    new ClassicNodeChild[0], Encoding.UTF8.GetString(textAsset.Script));
            }

            if (location.ClassId == UnityClassIds.AudioClip)
            {
                // Sound img: inner walks the WZ node tree (Sound/Bgm00/FloralLife).
                if (this.Catalog.IsSoundImage(imageKey))
                {
                    WzLib.Wz_Node soundNode = this.FindSoundNode(this.GetSoundImage(imageKey).AsWzNode(), inner);
                    if (soundNode == null)
                    {
                        throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
                    }
                    List<ClassicNodeChild> soundChildren = new List<ClassicNodeChild>(soundNode.Nodes.Count);
                    foreach (WzLib.Wz_Node child in soundNode.Nodes)
                    {
                        soundChildren.Add(new ClassicNodeChild(child.Text, 0, child.Nodes.Count > 0));
                    }
                    return new ClassicNodeDetail(
                        path, soundNode.Text, 0, WzjsValue.SubTree, null, soundChildren, null);
                }

                if (inner.Length > 0)
                {
                    throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
                }
                UnityAudioClipInfo clip = this.GetAudioClip(imageKey);
                return new ClassicNodeDetail(
                    path, clip.Name, UnityClassIds.AudioClip, WzjsValue.SubTree, null,
                    new ClassicNodeChild[0], null);
            }

            if (location.ClassId == UnityClassIds.Sprite)
            {
                // A sprite in a spriteset corresponds to a WZ canvas node.
                if (inner.Length > 0)
                {
                    throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
                }
                return new ClassicNodeDetail(
                    path, GetLeafName(imageKey), WzjsImage.TypeCanvas, WzjsValue.Canvas, null,
                    new ClassicNodeChild[0], null);
            }

            WzjsNode root = this.GetTree(imageKey);
            WzjsNode node = inner.Length == 0 ? root : root.Find(inner);
            if (node == null)
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
            }
            List<ClassicNodeChild> children = new List<ClassicNodeChild>(node.Children.Count);
            foreach (WzjsNode child in node.Children)
            {
                children.Add(new ClassicNodeChild(child.Name, child.TypeTag, child.Children.Count > 0));
            }
            return new ClassicNodeDetail(
                path, node.Name, node.TypeTag, node.Value, node.Path, children, null);
        }

        /// <summary>Reads the AudioClip object itself (without the sound data).</summary>
        public UnityAudioClipInfo GetAudioClip(string path)
        {
            string imageKey;
            string inner;
            if (!this.Catalog.TrySplitImagePrefix(path, out imageKey, out inner) || inner.Length > 0)
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
            }

            UnityEntryLocation location;
            if (this.Catalog.IsSoundImage(imageKey))
            {
                // Sound img: use the AudioClip of the first sound (the img itself has no concrete object).
                IReadOnlyList<string> keys = this.Catalog.GetSoundKeys(imageKey);
                if (keys.Count == 0 || !this.Catalog.SoundEntries.TryGetValue(keys[0], out location))
                {
                    throw new ClassicAssetException("The path does not exist or cannot be parsed: " + imageKey);
                }
            }
            else if (!this.Catalog.SoundEntries.TryGetValue(imageKey, out location)
                && !this.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + imageKey);
            }

            byte[] data = this.ReadEntry(location);
            return UnityObjectParsers.ParseAudioClip(data, true);
        }

        /// <summary>
        /// Gets the sound data (Ogg Vorbis). A Unity AudioClip internally holds FSB5, so here it is
        /// repackaged into a playable/exportable Ogg. The result is cached.
        /// </summary>
        public byte[] GetSoundData(string soundKey)
        {
            if (string.IsNullOrEmpty(soundKey))
            {
                throw new ArgumentNullException(nameof(soundKey));
            }
            lock (this.soundCache)
            {
                byte[] cached;
                if (this.soundCache.TryGetValue(soundKey, out cached))
                {
                    return cached;
                }
            }

            byte[] ogg = this.DecodeSound(soundKey);
            lock (this.soundCache)
            {
                if (!this.soundCache.ContainsKey(soundKey))
                {
                    this.soundCache[soundKey] = ogg;
                    this.soundCacheOrder.Add(soundKey);
                    this.soundCacheBytes += ogg.Length;
                    // Sounds are several hundred KB, so keep only recently used ones to avoid unbounded memory growth during long browsing sessions.
                    while (this.soundCacheBytes > SoundCacheByteLimit && this.soundCacheOrder.Count > 1)
                    {
                        string evict = this.soundCacheOrder[0];
                        this.soundCacheOrder.RemoveAt(0);
                        byte[] removed;
                        if (this.soundCache.TryGetValue(evict, out removed))
                        {
                            this.soundCacheBytes -= removed.Length;
                            this.soundCache.Remove(evict);
                        }
                    }
                }
            }
            return ogg;
        }

        private byte[] DecodeSound(string soundKey)
        {
            UnityEntryLocation location;
            if (!this.Catalog.SoundEntries.TryGetValue(soundKey, out location))
            {
                throw new ClassicAssetException("Sound not found: " + soundKey);
            }

            byte[] data = this.ReadEntry(location);
            UnityAudioClipInfo clip = UnityObjectParsers.ParseAudioClip(data, true);
            UnityFsBundle bundle = this.bundles[location.BundleIndex];
            int resourceNode = bundle.FindNode(clip.ResourceNode);
            if (resourceNode < 0)
            {
                throw new UnityException(string.Format(
                    "The resource node of sound {0} does not exist: {1}", soundKey, clip.ResourceNode));
            }
            if (clip.ResourceSize <= 0 || clip.ResourceSize > int.MaxValue)
            {
                throw new UnityException("Invalid sound resource size: " + clip.ResourceSize);
            }
            byte[] fsb = bundle.ReadNodeRange(
                resourceNode, (long)clip.ResourceOffset, (int)clip.ResourceSize);
            return Fsb5ToOgg.ToOgg(fsb);
        }

        /// <summary>Creates the <see cref="WzLib.Wz_Sound"/> corresponding to a sound path.</summary>
        public WzLib.Wz_Sound CreateSound(string soundKey)
        {
            UnityEntryLocation location;
            if (!this.Catalog.SoundEntries.TryGetValue(soundKey, out location))
            {
                throw new ClassicAssetException("Sound not found: " + soundKey);
            }

            UnityAudioClipInfo clip = UnityObjectParsers.ParseAudioClip(this.ReadEntry(location), true);
            string imageKey = soundKey.Substring(0, soundKey.LastIndexOf('/'));
            int ms = (int)Math.Round(clip.Length * 1000);
            return new WzLib.Wz_Sound(0, 0, ms, null, this.GetSoundImage(imageKey))
            {
                ExternalChannels = clip.Channels,
                ExternalFrequency = clip.Frequency,
                ExternalMs = ms,
                ExternalSoundType = WzLib.Wz_SoundType.Ogg,
                DataProvider = () => this.GetSoundData(soundKey),
            };
        }

        /// <summary>Gets the sound img (cached).</summary>
        public UnitySoundImage GetSoundImage(string imageKey)
        {
            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }
            lock (this.soundImageCache)
            {
                UnitySoundImage cached;
                if (this.soundImageCache.TryGetValue(imageKey, out cached))
                {
                    return cached;
                }
            }

            if (!this.Catalog.IsSoundImage(imageKey))
            {
                return null;
            }

            UnitySoundImage image = new UnitySoundImage(this, imageKey);
            lock (this.soundImageCache)
            {
                UnitySoundImage existing;
                if (this.soundImageCache.TryGetValue(imageKey, out existing))
                {
                    return existing;
                }
                this.soundImageCache[imageKey] = image;
            }
            return image;
        }

        /// <summary>Reads a TextAsset (class 49): returns the plain JSON directly.</summary>
        public UnityTextAsset ReadTextAsset(string imageKey)
        {
            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + imageKey);
            }
            byte[] data = this.ReadEntry(location);
            return UnityObjectParsers.ParseTextAsset(data, true);
        }

        /// <summary>Gets the image (PNG bytes) of a canvas or uol node. Returns null when no image is found.</summary>
        public byte[] GetImage(string path)
        {
            return this.renderer.GetImage(path);
        }

        /// <summary>Whole-page atlas preview (PNG bytes) used when the exact frame cannot be decoded.</summary>
        public byte[] GetCanvasAtlasFallback(string path)
        {
            return this.renderer.GetCanvasAtlasFallback(path);
        }

        /// <summary>Decodes (or returns from cache) the WZJS tree of an img.</summary>
        public WzjsNode GetTree(string imageKey)
        {
            if (imageKey == null)
            {
                throw new ArgumentNullException(nameof(imageKey));
            }
            lock (this.treeCacheLock)
            {
                WzjsNode cached;
                if (this.treeCache.TryGetValue(imageKey, out cached))
                {
                    this.treeCacheOrder.Remove(imageKey);
                    this.treeCacheOrder.Add(imageKey);
                    return cached;
                }
            }

            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + imageKey);
            }
            if (location.ClassId != UnityClassIds.MonoBehaviour)
            {
                throw new ClassicAssetException(string.Format(
                    "{0} is not a WZJS (class {1}) and cannot be expanded.", imageKey, location.ClassId));
            }

            byte[] payload = this.ReadEntry(location);
            UnityMonoBehaviourInfo mono = UnityObjectParsers.ParseMonoBehaviour(payload, true);
            if (mono.HeaderLength > payload.Length)
            {
                throw new UnityException("MonoBehaviour header out of range: " + imageKey);
            }
            byte[] wzjsPayload = new byte[payload.Length - mono.HeaderLength];
            Buffer.BlockCopy(payload, mono.HeaderLength, wzjsPayload, 0, wzjsPayload.Length);
            WzjsNode tree = WzjsImage.Parse(wzjsPayload);

            lock (this.treeCacheLock)
            {
                WzjsNode existing;
                if (this.treeCache.TryGetValue(imageKey, out existing))
                {
                    return existing;
                }
                if (this.treeCacheOrder.Count >= TreeCacheCapacity)
                {
                    string evict = this.treeCacheOrder[0];
                    this.treeCacheOrder.RemoveAt(0);
                    this.treeCache.Remove(evict);
                }
                this.treeCacheOrder.Add(imageKey);
                this.treeCache[imageKey] = tree;
            }
            return tree;
        }

        /// <summary>
        /// Total action frame delay: sums the absolute values of <c>&lt;imgKey&gt;/&lt;actionName&gt;/&lt;frame&gt;/delay</c>.
        /// Returns null when the img or the action is not found.
        /// </summary>
        public int? GetActionDelay(string imageKey, string actionName)
        {
            if (string.IsNullOrEmpty(imageKey) || string.IsNullOrEmpty(actionName))
            {
                return null;
            }
            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location)
                || location.ClassId != UnityClassIds.MonoBehaviour)
            {
                return null;
            }

            WzjsNode actionNode = this.GetTree(imageKey).FindChild(actionName);
            if (actionNode == null)
            {
                return null;
            }
            return ClassicWzBridge.SumActionDelay(ClassicWzBridge.ConvertNode(actionNode));
        }

        /// <summary>
        /// Gets the <see cref="UnityWzImage"/> of an img (internally cached; null for non-WZJS imgs).
        /// </summary>
        public UnityWzImage GetWzImage(string imageKey)
        {
            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }
            lock (this.imageCache)
            {
                UnityWzImage cached;
                if (this.imageCache.TryGetValue(imageKey, out cached))
                {
                    return cached;
                }
            }

            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location)
                || location.ClassId != UnityClassIds.MonoBehaviour)
            {
                // Directories that WZ exposes as a single img (for example, Map/MapHelper.img) are
                // assembled from the index of their individual sprites.
                return ClassicWzBridge.IsWzImagedDirectory(imageKey) ? this.GetDirectoryImage(imageKey) : null;
            }

            UnityWzImage image = new UnityWzImage(this, imageKey);
            lock (this.imageCache)
            {
                UnityWzImage existing;
                if (this.imageCache.TryGetValue(imageKey, out existing))
                {
                    return existing;
                }
                this.imageCache[imageKey] = image;
            }
            return image;
        }

        /// <summary>
        /// Returns a directory that WZ exposes as a single img (for example, <c>Map/MapHelper</c>) as one img
        /// (internally cached).
        /// </summary>
        public UnityDirectoryImage GetDirectoryImage(string directoryPath)
        {
            if (!ClassicWzBridge.IsWzImagedDirectory(directoryPath)
                || this.Catalog.Entries.ContainsKey(directoryPath))
            {
                // Not used for data sets that have a real entry (a WZJS img) with the same key.
                return null;
            }

            UnityWzImage cached;
            if (this.imageCache.TryGetValue(directoryPath, out cached))
            {
                return cached as UnityDirectoryImage;
            }

            UnityDirectoryImage image = new UnityDirectoryImage(this, directoryPath);
            lock (this.imageCache)
            {
                UnityWzImage existing;
                if (this.imageCache.TryGetValue(directoryPath, out existing))
                {
                    return existing as UnityDirectoryImage;
                }
                this.imageCache[directoryPath] = image;
            }
            return image;
        }

        /// <summary>Whether the catalog index key is a single sprite of a spriteset.</summary>
        private bool IsSpriteEntry(string key)
        {
            UnityEntryLocation location;
            return this.Catalog.Entries.TryGetValue(key, out location)
                && location.ClassId == UnityClassIds.Sprite;
        }

        private WzLib.Wz_Node CreateSpriteNode(string key)
        {
            string[] segments = key.Split('/');
            WzLib.Wz_Node sprite = new WzLib.Wz_Node(segments[segments.Length - 1]);
            sprite.Value = new UnityCanvas(this, key);
            AttachSpriteOrigin(sprite, this, key);

            // Fill in the ancestor directories so that a WZ-style path can be resolved again from
            // Wz_Node.FullPathToFile (needed by code that fetches textures through PluginManager.FindWz, such as MapRender).
            WzLib.Wz_Node node = sprite;
            for (int i = segments.Length - 2; i >= 0; i--)
            {
                WzLib.Wz_Node parent = new WzLib.Wz_Node(segments[i]);
                parent.Nodes.Add(node);
                node = parent;
            }
            return sprite;
        }

        /// <summary>
        /// Attaches the WZ <c>origin</c> child node to a spriteset sprite node.
        /// MapRender and others read the frame's reference point from this child node.
        /// </summary>
        public static void AttachSpriteOrigin(WzLib.Wz_Node node, ClassicWzSource source, string key)
        {
            if (node == null || source == null || string.IsNullOrEmpty(key))
            {
                return;
            }
            UnitySpriteOrigin origin;
            if (!source.Catalog.SpritesetOrigins.TryGetValue(key, out origin))
            {
                return;
            }
            if (node.Nodes["origin"] != null)
            {
                return;
            }
            WzLib.Wz_Node originNode = new WzLib.Wz_Node("origin");
            originNode.Value = new WzLib.Wz_Vector(origin.X, origin.Y);
            node.Nodes.Add(originNode);
        }

        private static string GetLeafName(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash < 0 ? path : path.Substring(slash + 1);
        }

        /// <summary>
        /// Searches for a node using a WZ-style path (a <c>.img</c> suffix and <c>\</c> separators are also accepted).
        /// Example: <c>Map\Map\Map0\100000000.img\miniMap</c>. Returns null when not found.
        /// </summary>
        public WzLib.Wz_Node FindNode(string wzPath)
        {
            string normalized = NormalizeWzPath(wzPath);
            if (normalized == null)
            {
                return null;
            }

           WzLib.Wz_Node node = this.FindNodeCore(normalized)
               ?? this.FindStringTableNode(normalized)
               ?? this.FindScriptStringNode(normalized);
            if (node == null)
            {
                // Nodes of the index tree return paths that start with the root name (the client name),
                // so search again with that first segment removed.
                int slash = normalized.IndexOf('/');
                if (slash > 0)
                {
                    string stripped = normalized.Substring(slash + 1);
                   node = this.FindNodeCore(stripped)
                       ?? this.FindStringTableNode(stripped)
                       ?? this.FindScriptStringNode(stripped);
                }
            }
            return node;
        }

        /// <summary>
        /// Also resolves client script strings (<c>String/&lt;region&gt;/StringTable/SID_...</c>) through a path that
        /// omits the region code (<c>String/StringTable/SID_...</c>).
        /// The current locale is tried first; other locales are tried when not found. Returns null when there is no match.
        /// </summary>
        private WzLib.Wz_Node FindScriptStringNode(string normalized)
        {
           string[] segments = normalized.Split('/');
           if (segments.Length != 3
               || !string.Equals(segments[0], "String", StringComparison.OrdinalIgnoreCase)
               || !string.Equals(segments[1], "StringTable", StringComparison.OrdinalIgnoreCase))
           {
               return null;
           }

           string key = segments[2];
           WzLib.Wz_Node node = this.FindNodeCore($"{segments[0]}/{this.locale}/{segments[1]}/{key}");
           if (node != null)
           {
               return node;
           }
           if (this.Locales == null)
           {
               return null;
           }
           foreach (string locale in this.Locales)
           {
               if (string.Equals(locale, this.locale, StringComparison.Ordinal))
               {
                   continue;
               }
               node = this.FindNodeCore($"{segments[0]}/{locale}/{segments[1]}/{key}");
               if (node != null)
               {
                   return node;
               }
           }
           return null;
        }

        /// <summary>
        /// Resolves paths corresponding to WZ's <c>String.wz</c> / <c>Etc.wz</c> / <c>Quest.wz</c> from the
        /// Unity string tables (<see cref="StringTables"/>). Returns null when not found.
        /// </summary>
        private WzLib.Wz_Node FindStringTableNode(string normalized)
        {
            string[] segments = normalized.Split('/');
            if (segments.Length < 2)
            {
                return null;
            }

            ClassicStringLinkerTables tables;
            if (this.stringTablesUnavailable)
            {
                return null;
            }
            try
            {
                tables = this.StringTables;
            }
            catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException || ex is WzjsException)
            {
                // For data sets whose string tables cannot be read, stop trying from then on.
                this.stringTablesUnavailable = true;
                return null;
            }
            if (tables == null)
            {
                return null;
            }

            WzLib.Wz_Node node;
            switch (segments[0])
            {
                case "String":
                    node = tables.StringNode;
                    break;
                case "Etc":
                    node = tables.EtcNode;
                    break;
                case "Quest":
                    node = tables.QuestNode;
                    break;
                default:
                    return null;
            }
            if (node == null)
            {
                return null;
            }

            for (int i = 1; i < segments.Length; i++)
            {
                // Leaf nodes of the tables are named "<name>.img", same as in WZ.
                string name = segments[i];
                node = FindChildByName(node, name) ?? FindChildByName(node, name + ".img");
                if (node == null)
                {
                    return null;
                }
            }
            return node;
        }

        private WzLib.Wz_Node FindNodeCore(string normalized)
        {
            string imageKey, inner;
            if (!this.Catalog.TrySplitImagePrefix(normalized, out imageKey, out inner))
            {
                // Resolve inside directories that WZ exposes as an img (such as Map/MapHelper.img).
                WzLib.Wz_Node directoryNode = this.FindDirectoryImageNode(normalized);
                if (directoryNode != null)
                {
                    return directoryNode;
                }
                // The directory layout differs from WZ (for example Character/<id>, Item/<type>/<group>/<id>):
                // generate catalog path candidates from the id instead.
                return this.FindNodeByCandidates(normalized);
            }
            if (this.IsSpriteEntry(imageKey))
            {
                // A single sprite in a spriteset: the node value is a UnityCanvas, which fetches the image for the locale when decoded.
                if (!string.IsNullOrEmpty(inner))
                {
                    return null;
                }
                return CreateSpriteNode(imageKey);
            }
            if (this.Catalog.IsSoundImage(imageKey))
            {
                UnitySoundImage soundImage = this.GetSoundImage(imageKey);
                if (soundImage == null)
                {
                    return null;
                }
                return RootAssetNode(this.FindSoundNode(soundImage.AsWzNode(), inner), soundImage);
            }
            UnityWzImage image = this.GetWzImage(imageKey);
            if (image == null)
            {
                return null;
            }

            WzLib.Wz_Node node = image.AsWzNode();
            if (string.IsNullOrEmpty(inner))
            {
                return RootAssetNode(node, image);
            }
            foreach (string segment in inner.Split('/'))
            {
                if (segment.Length == 0)
                {
                    continue;
                }
                node = FindChildByName(node, segment);
                if (node == null)
                {
                    return null;
                }
            }
            return RootAssetNode(node, image);
        }

        /// <summary>
        /// Fills in a parent node chain with the same names as the directory above an img content node, so that
        /// WZ-style path checks such as <c>FullPathToFile</c> (for example MapRender's resource cache keys) can keep using the WZ logic.
        /// </summary>
        private static WzLib.Wz_Node RootAssetNode(WzLib.Wz_Node node, UnityAssetImage image)
        {
            if (node == null || image == null)
            {
                return node;
            }
            image.GetRootedNode(); // idempotent: returns as-is once the parent chain has been filled in
            return node;
        }

        /// <summary>
        /// Resolves inside a directory that WZ exposes as a single img (for example, <c>Map/MapHelper</c>).
        /// Returns null when not found.
        /// </summary>
        private WzLib.Wz_Node FindDirectoryImageNode(string normalized)
        {
            string directoryPath = this.GetWzImagedDirectoryPath(normalized);
            if (directoryPath == null)
            {
                return null;
            }
            UnityDirectoryImage image = this.GetDirectoryImage(directoryPath);
            if (image == null)
            {
                return null;
            }
            WzLib.Wz_Node node = image.AsWzNode();
            if (normalized.Length > directoryPath.Length)
            {
                foreach (string segment in normalized.Substring(directoryPath.Length + 1).Split('/'))
                {
                    if (segment.Length == 0)
                    {
                        continue;
                    }
                    node = FindChildByName(node, segment);
                    if (node == null)
                    {
                        return null;
                    }
                }
            }
            return RootAssetNode(node, image);
        }

        /// <summary>
        /// Returns the "directory treated as an img" that <paramref name="normalized"/> belongs to (null when none applies).
        /// </summary>
        private string GetWzImagedDirectoryPath(string normalized)
        {
            foreach (string directoryPath in ClassicWzBridge.WzImagedDirectories)
            {
                if (string.Equals(normalized, directoryPath, StringComparison.Ordinal)
                    || normalized.StartsWith(directoryPath + "/", StringComparison.Ordinal))
                {
                    return directoryPath;
                }
            }
            foreach (string parentPath in ClassicWzBridge.WzImagedDirectoryParents)
            {
                if (!normalized.StartsWith(parentPath + "/", StringComparison.Ordinal))
                {
                    continue;
                }
                int slash = normalized.IndexOf('/', parentPath.Length + 1);
                return slash < 0 ? normalized : normalized.Substring(0, slash);
            }
            return null;
        }

        private WzLib.Wz_Node FindSoundNode(WzLib.Wz_Node root, string inner)
        {
            if (root == null || string.IsNullOrEmpty(inner))
            {
                return root;
            }
            WzLib.Wz_Node node = root;
            foreach (string segment in inner.Split('/'))
            {
                if (segment.Length == 0)
                {
                    continue;
                }
                node = FindChildByName(node, segment);
                if (node == null)
                {
                    return null;
                }
            }
            return node;
        }

        /// <summary>
        /// Generates catalog path candidates from the tail of the path (the id of an item / equipment / skill…)
        /// and then searches. Used when the directory layout differs between WZ and Unity.
        /// </summary>
        private WzLib.Wz_Node FindNodeByCandidates(string normalized)
        {
            int slash = normalized.LastIndexOf('/');
            if (slash <= 0)
            {
                return null;
            }
            int id;
            if (!int.TryParse(normalized.Substring(slash + 1), out id))
            {
                return null;
            }

            int rootEnd = normalized.IndexOf('/');
            List<string> candidates = new List<string>();
            switch (normalized.Substring(0, rootEnd))
            {
                case "Item":
                    candidates.AddRange(this.Catalog.GetItemPathCandidates(id));
                    candidates.AddRange(this.Catalog.GetEquipPathCandidates(id)); // equipment lives under Character, but some callers build Item paths
                    break;
                case "Character":
                    candidates.AddRange(this.Catalog.GetEquipPathCandidates(id));
                    break;
                case "Mob":
                    candidates.AddRange(this.Catalog.GetMobPathCandidates(id));
                    break;
                case "Npc":
                    candidates.AddRange(this.Catalog.GetNpcPathCandidates(id));
                    break;
                case "Map":
                    candidates.AddRange(this.Catalog.GetMapPathCandidates(id));
                    break;
                case "Quest":
                    candidates.AddRange(this.Catalog.GetQuestPathCandidates(id));
                    break;
                case "Skill":
                    candidates.AddRange(this.Catalog.GetSkillPathCandidates(id));
                    break;
            }

            foreach (string candidate in candidates)
            {
                string imageKey, inner;
                if (!this.Catalog.TrySplitImagePrefix(candidate, out imageKey, out inner))
                {
                    continue;
                }
                UnityWzImage image = this.GetWzImage(imageKey);
                if (image == null)
                {
                    continue;
                }
                WzLib.Wz_Node node = image.AsWzNode();
                foreach (string segment in inner.Split('/'))
                {
                    if (segment.Length == 0)
                    {
                        continue;
                    }
                    node = FindChildByName(node, segment);
                    if (node == null)
                    {
                        break;
                    }
                }
                if (node != null)
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>Normalizes a WZ-style path into the catalog index key format (strips <c>.img</c> and unifies slashes).</summary>
        private static string NormalizeWzPath(string wzPath)
        {
            if (string.IsNullOrEmpty(wzPath))
            {
                return null;
            }
            string[] segments = wzPath.Replace('\\', '/').Split('/');
            List<string> output = new List<string>(segments.Length);
            foreach (string segment in segments)
            {
                string name = segment;
                if (name.Length == 0)
                {
                    continue;
                }
                if (name.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - 4);
                }
                output.Add(name);
            }
            return output.Count == 0 ? null : string.Join("/", output);
        }

        private static WzLib.Wz_Node FindChildByName(WzLib.Wz_Node parent, string name)
        {
            WzLib.Wz_Node found = parent.FindNodeByPath(name);
            if (found != null)
            {
                return found;
            }

            // Id nodes may differ in the number of zero-padded digits (02000000 / 2000000).
            long number;
            bool nameIsNumber = long.TryParse(name, out number);
            foreach (WzLib.Wz_Node child in parent.Nodes)
            {
                if (string.Equals(child.Text, name, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
                long childNumber;
                if (nameIsNumber && long.TryParse(child.Text, out childNumber) && childNumber == number)
                {
                    return child;
                }
            }
            return null;
        }

        private static List<string> DetectLocales(ClassicCatalog catalog)
        {
            SortedSet<string> found = new SortedSet<string>(StringComparer.Ordinal);
            const string prefix = "String/";
            foreach (string key in catalog.Entries.Keys)
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }
                string rest = key.Substring(prefix.Length);
                int slash = rest.IndexOf('/');
                if (slash <= 0 || slash == rest.Length - 1)
                {
                    continue;
                }
                found.Add(rest.Substring(0, slash));
            }

            List<string> ordered = new List<string>();
            foreach (string code in PreferredLocales)
            {
                if (found.Remove(code))
                {
                    ordered.Add(code);
                }
            }
            ordered.AddRange(found);
            if (ordered.Count == 0)
            {
                ordered.Add("TW");
            }
            return ordered;
        }
    }
}

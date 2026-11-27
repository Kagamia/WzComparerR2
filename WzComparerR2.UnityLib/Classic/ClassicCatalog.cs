using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WzComparerR2.UnityLib.Images;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>Identification data for a bundle in the catalog.</summary>
    public sealed class ClassicBundleMeta
    {
        public ClassicBundleMeta(string name, long size, long lastWriteTicks)
        {
            this.Name = name;
            this.Size = size;
            this.LastWriteTicks = lastWriteTicks;
        }

        /// <summary>Bundle file name (without the extension).</summary>
        public string Name { get; private set; }

        public long Size { get; private set; }

        public long LastWriteTicks { get; private set; }
    }

    /// <summary>A child of the index directory.</summary>
    public sealed class ClassicIndexChild
    {
        internal ClassicIndexChild(string name, string path, bool isImage, int imageCount)
        {
            this.Name = name;
            this.Path = path;
            this.IsImage = isImage;
            this.ImageCount = imageCount;
        }

        public string Name { get; private set; }
        public string Path { get; private set; }
        public bool IsImage { get; private set; }
        public int ImageCount { get; private set; }
    }

    /// <summary>
    /// Asset index: bundle → logical path → object location.
    /// <para>
    /// Path convention: the container path
    /// <c>Assets/WzAssets/Json/Character/00002000.wzjson</c> → index key
    /// <c>Character/00002000</c> (prefix and extension stripped). The leading segments of the tree are "directories",
    /// the node matching the index key is the "img", and segments after it are navigated by the WZJS tree
    /// (e.g. <c>Character/00020116/angry/0/face</c>).
    /// </para>
    /// <para>
    /// The index is rebuilt in <see cref="Build"/> (scanning the
    /// AssetBundle container of each bundle, as the reference implementation does); disk caching is not implemented yet.
    /// </para>
    /// </summary>
    public sealed class ClassicCatalog
    {
        public const int CurrentVersion = 7;

        private const string JsonPrefix = "assets/wzassets/json/";
        private const string SoundPrefix = "assets/wzdata/sound/";
        private static readonly string[] SoundSuffixes = new[] { ".mp3", ".wav" };

        private ClassicCatalog(string rootPath)
        {
            this.RootPath = rootPath;
            this.Bundles = new List<ClassicBundleMeta>();
            this.Entries = new SortedDictionary<string, UnityEntryLocation>(StringComparer.Ordinal);
            this.Image = new UnityImageIndex();
            this.spritesetEntryKeys = new HashSet<string>(StringComparer.Ordinal);
            this.atlasEntryKeys = new HashSet<string>(StringComparer.Ordinal);
            this.SpritesetOrigins = new Dictionary<string, UnitySpriteOrigin>(StringComparer.Ordinal);
            this.SoundEntries = new SortedDictionary<string, UnityEntryLocation>(StringComparer.Ordinal);
            this.SoundKeysByImage = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        }

        public string RootPath { get; private set; }

        public List<ClassicBundleMeta> Bundles { get; private set; }

        /// <summary>Logical path (e.g. <c>Character/00002000</c>) → object location.</summary>
        public SortedDictionary<string, UnityEntryLocation> Entries { get; private set; }

        public UnityImageIndex Image { get; private set; }

        /// <summary>
        /// Sound path (e.g. <c>Sound/Bgm00/FloralLife</c>) → AudioClip location.
        /// These entries are not added to <see cref="Entries"/>: WZ sounds are nodes under an img, so
        /// the img content is built from <see cref="SoundKeysByImage"/>.
        /// </summary>
        public SortedDictionary<string, UnityEntryLocation> SoundEntries { get; private set; }

        /// <summary>Sound img (e.g. <c>Sound/Bgm00</c>) → the sound paths beneath it.</summary>
        public SortedDictionary<string, List<string>> SoundKeysByImage { get; private set; }

        /// <summary>Index keys added by <see cref="RebuildSpritesetEntries"/> (all removable at once when the locale changes).</summary>
        private readonly HashSet<string> spritesetEntryKeys;

        /// <summary>Index keys added by <see cref="AddAtlasEntries"/> (locale-independent, so they are rebuilt only once).</summary>
        private readonly HashSet<string> atlasEntryKeys;

        /// <summary>
        /// spriteset sprite logical path → WZ <c>origin</c>.
        /// Computed from the sprite's m_Pivot and used to align animation anchor points.
        /// </summary>
        public Dictionary<string, UnitySpriteOrigin> SpritesetOrigins { get; private set; }

        /// <summary>Scans every .bundle under the directory and indexes them using each AssetBundle object's m_Container.</summary>
        /// <param name="rootDirectory">
        /// Directory containing the bundles. The bundle directory itself, StreamingAssets/aa, or the client folder
        /// are all resolved automatically.
        /// </param>
        public static ClassicCatalog Build(string rootDirectory)
        {
            if (rootDirectory == null)
            {
                throw new ArgumentNullException(nameof(rootDirectory));
            }
            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException("The dataset directory does not exist: " + rootDirectory);
            }

            string bundleDirectory = ResolveBundleDirectory(rootDirectory);

            List<string> files = new List<string>();
            foreach (string path in Directory.GetFiles(bundleDirectory))
            {
                if (string.Equals(Path.GetExtension(path), ".bundle", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(path);
                }
            }
            files.Sort(StringComparer.Ordinal);

            ClassicCatalog catalog = new ClassicCatalog(bundleDirectory);
            foreach (string path in files)
            {
                FileInfo info = new FileInfo(path);
                catalog.Bundles.Add(new ClassicBundleMeta(
                    Path.GetFileNameWithoutExtension(path), info.Length, info.LastWriteTimeUtc.Ticks));
            }

            // 1st pass: cross-bundle pathID → location index. spriteset .asset files reference Sprites / Texture2Ds
            // in other bundles (such as atlas_*), so this is needed to resolve external references.
            Dictionary<long, UnityEntryLocation> objectLocations =
                new Dictionary<long, UnityEntryLocation>(65536);
            for (int bundleIndex = 0; bundleIndex < files.Count; bundleIndex++)
            {
                using (UnityFsBundle bundle = UnityFsBundle.Open(files[bundleIndex]))
                {
                    catalog.CollectObjectLocations(bundle, bundleIndex, objectLocations);
                }
            }

            for (int bundleIndex = 0; bundleIndex < files.Count; bundleIndex++)
            {
                using (UnityFsBundle bundle = UnityFsBundle.Open(files[bundleIndex]))
                {
                    catalog.IndexBundle(
                        bundle, bundleIndex, catalog.Bundles[bundleIndex].Name, objectLocations);
                }
            }
            catalog.GroupSoundEntries();
            return catalog;
        }

        /// <summary>
        /// Walks the SerializedFile of every bundle and collects pathID → <see cref="UnityEntryLocation"/>
        /// (the first occurrence wins).
        /// </summary>
        private void CollectObjectLocations(
            UnityFsBundle bundle, int bundleIndex, Dictionary<long, UnityEntryLocation> objectLocations)
        {
            for (int nodeIndex = 0; nodeIndex < bundle.Nodes.Count; nodeIndex++)
            {
                if (!bundle.Nodes[nodeIndex].IsSerializedFile)
                {
                    continue;
                }
                UnitySerializedFile serializedFile;
                try
                {
                    int metadataLength = UnitySerializedFile.GetMetadataLength(
                        bundle.ReadNodeRange(nodeIndex, 0, UnitySerializedFile.HeaderSize));
                    serializedFile = UnitySerializedFile.Parse(
                        bundle.ReadNodeRange(nodeIndex, 0, metadataLength));
                }
                catch (Exception ex) when (ex is UnityException || ex is ArgumentException || ex is OverflowException)
                {
                    continue;
                }

                foreach (UnityObjectInfo obj in serializedFile.Objects)
                {
                    if (obj.PathId == 0 || objectLocations.ContainsKey(obj.PathId))
                    {
                        continue;
                    }
                    long absolute = serializedFile.DataOffset + obj.ByteStart;
                    if (absolute < serializedFile.DataOffset)
                    {
                        continue;
                    }
                    objectLocations[obj.PathId] = new UnityEntryLocation(
                        bundleIndex, nodeIndex, absolute, obj.ByteSize, obj.ClassId);
                }
            }
        }

        /// <summary>Reads an object's bytes from any <see cref="UnityEntryLocation"/> (reopening the bundle each time).</summary>
        internal byte[] ReadEntryBytes(UnityEntryLocation location)
        {
            string bundlePath = Path.Combine(this.RootPath, this.Bundles[location.BundleIndex].Name + ".bundle");
            using (UnityFsBundle bundle = UnityFsBundle.Open(bundlePath))
            {
                return bundle.ReadNodeRange(
                    location.NodeIndex, location.AbsoluteOffset, (int)location.ByteSize);
            }
        }

        /// <summary>
        /// Locates the directory that actually contains the .bundle files.
        /// Addressables 1.x keeps local content in <c>&lt;aa&gt;/StandaloneWindows64</c>,
        /// while the 2.x remote catalog layout uses <c>&lt;aa&gt;/w</c>, so
        /// the client root, <see cref="System.IO.Path"/>'s <c>StreamingAssets/aa</c>,
        /// or any directory beneath them can all be opened.
        /// </summary>
        public static string ResolveBundleDirectory(string rootDirectory)
        {
            if (rootDirectory == null)
            {
                throw new ArgumentNullException(nameof(rootDirectory));
            }
            if (HasBundles(rootDirectory))
            {
                return rootDirectory;
            }

            List<string> candidates = new List<string>();
            // Accept the client root, StreamingAssets, or aa.
            List<string> aaDirectories = new List<string>();
            aaDirectories.Add(rootDirectory);
            aaDirectories.Add(Path.Combine(rootDirectory, "StreamingAssets", "aa"));
            aaDirectories.Add(Path.Combine(rootDirectory, "aa"));
            aaDirectories.Add(Path.Combine(rootDirectory, "StreamingAssets"));

            foreach (string aaDirectory in aaDirectories)
            {
                // The two Addressables layouts (remote catalog w / local StandaloneWindows64).
                candidates.Add(Path.Combine(aaDirectory, "w"));
                string buildTarget = ReadBuildTarget(Path.Combine(aaDirectory, "settings.json"));
                if (buildTarget != null)
                {
                    candidates.Add(Path.Combine(aaDirectory, buildTarget));
                }
                candidates.Add(Path.Combine(aaDirectory, "StandaloneWindows64"));

                // Also check the subdirectories directly under aa.
                candidates.AddRange(SafeGetDirectories(aaDirectory));
                candidates.Add(aaDirectory);
            }

            // Also check the subdirectories directly under the client root.
            candidates.AddRange(SafeGetDirectories(rootDirectory));

            foreach (string candidate in candidates)
            {
                if (Directory.Exists(candidate) && HasBundles(candidate))
                {
                    return candidate;
                }
            }

            throw new ClassicAssetException(
                "No AssetBundle (.bundle) was found under: " + rootDirectory + "\r\n"
                + "Select the StreamingAssets/aa folder of the client, or a subfolder that contains the bundles.");
        }

        private static bool HasBundles(string directory)
        {
            return Directory.Exists(directory)
                && Directory.GetFiles(directory, "*.bundle").Length > 0;
        }

        private static IEnumerable<string> SafeGetDirectories(string directory)
        {
            try
            {
                return Directory.Exists(directory)
                    ? Directory.GetDirectories(directory)
                    : Enumerable.Empty<string>();
            }
            catch (IOException)
            {
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>Reads the build target name (e.g. <c>StandaloneWindows64</c>) from aa/settings.json.</summary>
        private static string ReadBuildTarget(string settingsPath)
        {
            try
            {
                if (!File.Exists(settingsPath))
                {
                    return null;
                }
                string text = File.ReadAllText(settingsPath);
                Match match = Regex.Match(text, "\"m_BuildTarget\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.CultureInvariant);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        /// <summary>
        /// Organizes sound entries into the WZ img layout: <c>Sound/&lt;img&gt;/&lt;subdirectory…&gt;/&lt;name&gt;</c>,
        /// stores them in <see cref="SoundEntries"/>, and leaves only the img itself in <see cref="Entries"/>
        /// (collapsed to <c>Sound/&lt;img&gt;</c>) so the tree shows "Sound &gt; Bgm00 &gt; FloralLife" instead of every sound at the root.
        /// </summary>
        private void GroupSoundEntries()
        {
            List<KeyValuePair<string, UnityEntryLocation>> sounds = new List<KeyValuePair<string, UnityEntryLocation>>();
            foreach (KeyValuePair<string, UnityEntryLocation> entry in this.Entries)
            {
                if (entry.Value.ClassId == UnityClassIds.AudioClip)
                {
                    sounds.Add(entry);
                }
            }

            foreach (KeyValuePair<string, UnityEntryLocation> entry in sounds)
            {
                int firstSlash = entry.Key.IndexOf('/');
                int secondSlash = firstSlash < 0 ? -1 : entry.Key.IndexOf('/', firstSlash + 1);
                if (secondSlash <= firstSlash + 1 || secondSlash == entry.Key.Length - 1)
                {
                    continue; // Shape does not match (e.g. Sound/<name>): keep as is
                }

                string imageKey = entry.Key.Substring(0, secondSlash);
                List<string> keys;
                if (!this.SoundKeysByImage.TryGetValue(imageKey, out keys))
                {
                    keys = new List<string>();
                    this.SoundKeysByImage[imageKey] = keys;
                }
                keys.Add(entry.Key);
                this.SoundEntries[entry.Key] = entry.Value;
                this.Entries.Remove(entry.Key);
            }

            foreach (KeyValuePair<string, List<string>> group in this.SoundKeysByImage)
            {
                group.Value.Sort(StringComparer.Ordinal);
                if (!this.Entries.ContainsKey(group.Key))
                {
                    this.Entries[group.Key] = this.SoundEntries[group.Value[0]];
                }
            }
        }

        /// <summary>Whether the index key is a sound img (e.g. <c>Sound/Bgm00</c>).</summary>
        public bool IsSoundImage(string key)
        {
            return key != null && this.SoundKeysByImage.ContainsKey(key);
        }

        /// <summary>Sound paths beneath a sound img (e.g. <c>Sound/Bgm00/FloralLife</c>), sorted.</summary>
        public IReadOnlyList<string> GetSoundKeys(string imageKey)
        {
            List<string> keys;
            if (imageKey != null && this.SoundKeysByImage.TryGetValue(imageKey, out keys))
            {
                return keys;
            }
            return new string[0];
        }

        /// <summary>
        /// Rebuilds the spriteset index, including only paths provided by <paramref name="locale"/> and <c>Common</c>.
        /// <para>
        /// spritesets (<c>SpriteSet/&lt;region&gt;/UI/…</c>) are not WZJS/JSON assets, so they do not normally appear in the index,
        /// and directories provided only by spritesets, such as <c>UI/UIWindow</c> and <c>UI/Login</c>, vanished from the tree entirely.
        /// As in classic WZ, only nodes that actually exist in the current region are indexed here: assets that exist only in other regions
        /// (e.g. <c>UI/Login/GameLogo</c>, provided only for TW/KR) do not appear in the tree.
        /// </para>
        /// <para>At render time, <see cref="UnityCanvas"/> resolves the value according to the locale.</para>
        /// </summary>
        public void RebuildSpritesetEntries(string locale)
        {
            foreach (string key in this.spritesetEntryKeys)
            {
                this.Entries.Remove(key);
                this.SpritesetOrigins.Remove(key);
            }
            this.spritesetEntryKeys.Clear();

            if (!string.IsNullOrEmpty(locale))
            {
                this.AddSpritesetEntries(locale);
            }
            this.AddSpritesetEntries("Common");
            this.AddAtlasEntries();
        }

        /// <summary>
        /// Indexes SpriteAtlases (<c>Assets/WzAssets/Atlas/&lt;name&gt;.spriteatlasv2</c>) as <c>UI/&lt;atlas name&gt;.img</c>.
        /// Sprites missing from the current locale's spriteset can also be reached here (atlases are locale-fixed, so they are rebuilt only once).
        /// </summary>
        private void AddAtlasEntries()
        {
            foreach (KeyValuePair<string, Dictionary<string, UnitySetSpriteEntry>> asset in this.Image.AtlasAssets)
            {
                foreach (KeyValuePair<string, UnitySetSpriteEntry> sprite in asset.Value)
                {
                    string path = asset.Key + "/" + sprite.Key;
                    if (this.Entries.ContainsKey(path) && !this.atlasEntryKeys.Contains(path))
                    {
                        continue; // Prefer the definition from WZJS / JSON / spriteset
                    }
                    this.Entries[path] = sprite.Value.SpriteLocation;
                    this.SpritesetOrigins[path] = sprite.Value.Origin;
                    this.atlasEntryKeys.Add(path);
                }
            }
        }

        private void AddSpritesetEntries(string region)
        {
            foreach (KeyValuePair<string, Dictionary<string, UnitySetSpriteEntry>> asset in this.Image.SetAssets)
            {
                int slash = asset.Key.IndexOf('/');
                if (slash <= 0 || slash == asset.Key.Length - 1)
                {
                    continue;
                }
                if (!string.Equals(asset.Key.Substring(0, slash), region, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string logicalPath = asset.Key.Substring(slash + 1);
                foreach (KeyValuePair<string, UnitySetSpriteEntry> sprite in asset.Value)
                {
                    string path = string.IsNullOrEmpty(sprite.Key)
                        ? logicalPath
                        : logicalPath + "/" + sprite.Key;
                    if (this.Entries.ContainsKey(path) && !this.spritesetEntryKeys.Contains(path))
                    {
                        continue; // Prefer the WZJS / JSON asset
                    }
                    this.Entries[path] = sprite.Value.SpriteLocation;
                    this.SpritesetOrigins[path] = sprite.Value.Origin;
                    this.spritesetEntryKeys.Add(path);
                }
            }
        }

        /// <summary>The children of the top level (or of a given directory node).</summary>
        public List<ClassicIndexChild> ListIndexChildren(string prefix)
        {
            SortedDictionary<string, ClassicIndexChild> output =
                new SortedDictionary<string, ClassicIndexChild>(StringComparer.Ordinal);
            string pfx = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + "/";
            foreach (string key in this.Entries.Keys)
            {
                if (!key.StartsWith(pfx, StringComparison.Ordinal))
                {
                    continue;
                }
                string rest = key.Substring(pfx.Length);
                int slash = rest.IndexOf('/');
                string segment = slash < 0 ? rest : rest.Substring(0, slash);
                string childPath = pfx + segment;

                ClassicIndexChild child;
                if (!output.TryGetValue(childPath, out child))
                {
                    child = new ClassicIndexChild(segment, childPath, false, 0);
                    output[childPath] = child;
                }
                bool isImage = slash < 0;
                output[childPath] = new ClassicIndexChild(
                    segment, childPath, child.IsImage || isImage, child.ImageCount + 1);
            }
            List<ClassicIndexChild> result = new List<ClassicIndexChild>(output.Values);
            result.Sort((a, b) => CompareNodeNames(a.Name, b.Name));
            return result;
        }

        /// <summary>Splits path into (img key, path inside the img); returns false when no img prefix is found.</summary>
        public bool TrySplitImagePrefix(string path, out string imageKey, out string inner)
        {
            imageKey = null;
            inner = null;
            if (path == null)
            {
                return false;
            }
            string current = path;
            while (true)
            {
                if (this.Entries.ContainsKey(current))
                {
                    imageKey = current;
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

        /// <summary>
        /// Lists candidate paths under the <c>Item</c> directory for an item id, in priority order. The end of a path is
        /// the img, and later segments are nodes inside the img (matching item_path_candidates in the reference implementation).
        /// </summary>
        public List<string> GetItemPathCandidates(int itemId)
        {
            List<string> candidates = new List<string>();
            if (itemId < 0)
            {
                return candidates;
            }
            string id = itemId.ToString("D8", CultureInfo.InvariantCulture);
            string group = (itemId / 10000).ToString("D4", CultureInfo.InvariantCulture);
            switch (itemId / 1000000)
            {
                case 2:
                    candidates.Add("Item/Consume/" + group + "/" + id);
                    break;
                case 3:
                    candidates.Add("Item/Install/" + group + "/" + id);
                    break;
                case 4:
                    candidates.Add("Item/Etc/" + group + "/" + id);
                    break;
                case 5:
                    // Pets have one img per id; other items starting with 5 go into the Cash category img.
                    string pet = "Item/Pet/" + itemId.ToString(CultureInfo.InvariantCulture);
                    if (this.Entries.ContainsKey(pet))
                    {
                        candidates.Add(pet);
                    }
                    candidates.Add("Item/Cash/" + group + "/" + id);
                    break;
                case 9:
                    // Set items, inventory expansions, etc.: nodes inside the Special img use 7-digit ids.
                    candidates.Add("Item/Special/0"
                        + (itemId / 10000).ToString(CultureInfo.InvariantCulture)
                        + "/" + itemId.ToString("D7", CultureInfo.InvariantCulture));
                    break;
            }
            return candidates;
        }

        /// <summary>
        /// Lists candidate paths under the <c>Character</c> directory for an equipment id, in priority order.
        /// </summary>
        public List<string> GetEquipPathCandidates(int itemId)
        {
            List<string> candidates = new List<string>();
            if (itemId / 1000000 != 1)
            {
                return candidates;
            }
            string directory = GetEquipDirectory(itemId / 10000);
            if (directory != null)
            {
                candidates.Add("Character/" + directory + "/"
                    + itemId.ToString("D8", CultureInfo.InvariantCulture));
            }
            return candidates;
        }

        /// <summary>
        /// Lists candidate paths under the <c>Mob</c> directory for a mob id, in priority order
        /// (covers classic WZ <c>Mob/&lt;id&gt;.img</c> and the special categories).
        /// </summary>
        public List<string> GetMobPathCandidates(int mobId)
        {
            List<string> candidates = new List<string>();
            if (mobId <= 0)
            {
                return candidates;
            }
            string id = mobId.ToString("D7", CultureInfo.InvariantCulture);
            candidates.Add("Mob/" + id);
            foreach (string group in new[]
            {
                "AbyssExpeditionMob", "MExplorerMob", "QuestCountGroup", "RoguelikeMob",
            })
            {
                candidates.Add("Mob/" + group + "/" + id);
            }
            candidates.Add("Mob/RoguelikeMob/Redmoon/" + id);
            return candidates;
        }

        /// <summary>Derives the path under the <c>Npc</c> directory from an NPC id.</summary>
        public List<string> GetNpcPathCandidates(int npcId)
        {
            List<string> candidates = new List<string>();
            if (npcId <= 0)
            {
                return candidates;
            }
            candidates.Add("Npc/" + npcId.ToString("D7", CultureInfo.InvariantCulture));
            return candidates;
        }

        /// <summary>
        /// Derives the path under the <c>Skill</c> directory from a skill id:
        /// the img is the job (<c>Skill/&lt;id/10000&gt;</c>) and the skill itself is
        /// <c>skill/&lt;id&gt;</c> inside the img.
        /// </summary>
        public List<string> GetSkillPathCandidates(int skillId)
        {
            List<string> candidates = new List<string>();
            if (skillId <= 0)
            {
                return candidates;
            }
            candidates.Add("Skill/" + (skillId / 10000).ToString(CultureInfo.InvariantCulture)
                + "/skill/" + skillId.ToString(CultureInfo.InvariantCulture));
            return candidates;
        }

        /// <summary>
        /// Derives the path under the <c>Map</c> directory from a map id:
        /// <c>Map/Map/Map&lt;first digit&gt;/&lt;9-digit id&gt;</c> (corresponds to WZ <c>Map/Map/MapN/&lt;id&gt;.img</c>).
        /// </summary>
        public List<string> GetMapPathCandidates(int mapId)
        {
            List<string> candidates = new List<string>();
            if (mapId <= 0)
            {
                return candidates;
            }
            string id = mapId.ToString("D9", CultureInfo.InvariantCulture);
            candidates.Add("Map/Map/Map" + id[0] + "/" + id);
            return candidates;
        }

        /// <summary>
        /// Derives the path under the <c>Quest</c> directory from a quest id:
        /// <c>Quest/QuestInfo/&lt;id&gt;</c> (corresponds to WZ <c>Quest/QuestInfo.img/&lt;id&gt;</c>).
        /// </summary>
        public List<string> GetQuestPathCandidates(int questId)
        {
            List<string> candidates = new List<string>();
            if (questId <= 0)
            {
                return candidates;
            }
            candidates.Add("Quest/QuestInfo/" + questId.ToString(CultureInfo.InvariantCulture));
            return candidates;
        }

        /// <summary>
        /// Finds the img index key whose node name has a numeric value equal to <paramref name="id"/> within the given directory
        /// (including one level of subdirectories), ignoring zero padding. Returns null if not found. Used when the WZ category directories and the asset tree directories do not match.
        /// </summary>
        public string FindImageKeyByNumericName(string directory, int id)
        {
            if (string.IsNullOrEmpty(directory) || id < 0)
            {
                return null;
            }
            string prefix = directory + "/";
            foreach (string key in this.Entries.Keys)
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }
                string rest = key.Substring(prefix.Length);
                int slash = rest.LastIndexOf('/');
                if (slash >= 0 && rest.IndexOf('/') != slash)
                {
                    continue; // Do not treat three or more levels as a category img
                }
                string leaf = slash < 0 ? rest : rest.Substring(slash + 1);
                long value;
                if (TryParseNumericName(leaf, out value) && value == id)
                {
                    return key;
                }
            }
            return null;
        }

        private static string GetEquipDirectory(int group)
        {
            switch (group)
            {
                case 100:
                    return "Cap";
                case 101:
                case 102:
                case 103:
                case 112:
                case 113:
                case 114:
                case 115:
                    return "Accessory";
                case 104:
                    return "Coat";
                case 105:
                    return "Longcoat";
                case 106:
                    return "Pants";
                case 107:
                    return "Shoes";
                case 108:
                    return "Glove";
                case 109:
                    return "Shield";
                case 110:
                    return "Cape";
                case 111:
                    return "Ring";
                case 180:
                    return "PetEquip";
                default:
                    return group >= 121 && group <= 179 ? "Weapon" : null;
            }
        }

        /// <summary>
        /// Node name ordering: purely numeric names come first by numeric value, then the rest alphabetically, case-insensitively.
        /// </summary>
        public static int CompareNodeNames(string left, string right)
        {
            long leftNumber;
            long rightNumber;
            bool leftIsNumber = TryParseNumericName(left, out leftNumber);
            bool rightIsNumber = TryParseNumericName(right, out rightNumber);
            if (leftIsNumber && rightIsNumber)
            {
                int compare = leftNumber.CompareTo(rightNumber);
                return compare != 0 ? compare : string.CompareOrdinal(left, right);
            }
            if (leftIsNumber)
            {
                return -1;
            }
            if (rightIsNumber)
            {
                return 1;
            }
            int insensitive = string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
            return insensitive != 0 ? insensitive : string.CompareOrdinal(left, right);
        }

        /// <summary>Normalizes a container path into an index key; returns null for assets that are not json / sound.</summary>
        public static string NormalizeContainerPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            string lower = path.ToLowerInvariant();
            if (lower.StartsWith(JsonPrefix, StringComparison.Ordinal))
            {
                string stripped = path.Substring(JsonPrefix.Length);
                if (lower.EndsWith(".wzjson", StringComparison.Ordinal))
                {
                    return stripped.Substring(0, stripped.Length - ".wzjson".Length);
                }
                if (lower.EndsWith(".json", StringComparison.Ordinal))
                {
                    return stripped.Substring(0, stripped.Length - ".json".Length);
                }
                return null;
            }
            if (lower.StartsWith(SoundPrefix, StringComparison.Ordinal))
            {
                foreach (string suffix in SoundSuffixes)
                {
                    if (lower.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        string stripped = path.Substring(SoundPrefix.Length);
                        return "Sound/" + stripped.Substring(0, stripped.Length - suffix.Length);
                    }
                }
            }
            return null;
        }

        private static bool TryParseNumericName(string name, out long value)
        {
            value = 0;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] < '0' || name[i] > '9')
                {
                    return false;
                }
            }
            return long.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        private void IndexBundle(
            UnityFsBundle bundle, int bundleIndex, string bundleStem,
            Dictionary<long, UnityEntryLocation> objectLocations)
        {
            IReadOnlyList<UnityFsBundle.BundleNode> nodes = bundle.Nodes;
            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                if (!nodes[nodeIndex].IsSerializedFile)
                {
                    continue;
                }

                byte[] head;
                int metadataLength;
                try
                {
                    head = bundle.ReadNodeRange(nodeIndex, 0, UnitySerializedFile.HeaderSize);
                    metadataLength = UnitySerializedFile.GetMetadataLength(head);
                }
                catch (Exception ex) when (ex is UnityException || ex is ArgumentException || ex is OverflowException)
                {
                    continue;
                }

                UnitySerializedFile serializedFile;
                try
                {
                    head = bundle.ReadNodeRange(nodeIndex, 0, metadataLength);
                    serializedFile = UnitySerializedFile.Parse(head);
                }
                catch (Exception ex) when (ex is UnityException || ex is ArgumentException || ex is OverflowException)
                {
                    continue;
                }

                Dictionary<long, UnityObjectInfo> objectsByPathId =
                    new Dictionary<long, UnityObjectInfo>(serializedFile.Objects.Count);
                foreach (UnityObjectInfo obj in serializedFile.Objects)
                {
                    objectsByPathId[obj.PathId] = obj;
                }

                foreach (UnityObjectInfo assetBundleObject in serializedFile.ObjectsByClass(UnityClassIds.AssetBundle))
                {
                    long objectOffset = serializedFile.DataOffset + assetBundleObject.ByteStart;
                    if (objectOffset < serializedFile.DataOffset || assetBundleObject.ByteSize > int.MaxValue)
                    {
                        throw new UnityException(string.Format(
                            "Invalid AssetBundle object offset: bundle={0}, node={1}", bundleStem, nodeIndex));
                    }

                    UnityAssetBundleInfo assetBundle;
                    try
                    {
                        byte[] data = bundle.ReadNodeRange(
                            nodeIndex, objectOffset, (int)assetBundleObject.ByteSize);
                        assetBundle = UnityObjectParsers.ParseAssetBundle(data, serializedFile.IsLittleEndian);
                    }
                    catch (UnityException ex)
                    {
                        throw new UnityException(string.Format(
                            "Failed to parse the AssetBundle object: bundle={0}, node={1}: {2}",
                            bundleStem, nodeIndex, ex.Message), ex);
                    }

                    this.Image.CollectFromContainer(
                        bundle, bundleIndex, bundleStem, nodeIndex,
                        serializedFile, assetBundle.Container, objectsByPathId,
                        objectLocations, this.ReadEntryBytes);

                    foreach (UnityContainerEntry containerEntry in assetBundle.Container)
                    {
                        string key = NormalizeContainerPath(containerEntry.Path);
                        if (key == null)
                        {
                            continue;
                        }
                        if (containerEntry.Asset.FileId != 0)
                        {
                            continue; // External reference is not in this bundle: skip
                        }
                        UnityObjectInfo obj;
                        if (!objectsByPathId.TryGetValue(containerEntry.Asset.PathId, out obj))
                        {
                            continue;
                        }
                        long absolute = serializedFile.DataOffset + obj.ByteStart;
                        if (absolute < serializedFile.DataOffset)
                        {
                            continue;
                        }
                        this.Entries[key] = new UnityEntryLocation(
                            bundleIndex, nodeIndex, absolute, obj.ByteSize, obj.ClassId);
                    }
                }
            }
        }
    }
}


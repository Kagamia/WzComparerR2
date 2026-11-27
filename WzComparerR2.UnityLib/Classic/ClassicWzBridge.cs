using System;
using System.Collections.Generic;
using System.IO;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.UnityLib.Wzjs;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Base class for img of a non-WZ file source (Unity asset): it inherits <see cref="Wz_Image"/> so that existing code
    /// that only handles <c>Wz_Image</c> (such as MapRender) can use it as-is.
    /// <para>
    /// <see cref="Wz_Image.TryExtract(out Exception)"/> converts the asset content into WZ nodes on the spot
    /// (<see cref="Wz_Image.Node"/>). Node display names follow the conventional <c>&lt;name&gt;.img</c> form.
    /// </para>
    /// </summary>
    public abstract class UnityAssetImage : Wz_Image
    {
        private readonly object extractLock = new object();
        private Wz_Node rootedNode;
        private readonly object rootedLock = new object();
        private bool extracted;

        protected UnityAssetImage(ClassicWzSource source, string imageKey)
            : base(imageKey, GetTrailingName(imageKey) + ".img")
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (imageKey == null)
            {
                throw new ArgumentNullException(nameof(imageKey));
            }
            this.Source = source;
        }

        /// <summary>Logical path of the img (e.g. <c>Character/00002000</c>, <c>Sound/Bgm00</c>).</summary>
        public string ImageKey
        {
            get { return this.Name; }
        }

        /// <summary>The owning dataset.</summary>
        public ClassicWzSource Source { get; private set; }

        /// <summary>Populates <see cref="Wz_Image.Node"/> with the asset content (called only once).</summary>
        protected abstract void PopulateContent();

        /// <summary>Whether the content is already extracted (used by comparison tools to decide whether to release it themselves).</summary>
        public bool IsExtracted
        {
            get { return this.extracted; }
        }

        /// <summary>Extracts the asset content as WZ nodes (may be called multiple times).</summary>
        public override bool TryExtract(out Exception e)
        {
            try
            {
                this.EnsureExtracted();
                // As with WZ imgs, allow the content node to be reached from the directory and img nodes.
                // Otherwise FullPathToFile produces paths such as "WorldMap.img\BaseImg\0" that omit the
                // directory name, and resource lookups via PluginManager.FindWz cannot resolve them.
                this.GetRootedNode();
                e = null;
                return true;
            }
            catch (Exception ex)
            {
                e = ex;
                return false;
            }
        }

        private void EnsureExtracted()
        {
            if (this.extracted)
            {
                return;
            }
            lock (this.extractLock)
            {
                if (this.extracted)
                {
                    return;
                }
                this.PopulateContent();
                this.extracted = true;
            }
        }

        /// <summary>Gets the content as a WZ node tree (converted and cached on the first call).</summary>
        public Wz_Node AsWzNode()
        {
            this.EnsureExtracted();
            return this.Node;
        }

        /// <summary>
        /// Releases the extracted content (comparison tools call this after each comparison). The next call to <see cref="TryExtract(out Exception)"/>
        /// converts from the asset again. The asset itself holds the data, so it can be extracted repeatedly.
        /// </summary>
        public override void Unextract()
        {
            lock (this.extractLock)
            {
                if (!this.extracted)
                {
                    return;
                }
                this.extracted = false;
                this.Node.Nodes.Clear();
            }
        }

        /// <summary>
        /// Content root node. Adds the chain of parent nodes named after the directories when needed
        /// (e.g. <c>Character\Weapon\01442000\...</c>) so that
        /// path checks such as <see cref="Wz_Node.FullPathToFile"/> keep using the WZ logic as-is.
        /// </summary>
        public Wz_Node GetRootedNode()
        {
            Wz_Node content = this.AsWzNode();
            if (content.ParentNode != null)
            {
                return content;
            }
            lock (rootedLock)
            {
                if (this.rootedNode == null)
                {
                    this.rootedNode = CreateRootedTree(this.Node, this.Name);
                    if (this.rootedNode != null)
                    {
                        this.OwnerNode = this.Node.ParentNode;
                    }
                }
            }
            return content;
        }

        /// <summary>
        /// Creates the same parent chain as classic WZ on the content node: a directory node plus a <c>&lt;img&gt;.img</c> node with the same name
        /// (the content node is attached beneath them). This makes <see cref="Wz_Node.FullPathToFile"/> produce
        /// paths such as <c>Sound\Bgm00.img\FloralLife</c>.
        /// </summary>
        private static Wz_Node CreateRootedTree(Wz_Node content, string imageKey)
        {
            string[] segments = imageKey.Split('/');
            if (segments.Length < 2)
            {
                return null;
            }

            Wz_Node root = new Wz_Node(segments[0]);
            Wz_Node parent = root;
            for (int i = 1; i < segments.Length - 1; i++)
            {
                Wz_Node folder = new Wz_Node(segments[i]);
                parent.Nodes.Add(folder);
                parent = folder;
            }

            Wz_Node imageNode = new Wz_Node(content.Text);
            imageNode.Value = content.GetValue<Wz_Image>();
            parent.Nodes.Add(imageNode);
            imageNode.Nodes.Add(content);
            return root;
        }

        private static string GetTrailingName(string name)
        {
            int slash = name.LastIndexOf('/');
            return slash < 0 ? name : name.Substring(slash + 1);
        }
    }

    /// <summary>
    /// Represents a single WZJS img in the classic WZ <see cref="Wz_Node"/> form.
    /// <para>
    /// Canvas values are <see cref="UnityCanvas"/>, which is also a <see cref="Wz_Png"/>, and
    /// the image is decoded when it is first needed.
    /// </para>
    /// </summary>
    public class UnityWzImage : UnityAssetImage
    {
        public UnityWzImage(ClassicWzSource source, string name)
            : base(source, name)
        {
        }

        public virtual WzjsNode Root
        {
            get { return this.Source.GetTree(this.Name); }
        }

        /// <summary>Gets the PNG bytes of a node inside the img; returns null when there is no image.</summary>
        public byte[] GetPng(string innerPath)
        {
            string path = string.IsNullOrEmpty(innerPath) ? this.Name : this.Name + "/" + innerPath;
            return this.Source.GetImage(path);
        }

        /// <summary>Finds a node in the same dataset by its WZ-style path (the <c>.img</c> extension is optional).</summary>
        public Wz_Node FindNode(string wzPath)
        {
            return this.Source.FindNode(wzPath);
        }

        protected override void PopulateContent()
        {
            WzjsNode tree = this.Source.GetTree(this.Name);
            foreach (WzjsNode child in tree.Children)
            {
                this.Node.Nodes.Add(ClassicWzBridge.ConvertNode(
                    child, this.Name, this.Source));
            }
        }
    }

    /// <summary>
    /// Builds a directory that WZ provides as a single img (e.g. <c>Map.wz/MapHelper.img</c>)
    /// as one img from the collection of individual sprites in the Unity dataset.
    /// </summary>
    public sealed class UnityDirectoryImage : UnityWzImage
    {
        public UnityDirectoryImage(ClassicWzSource source, string directoryPath)
            : base(source, directoryPath)
        {
        }

        /// <summary><see cref="UnityWzImage.Root"/> is unusable because there is no underlying WZJS.</summary>
        public override WzjsNode Root
        {
            get { return null; }
        }

        protected override void PopulateContent()
        {
            ClassicWzBridge.BuildDirectoryNodes(this.Source, this.Node, this.Name);
        }
    }

    /// <summary>
    /// Bridge from the Unity asset tree to WzComparerR2 <see cref="Wz_Node"/> trees.
    /// </summary>
    public static class ClassicWzBridge
    {
        /// <summary>Converts a WZJS node (including its subtree) into a WzLib node.</summary>
        public static Wz_Node ConvertNode(WzjsNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            Wz_Node root = new Wz_Node(node.Name);
            root.Value = ConvertValue(node.Value);
            foreach (WzjsNode child in node.Children)
            {
                root.Nodes.Add(ConvertNode(child));
            }
            return root;
        }

        /// <summary>Creates the WZ node for a single img (with the decoded subtree and Canvas PNGs).</summary>
        public static Wz_Node CreateImageNode(UnityWzImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }
            return CreateImageNode(image, image.Name);
        }

        /// <summary>
        /// Builds the whole WZ-like directory tree from the catalog: intermediate nodes are directories and leaf nodes are imgs
        /// (whose Value is a <see cref="UnityWzImage"/>), JSON (<see cref="UnityJsonAsset"/>),
        /// or AudioClip information.
        /// </summary>
        public static Wz_Node CreateCatalogNode(ClassicWzSource source, bool includeTrees, bool includeImages)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            string rootName = source.ClientInfo?.DisplayName;
            if (string.IsNullOrEmpty(rootName))
            {
                rootName = Path.GetFileName(
                    source.Catalog.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            if (string.IsNullOrEmpty(rootName))
            {
                rootName = "MapleStory Classic";
            }

            Wz_Node root = new Wz_Node(rootName);
            BuildEntryNodes(source, root, string.Empty, includeTrees, includeImages);
            CollapseWzImagedDirectories(root, source);
            return root;
        }

        /// <summary>
        /// Directories that WZ provides as a single img (e.g. <c>Map.wz/MapHelper.img</c>).
        /// The Unity dataset indexes them as directories of individual sprites, so they are collapsed into a single img.
        /// </summary>
        internal static readonly string[] WzImagedDirectories = { "Map/MapHelper" };

        /// <summary>
        /// Directories whose immediate subdirectories are each treated as an img (e.g. <c>UI.wz/UIWindow.img</c>).
        /// </summary>
        internal static readonly string[] WzImagedDirectoryParents = { "UI" };

        /// <summary>Whether <paramref name="key"/> is a directory that is collapsed into an img.</summary>
        public static bool IsWzImagedDirectory(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            foreach (string directory in WzImagedDirectories)
            {
                if (string.Equals(key, directory, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            foreach (string parent in WzImagedDirectoryParents)
            {
                // Only the level directly under the parent directory is treated as an img (e.g. UI/UIWindow).
                if (key.Length > parent.Length + 1
                    && key.StartsWith(parent + "/", StringComparison.Ordinal)
                    && key.IndexOf('/', parent.Length + 1) < 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Builds the catalog entries under <paramref name="directoryPath"/> as WZ-style nodes
        /// beneath <paramref name="root"/> (the content of <see cref="UnityDirectoryImage"/>).
        /// </summary>
        internal static void BuildDirectoryNodes(ClassicWzSource source, Wz_Node root, string directoryPath)
        {
            BuildEntryNodes(source, root, directoryPath + "/", false, false);
        }

        private static void BuildEntryNodes(
            ClassicWzSource source, Wz_Node root, string prefix, bool includeTrees, bool includeImages)
        {
            Dictionary<string, Wz_Node> nodes = new Dictionary<string, Wz_Node>(StringComparer.Ordinal);
            nodes[string.Empty] = root;

            foreach (KeyValuePair<string, UnityEntryLocation> entry in source.Catalog.Entries)
            {
                if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }
                string[] segments = entry.Key.Substring(prefix.Length).Split('/');
                string currentPath = string.Empty;
                Wz_Node current = root;
                for (int i = 0; i < segments.Length; i++)
                {
                    currentPath = currentPath.Length == 0 ? segments[i] : currentPath + "/" + segments[i];
                    Wz_Node child;
                    if (!nodes.TryGetValue(currentPath, out child))
                    {
                        child = current.Nodes.Add(segments[i]);
                        nodes[currentPath] = child;
                    }
                    current = child;
                }

                switch (entry.Value.ClassId)
                {
                    case UnityClassIds.TextAsset:
                        current.Value = new UnityJsonAsset(source, entry.Key);
                        if (includeTrees)
                        {
                            try
                            {
                                UnityJson.Populate(current, source.ReadTextAsset(entry.Key).Script);
                            }
                            catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException)
                            {
                                current.Nodes.Clear();
                            }
                        }
                        break;

                    case UnityClassIds.AudioClip:
                        if (source.Catalog.IsSoundImage(entry.Key))
                        {
                            // WZ sounds are nodes under an img (Sound/Bgm00.img), so represent them the same way.
                            current.Value = source.GetSoundImage(entry.Key);
                        }
                        else
                        {
                            try
                            {
                                current.Value = source.GetAudioClip(entry.Key);
                            }
                            catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException)
                            {
                                current.Value = null;
                            }
                        }
                        break;

                    case UnityClassIds.MonoBehaviour:
                        current.Value = new UnityWzImage(source, entry.Key);
                        if (includeTrees)
                        {
                            try
                            {
                                WzjsNode tree = source.GetTree(entry.Key);
                                foreach (WzjsNode child in tree.Children)
                                {
                                    current.Nodes.Add(ConvertNode(
                                        child, entry.Key, includeImages ? source : null));
                                }
                            }
                            catch (Exception ex) when (ex is UnityException || ex is WzjsException || ex is ClassicAssetException)
                            {
                                current.Nodes.Clear();
                            }
                        }
                        break;

                    case UnityClassIds.Sprite:
                        // Single sprite from a spriteset (SpriteSet/<region>/UI/…): the image is resolved by locale when decoding.
                        current.Value = new UnityCanvas(source, entry.Key);
                        ClassicWzSource.AttachSpriteOrigin(current, source, entry.Key);
                        break;
                }
            }
        }

        /// <summary>
        /// Collapses each directory in <see cref="WzImagedDirectories"/> / <see cref="WzImagedDirectoryParents"/>
        /// into a single img node.
        /// Does nothing for datasets where a real entity (WZJS) exists under the same key.
        /// </summary>
        private static void CollapseWzImagedDirectories(Wz_Node root, ClassicWzSource source)
        {
            foreach (string directoryPath in WzImagedDirectories)
            {
                CollapseWzImagedDirectory(root, directoryPath, source);
            }
            foreach (string parentPath in WzImagedDirectoryParents)
            {
                Wz_Node parent = FindNodeByPath(root, parentPath);
                if (parent == null)
                {
                    continue;
                }
                List<Wz_Node> children = new List<Wz_Node>(parent.Nodes);
                foreach (Wz_Node child in children)
                {
                    CollapseWzImagedDirectory(root, parentPath + "/" + child.Text, source);
                }
            }
        }

        private static void CollapseWzImagedDirectory(Wz_Node root, string directoryPath, ClassicWzSource source)
        {
            Wz_Node node = FindNodeByPath(root, directoryPath);
            if (node == null || node.Value != null || node.Nodes.Count == 0)
            {
                return;
            }
            node.Nodes.Clear();
            node.Text = node.Text + ".img";
            node.Value = source.GetDirectoryImage(directoryPath);
        }

        private static Wz_Node FindNodeByPath(Wz_Node root, string path)
        {
            Wz_Node node = root;
            foreach (string segment in path.Split('/'))
            {
                node = node?.Nodes[segment];
                if (node == null)
                {
                    return null;
                }
            }
            return node;
        }

        /// <summary>
        /// Appends the content of a catalog asset (a WZJS subtree or JSON properties) beneath
        /// <paramref name="target"/>.
        /// </summary>
        public static void AppendEntryContents(Wz_Node target, ClassicWzSource source, string imageKey)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            UnityEntryLocation location;
            if (!source.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                return;
            }
            switch (location.ClassId)
            {
                case UnityClassIds.TextAsset:
                    UnityJson.Populate(target, source.ReadTextAsset(imageKey).Script);
                    break;

                case UnityClassIds.MonoBehaviour:
                    foreach (WzjsNode child in source.GetTree(imageKey).Children)
                    {
                        target.Nodes.Add(ConvertNode(child, imageKey, source));
                    }
                    break;

                case UnityClassIds.AudioClip:
                    if (source.Catalog.IsSoundImage(imageKey))
                    {
                        UnitySoundImage soundImage = source.GetSoundImage(imageKey);
                        foreach (Wz_Node child in soundImage.AsWzNode().Nodes)
                        {
                            target.Nodes.Add(child);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Total action frame delay (sums the absolute value of each frame's <c>delay</c>).
        /// Returns 0 when the node is null or has no delay.
        /// </summary>
        public static int SumActionDelay(Wz_Node actionNode)
        {
            if (actionNode == null)
            {
                return 0;
            }
            int delay = 0;
            foreach (Wz_Node frameNode in actionNode.Nodes)
            {
                Wz_Node delayNode = frameNode.FindNodeByPath("delay");
                if (delayNode != null && delayNode.Value != null)
                {
                    delay += Math.Abs(delayNode.GetValueEx<int>(0));
                }
            }
            return delay;
        }

        /// <summary>Converts a WZJS node value into the value types WzLib conventionally uses.</summary>
        public static object ConvertValue(WzjsValue value)        {
            switch (value.Kind)
            {
                case WzjsValueKind.U8:
                case WzjsValueKind.U16:
                case WzjsValueKind.Int:
                    return (int)value.Integer;
                case WzjsValueKind.Long:
                    return value.Integer;
                case WzjsValueKind.Double:
                    return value.Real;
                case WzjsValueKind.Float2:
                    return new[] { value.X, value.Y };
                case WzjsValueKind.Int2:
                    return new[] { value.Int2A, value.Int2B };
                case WzjsValueKind.Vector:
                    return new Wz_Vector(
                        (int)Math.Round(value.X), (int)Math.Round(value.Y));
                case WzjsValueKind.String:
                    return value.Text;
                case WzjsValueKind.Uol:
                    return new Wz_Uol(value.Text);
                default:
                    return null;
            }
        }

        private static Wz_Node CreateImageNode(UnityWzImage image, string imageKey)
        {
            WzjsNode tree = image.Root;
            Wz_Node root = new Wz_Node(tree.Name);
            root.Value = image;
            foreach (WzjsNode child in tree.Children)
            {
                root.Nodes.Add(ConvertNode(child, imageKey, image.Source));
            }
            return root;
        }

        internal static Wz_Node ConvertNode(WzjsNode node, string imageKey, ClassicWzSource imageSource)
        {
            return ConvertNode(node, imageKey, string.Empty, imageSource);
        }

        /// <summary>
        /// Converts a single node while tracking the relative path, so a Canvas can find its PNG.
        /// </summary>
        private static Wz_Node ConvertNode(WzjsNode node, string imageKey, string parentPath, ClassicWzSource imageSource)
        {
            string path = parentPath.Length == 0 ? node.Name : parentPath + "/" + node.Name;
            Wz_Node result = new Wz_Node(node.Name);
            result.Value = ConvertNodeValue(node, imageKey, path, imageSource);
            foreach (WzjsNode child in node.Children)
            {
                result.Nodes.Add(ConvertNode(child, imageKey, path, imageSource));
            }
            return result;
        }

        private static object ConvertNodeValue(
            WzjsNode node, string imageKey, string innerPath, ClassicWzSource imageSource)
        {
            if (node.IsCanvas)
            {
                if (imageSource == null)
                {
                    return null;
                }
                string fullPath = innerPath.Length == 0 ? imageKey : imageKey + "/" + innerPath;
                return new UnityCanvas(imageSource, fullPath);
            }

            // In Classic, images that live in a spriteset (e.g. Map/MapHelper/worldMap/mapImage)
            // appear on the WZJS side as leaf nodes with no value and no children.
            // If the catalog's sprite index has the same path, treat it as a Canvas to match the WZ shape.
            if (node.Value.Kind == WzjsValueKind.SubTree && node.Children.Count == 0
                && imageSource != null && innerPath.Length > 0)
            {
                string spritePath = imageKey + "/" + innerPath;
                UnityEntryLocation location;
                if (imageSource.Catalog.Entries.TryGetValue(spritePath, out location)
                    && location.ClassId == UnityClassIds.Sprite)
                {
                    return new UnityCanvas(imageSource, spritePath);
                }
            }

            return ConvertValue(node.Value);
        }
    }
}

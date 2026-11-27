using System;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// WZ-like representation of a TextAsset (class 49) JSON asset; the node tree is parsed on first use.
    /// </summary>
    public sealed class UnityJsonAsset
    {
        private readonly ClassicWzSource source;
        private readonly object syncRoot = new object();
        private Wz_Node node;
        private Wz_Node rootedNode;
        private string text;

        public UnityJsonAsset(ClassicWzSource source, string imageKey)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (imageKey == null)
            {
                throw new ArgumentNullException(nameof(imageKey));
            }
            this.source = source;
            this.ImageKey = imageKey;
        }

        /// <summary>Index key of the asset, e.g. <c>String/CN/Skill</c>.</summary>
        public string ImageKey { get; private set; }

        /// <summary>Node name (the last segment of the index key), e.g. <c>Skill</c>.</summary>
        public string Name
        {
            get
            {
                int slash = this.ImageKey.LastIndexOf('/');
                return slash < 0 ? this.ImageKey : this.ImageKey.Substring(slash + 1);
            }
        }

        /// <summary>Raw JSON text (UTF-8).</summary>
        public string Text
        {
            get
            {
                if (this.text == null)
                {
                    this.text = Encoding.UTF8.GetString(this.source.ReadTextAsset(this.ImageKey).Script);
                }
                return this.text;
            }
        }

        /// <summary>Gets the content as a WZ node tree (parsed and cached on first call).</summary>
        public Wz_Node AsWzNode()
        {
            if (this.node == null)
            {
                lock (this.syncRoot)
                {
                    if (this.node == null)
                    {
                        this.node = UnityJson.Parse(
                            this.source.ReadTextAsset(this.ImageKey).Script, this.Name);
                    }
                }
            }
            return this.node;
        }

        /// <summary>
        /// Content root node. When needed, prepends a chain of parent nodes named after the directories
        /// (e.g. <c>String\CN\Skill.img\…</c>) so that path handling such as <see cref="Wz_Node.FullPathToFile"/>
        /// keeps working with the WZ logic. Plays the same role as <see cref="UnityAssetImage.GetRootedNode"/>.
        /// </summary>
        public Wz_Node GetRootedNode()
        {
            Wz_Node content = this.AsWzNode();
            if (content.ParentNode != null)
            {
                return content;
            }
            lock (this.syncRoot)
            {
                if (this.rootedNode == null)
                {
                    this.rootedNode = CreateRootedTree(content, this.ImageKey);
                }
            }
            return content;
        }

        private Wz_Node CreateRootedTree(Wz_Node content, string imageKey)
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

            string imageName = segments[segments.Length - 1];
            Wz_Node imageNode = new Wz_Node(
                imageName.EndsWith(".img", StringComparison.OrdinalIgnoreCase) ? imageName : imageName + ".img");
            imageNode.Value = this;
            parent.Nodes.Add(imageNode);
            imageNode.Nodes.Add(content);
            return root;
        }

        public override string ToString()
        {
            return "json " + this.ImageKey;
        }
    }
}

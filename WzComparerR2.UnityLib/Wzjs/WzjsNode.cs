using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.UnityLib.Wzjs
{
    /// <summary>
    /// Parsed WZJS node tree model, mirroring the Property / Canvas / Uol hierarchy of classic MapleStory WZ.
    /// </summary>
    public sealed class WzjsNode
    {
        private static readonly WzjsNode[] EmptyChildren = new WzjsNode[0];
        private readonly WzjsNode[] children;

        internal WzjsNode(string name, WzjsValue value, string path, WzjsNode[] children, int typeTag)
        {
            this.Name = name;
            this.Value = value;
            this.Path = path;
            this.children = children ?? EmptyChildren;
            this.TypeTag = typeTag;
        }

        public string Name { get; private set; }

        public WzjsValue Value { get; private set; }

        /// <summary>
        /// pool B path: path relative to the img root (for the root node this is the asset path,
        /// for example <c>Character/Afterimage/blank</c>).
        /// </summary>
        public string Path { get; private set; }

        public IReadOnlyList<WzjsNode> Children
        {
            get { return this.children; }
        }

        /// <summary>Raw type tag (used for debugging and export cross-reference).</summary>
        public int TypeTag { get; private set; }

        public bool IsCanvas
        {
            get { return this.Value.Kind == WzjsValueKind.Canvas; }
        }

        /// <summary>Canvas nodes carry pixels; UOL nodes are links, but the WZSS node key pool lists them as well.</summary>
        public bool IsImageCapable
        {
            get
            {
                return WzjsImage.IsCanvasTag(this.TypeTag) || this.TypeTag == WzjsImage.TypeUol;
            }
        }

        /// <summary>Walks down the child nodes segment by segment (names are case-sensitive).</summary>
        public WzjsNode Find(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            WzjsNode current = this;
            int index = 0;
            while (index <= path.Length)
            {
                int slash = path.IndexOf('/', index);
                int end = slash < 0 ? path.Length : slash;
                string segment = path.Substring(index, end - index);
                current = current.FindChild(segment);
                if (current == null)
                {
                    return null;
                }
                if (slash < 0)
                {
                    break;
                }
                index = slash + 1;
            }
            return current;
        }

        public WzjsNode FindChild(string name)
        {
            for (int i = 0; i < this.children.Length; i++)
            {
                if (string.Equals(this.children[i].Name, name, StringComparison.Ordinal))
                {
                    return this.children[i];
                }
            }
            return null;
        }

        /// <summary>Total node count (including this node).</summary>
        public int CountNodes()
        {
            int count = 1;
            for (int i = 0; i < this.children.Length; i++)
            {
                count += this.children[i].CountNodes();
            }
            return count;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.Name).Append(" (type ").Append(this.TypeTag).Append(')');
            if (this.Value.HasValue)
            {
                builder.Append(" = ").Append(this.Value);
            }
            return builder.ToString();
        }
    }
}

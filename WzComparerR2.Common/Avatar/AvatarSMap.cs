using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WzComparerR2.WzLib;

namespace WzComparerR2.Avatar
{
    public sealed class AvatarSMap
    {
        private static readonly IReadOnlyList<string> empty = new ReadOnlyCollection<string>(new List<string>());

        public AvatarSMap()
        {
            this.entries = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        }

        private readonly Dictionary<string, IReadOnlyList<string>> entries;

        public int Count => this.entries.Count;

        public IReadOnlyDictionary<string, IReadOnlyList<string>> Entries
            => new ReadOnlyDictionary<string, IReadOnlyList<string>>(this.entries);

        public bool Load(Wz_Node node)
        {
            this.entries.Clear();

            node = node.ExtractImageNode();
            if (node == null)
            {
                return false;
            }

            // Later definitions replace earlier masks with the same z name.
            foreach (Wz_Node child in node.Nodes)
            {
                string value = child.ReadString();
                this.entries[child.Text] = AvatarSlotTokens.Parse(value);
            }

            return true;
        }

        public bool TryGetTokens(string zSlot, out IReadOnlyList<string> tokens)
        {
            return this.entries.TryGetValue(zSlot, out tokens);
        }
    }
}

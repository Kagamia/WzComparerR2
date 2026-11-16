using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using WzComparerR2.WzLib;

namespace WzComparerR2.Avatar
{
    public sealed class AvatarZMap
    {
        public AvatarZMap()
        {
            this.layers = new List<string>();
            this.values = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        private readonly List<string> layers;
        private readonly Dictionary<string, int> values;

        public IReadOnlyList<string> Layers => new ReadOnlyCollection<string>(this.layers);

        public bool Load(Wz_Node node)
        {
            this.layers.Clear();
            this.values.Clear();

            node = node.ExtractImageNode();
            if (node == null)
            {
                return false;
            }

            // Original empty table entries receive descending negative z values in stored order.
            int currentZ = 0;
            foreach (Wz_Node child in node.Nodes)
            {
                currentZ--;
                this.values[child.Text] = currentZ;
                this.layers.Add(child.Text);
            }

            return true;
        }

        public int ResolveZ(AvatarLayerSource source)
        {
            return ResolveZ(source, this.values);
        }

        internal static int ResolveZ(AvatarLayerSource source, IReadOnlyDictionary<string, int> values)
        {
            if (source == null)
            {
                return 0;
            }

            // A supplied string takes precedence; numeric strings are parsed before name lookup.
            if (source.ZSlot != null)
            {
                if (int.TryParse(source.ZSlot, NumberStyles.Integer, CultureInfo.InvariantCulture, out int z))
                {
                    return z;
                }
                else if (values.TryGetValue(source.ZSlot, out z))
                {
                    return z;
                }

                return int.MinValue;
            }

            if (source.ExplicitZ != null)
            {
                return source.ExplicitZ.Value;
            }

            return 0;
        }
    }
}

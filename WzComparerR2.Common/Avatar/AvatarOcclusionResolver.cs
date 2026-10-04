using System;
using System.Collections.Generic;
using System.Linq;

namespace WzComparerR2.Avatar
{
    public sealed class AvatarOcclusionResolver
    {
        public AvatarOcclusionResolver(IReadOnlyDictionary<string, int> zMap,
            IReadOnlyDictionary<string, IReadOnlyList<string>> sMap)
        {
            this.zMap = zMap ?? throw new ArgumentNullException(nameof(zMap));
            this.sMap = sMap ?? throw new ArgumentNullException(nameof(sMap));
        }

        private readonly IReadOnlyDictionary<string, int> zMap;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> sMap;

        public void Resolve(AvatarLayout layout, IEnumerable<string> reservedSlots = null)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            IReadOnlyList<string> reserved = reservedSlots == null ? Array.Empty<string>() : new List<string>(reservedSlots).AsReadOnly();
            if (reserved.Any(token => token == null || token.Length != 2))
                throw new ArgumentException("Reserved avatar slots must be two-character tokens.", nameof(reservedSlots));

            layout.ReservedSlots = reserved;
            layout.SortByZ();

            // Slot priority is independent of drawing z; reset per-layer state for this run.
            foreach (AvatarLayoutLayer layer in layout.Layers)
            {
                AvatarPartContext part = layer.Source.Part;
                int priority = -int.MaxValue;
                if (part != null)
                {
                    foreach (string token in part.ISlotTokens)
                    {
                        if (this.zMap.TryGetValue(token, out int value))
                        {
                            priority = Math.Max(priority, value);
                        }
                    }
                }

                // Intersect in vslot order, retaining repeated matching tokens.
                var mask = new List<string>();
                string zName = layer.Source.ZSlot;
                if (part != null && zName != null && this.sMap.TryGetValue(zName, out var mapped))
                {
                    foreach (string visual in part.VSlotTokens)
                    {
                        foreach (string token in mapped)
                        {
                            if (visual == token)
                            {
                                mask.Add(visual);
                            }
                        }
                    }
                }

                layer.SlotPriority = priority;
                layer.SlotMask = mask.AsReadOnly();
                layer.OcclusionVisible = true;
            }

            // Character and mount layers maintain independent ownership tables.
            foreach (var domain in layout.Layers.GroupBy(layer => layer.Source.Part?.Group ?? AvatarLayerGroup.Character))
            {
                var owners = new Dictionary<string, AvatarLayoutLayer>(StringComparer.Ordinal);

                // A null owner marks a reserved slot, not an unclaimed one.
                if (domain.Key == AvatarLayerGroup.Character)
                {
                    foreach (string token in reserved)
                    {
                        owners[token] = null;   
                    }
                }

                // Failure hides the layer without rolling back its earlier claims or replacements.
                foreach (AvatarLayoutLayer layer in domain)
                {
                    foreach (string token in layer.SlotMask)
                    {
                        if (!owners.TryGetValue(token, out AvatarLayoutLayer previous))
                        {
                            owners.Add(token, layer);
                            continue;
                        }

                        if (previous == null)
                        {
                            layer.OcclusionVisible = false;
                            break;
                        }

                        // Hidden owners still compete; ties coexist without changing ownership.
                        int difference = unchecked(previous.SlotPriority - layer.SlotPriority);
                        if (difference == 0)
                            continue;
                        if (difference > 0)
                        {
                            layer.OcclusionVisible = false;
                            break;
                        }

                        previous.OcclusionVisible = false;
                        owners[token] = layer;
                    }
                }
            }
        }
    }
}
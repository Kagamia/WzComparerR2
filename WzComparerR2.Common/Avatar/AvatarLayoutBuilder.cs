using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WzComparerR2.Avatar
{
    public sealed class AvatarLayoutBuilder
    {
        public AvatarLayoutBuilder(IReadOnlyDictionary<string, int> zMap)
        {
            this.zMap = zMap ?? throw new ArgumentNullException(nameof(zMap));
        }

        private readonly IReadOnlyDictionary<string, int> zMap;

        public AvatarLayout Build(IEnumerable<AvatarLayerSource> sources, AvatarLayoutFrameInfo frameInfo = null,
            IEnumerable<string> diagnostics = null)
        {
            return this.Build(sources, false, frameInfo, diagnostics);
        }

        public AvatarLayout Build(IEnumerable<AvatarLayerSource> sources, bool attachTaming,
            AvatarLayoutFrameInfo frameInfo = null, IEnumerable<string> diagnostics = null)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var messages = diagnostics == null ? new List<string>() : new List<string>(diagnostics);
            var groups = new List<AnchorGroup>();
            var pending = new List<PendingLayer>();
            foreach (AvatarLayerSource source in sources)
            {
                if (source == null)
                    throw new ArgumentException("Null avatar layer source.", nameof(sources));

                // Anchors use the canvas offset; image placement also subtracts its origin.
                var group = new AnchorGroup(source.Part?.Group ?? AvatarLayerGroup.Character);
                foreach (AvatarMapPoint anchor in source.MapAnchors)
                    group.Points.Add(new AvatarMapPoint(anchor.Name, Add(anchor.Position, source.Offset)));

                var layer = new PendingLayer(source, Subtract(source.Offset, source.Origin), group);
                group.Layers.Add(layer);
                groups.Add(group);
                pending.Add(layer);

                // Repeated merges let this canvas bridge previously disconnected groups.
                while (true)
                {
                    AnchorGroup other = groups.FirstOrDefault(candidate => candidate != group && candidate.Domain == group.Domain
                        && candidate.Points.Any(target => group.Points.Any(point => point.Name == target.Name)));
                    if (other == null)
                        break;

                    // Preserve the first group in this domain when choosing the merge target.
                    AnchorGroup first = groups.First(candidate => candidate.Domain == group.Domain);
                    AnchorGroup targetGroup = group == first ? group : other;
                    AnchorGroup sourceGroup = group == first ? other : group;

                    Merge(targetGroup, sourceGroup);
                    groups.Remove(sourceGroup);
                    group = targetGroup;
                }
            }

            // Translate the complete character domain before taking output point snapshots.
            if (attachTaming)
                AttachTaming(groups, messages);

            // Assign result-local IDs only after all groups have finished merging.
            var outputGroups = new List<AvatarLayoutAnchorGroup>();
            for (int index = 0; index < groups.Count; index++)
            {
                groups[index].Id = index;
                outputGroups.Add(new AvatarLayoutAnchorGroup(index, groups[index].Domain, groups[index].Points));
            }

            bool hasTaming = attachTaming || frameInfo?.Taming != null || frameInfo?.TamingFrame != null
                || groups.Any(group => group.Domain == AvatarLayerGroup.Taming);
            var transformGroups = groups.Select(group => group.Domain).Distinct().OrderBy(domain => domain)
                .Select((domain, index) => CreateTransformGroup(index, domain, frameInfo, hasTaming)).ToList();
            foreach (AvatarLayoutTransformGroup group in transformGroups)
            {
                if (!group.IsResolved)
                    messages.Add("Unresolved " + group.Group + " transform: " + group.UnresolvedReason);
            }
            var transformIds = transformGroups.ToDictionary(group => group.Group, group => group.Id);

            // Per-layer map points retain their own geometry, distinct from canonical group points.
            var outputLayers = new List<AvatarLayoutLayer>();
            foreach (PendingLayer layer in pending)
            {
                Point translation = Add(layer.Position, layer.Source.Origin);
                var map = layer.Source.MapAnchors.Select(anchor => new AvatarMapPoint(anchor.Name, Add(anchor.Position, translation)));
                var outputLayer = new AvatarLayoutLayer(layer.Source, layer.Position, AvatarZMap.ResolveZ(layer.Source, this.zMap),
                    outputLayers.Count, map, layer.Group.Id, transformIds[layer.Group.Domain]);
                outputLayer.FrameDisplayVisible = layer.Group.Domain != AvatarLayerGroup.Character || frameInfo?.FrameRemoveBody != true;
                outputLayers.Add(outputLayer);
            }

            var layout = new AvatarLayout(outputLayers, outputGroups, frameInfo, messages, transformGroups);
            layout.SortByZ();
            return layout;
        }

        private static AvatarLayoutTransformGroup CreateTransformGroup(int id, AvatarLayerGroup domain,
            AvatarLayoutFrameInfo frameInfo, bool hasTaming)
        {
            AvatarAnimationFrame character = frameInfo?.CharacterFrame;
            AvatarAnimationFrame taming = frameInfo?.TamingFrame;
            bool baseFlip = !(frameInfo?.FacingLeft ?? true);
            bool characterFlip = baseFlip ^ (character?.Flip ?? false);
            if (character?.JustDir == 1)
                characterFlip = true;
            else if (character?.JustDir == 2)
                characterFlip = false;

            bool tamingFlip = taming?.ForceCharacterDirection > 0 ? characterFlip : baseFlip;
            int rotation = AvatarRenderTransform.GetRightAngleRotation(character?.Rotate ?? 0);
            Point move = character?.Move ?? Point.Empty;
            string unresolvedReason = null;
            if ((character?.RotateProp ?? 0) != 0 || (hasTaming && (taming?.RotateProp ?? 0) != 0))
                unresolvedReason = "Nonzero rotateProp semantics are not resolved.";
            else if (hasTaming && characterFlip != tamingFlip)
                unresolvedReason = "Different character and mount reflection axes require resolved attachment semantics.";
            else if (hasTaming && taming != null
                && (taming.Flip || taming.Rotate != 0 || taming.Move != Point.Empty))
            {
                unresolvedReason = "Mount-frame transforms are not resolved by the shared character-motion model.";
            }

            AvatarRenderTransform transform = null;
            if (unresolvedReason == null)
            {
                float moveX = rotation == 0 && baseFlip ? -(float)move.X : move.X;
                var translation = new PointF(moveX, move.Y);
                bool flip = domain == AvatarLayerGroup.Character ? characterFlip : tamingFlip;
                float axis = rotation == 0 ? moveX : 0;
                transform = new AvatarRenderTransform(rotation, PointF.Empty, translation, flip, axis);
            }

            return new AvatarLayoutTransformGroup(id, domain, frameInfo, transform, unresolvedReason);
        }

        private static void AttachTaming(IList<AnchorGroup> groups, IList<string> diagnostics)
        {
            var characterNavel = groups.Where(group => group.Domain == AvatarLayerGroup.Character)
                .SelectMany(group => group.Points)
                .Where(point => point.Name == "navel")
                .ToList();
            var tamingNavel = groups.Where(group => group.Domain == AvatarLayerGroup.Taming)
                .SelectMany(group => group.Points)
                .Where(point => point.Name == "navel")
                .ToList();
            if (characterNavel.Count != 1 || tamingNavel.Count != 1)
            {
                diagnostics.Add("Taming attachment requires one navel anchor per domain; character="
                    + characterNavel.Count + ", taming=" + tamingNavel.Count + ".");
                return;
            }

            Point delta = Subtract(tamingNavel[0].Position, characterNavel[0].Position);
            foreach (AnchorGroup group in groups)
            {
                if (group.Domain != AvatarLayerGroup.Character)
                    continue;

                for (int index = 0; index < group.Points.Count; index++)
                {
                    AvatarMapPoint point = group.Points[index];
                    group.Points[index] = new AvatarMapPoint(point.Name, Add(point.Position, delta));
                }
                foreach (PendingLayer layer in group.Layers)
                    layer.Position = Add(layer.Position, delta);
            }
        }

        private static void Merge(AnchorGroup target, AnchorGroup source)
        {
            Point targetSum = Point.Empty;
            Point sourceSum = Point.Empty;
            int matches = 0;
            var unmatched = new List<AvatarMapPoint>();

            // Each source point matches the first target point of the same name.
            foreach (AvatarMapPoint point in source.Points)
            {
                int index = target.Points.FindIndex(candidate => candidate.Name == point.Name);
                if (index < 0)
                {
                    unmatched.Add(point);
                    continue;
                }

                targetSum = Add(targetSum, target.Points[index].Position);
                sourceSum = Add(sourceSum, point.Position);
                matches++;
            }

            // Match native Int32 accumulation and signed division toward zero.
            Point difference = Subtract(targetSum, sourceSum);
            Point delta = new Point(difference.X / matches, difference.Y / matches);

            // Keep target coordinates; append only translated, unmatched source points.
            foreach (AvatarMapPoint point in unmatched)
                target.Points.Add(new AvatarMapPoint(point.Name, Add(point.Position, delta)));

            foreach (PendingLayer layer in source.Layers)
            {
                layer.Position = Add(layer.Position, delta);
                layer.Group = target;
                target.Layers.Add(layer);
            }
        }

        private static Point Add(Point left, Point right)
        {
            return new Point(unchecked(left.X + right.X), unchecked(left.Y + right.Y));
        }

        private static Point Subtract(Point left, Point right)
        {
            return new Point(unchecked(left.X - right.X), unchecked(left.Y - right.Y));
        }

        private sealed class AnchorGroup
        {
            public AnchorGroup(AvatarLayerGroup domain) { this.Domain = domain; }

            public AvatarLayerGroup Domain { get; }
            public List<AvatarMapPoint> Points { get; } = new List<AvatarMapPoint>();
            public List<PendingLayer> Layers { get; } = new List<PendingLayer>();
            public int Id { get; set; }
        }

        private sealed class PendingLayer
        {
            public PendingLayer(AvatarLayerSource source, Point position, AnchorGroup group)
            {
                this.Source = source;
                this.Position = position;
                this.Group = group;
            }

            public AvatarLayerSource Source { get; }
            public Point Position { get; set; }
            public AnchorGroup Group { get; set; }
        }
    }
}
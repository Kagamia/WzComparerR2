using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WzComparerR2.Avatar
{
    public sealed class AvatarLayoutTransformGroup
    {
        public AvatarLayoutTransformGroup(int id, AvatarLayerGroup group)
            : this(id, group, null, AvatarRenderTransform.Identity, null)
        {
        }

        public AvatarLayoutTransformGroup(int id, AvatarLayerGroup group, AvatarRenderTransform transform)
            : this(id, group, null, transform ?? throw new ArgumentNullException(nameof(transform)), null)
        {
        }

        internal AvatarLayoutTransformGroup(int id, AvatarLayerGroup group, AvatarLayoutFrameInfo frameInfo,
            AvatarRenderTransform transform, string unresolvedReason)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id));
            if (!Enum.IsDefined(typeof(AvatarLayerGroup), group))
                throw new ArgumentOutOfRangeException(nameof(group));

            this.Id = id;
            this.Group = group;
            this.RawFrame = group == AvatarLayerGroup.Character ? frameInfo?.RawCharacterFrame : frameInfo?.TamingFrame;
            this.EffectiveFrame = group == AvatarLayerGroup.Character ? frameInfo?.CharacterFrame : frameInfo?.TamingFrame;
            this.FacingLeft = frameInfo?.FacingLeft ?? true;
            this.transform = transform;
            this.UnresolvedReason = unresolvedReason;
        }

        private readonly AvatarRenderTransform transform;

        public int Id { get; }
        public AvatarLayerGroup Group { get; }
        public AvatarAnimationFrame RawFrame { get; }
        public AvatarAnimationFrame EffectiveFrame { get; }
        public bool FacingLeft { get; }
        public bool IsResolved => this.transform != null;
        public string UnresolvedReason { get; }
        public AvatarRenderTransform Transform => this.transform ?? throw new NotSupportedException(this.UnresolvedReason);
    }

    public sealed class AvatarLayoutAnchorGroup
    {
        public AvatarLayoutAnchorGroup(int id, AvatarLayerGroup group, IEnumerable<AvatarMapPoint> points)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id));

            this.Id = id;
            this.Group = group;
            this.Points = new List<AvatarMapPoint>(points ?? throw new ArgumentNullException(nameof(points))).AsReadOnly();
        }

        public int Id { get; }
        public AvatarLayerGroup Group { get; }
        public IReadOnlyList<AvatarMapPoint> Points { get; }
    }

    public sealed class AvatarFrameSelection
    {
        public AvatarFrameSelection(string action, int frame)
        {
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("An action or emotion name is required.", nameof(action));
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame));

            this.Action = action;
            this.Frame = frame;
        }

        public string Action { get; }
        public int Frame { get; }
    }

    public sealed class AvatarLayoutFrameInfo
    {
        public AvatarLayoutFrameInfo(AvatarFrameSelection character, AvatarFrameSelection taming, AvatarFrameSelection emotion)
            : this(character, taming, emotion, character)
        {
        }

        public AvatarLayoutFrameInfo(AvatarFrameSelection character, AvatarFrameSelection taming, AvatarFrameSelection emotion,
            AvatarFrameSelection body)
        {
            this.Character = character ?? throw new ArgumentNullException(nameof(character));
            this.Taming = taming;
            this.Emotion = emotion;
            this.Body = body ?? character;
        }

        public AvatarFrameSelection Character { get; }
        public AvatarFrameSelection Taming { get; }
        public AvatarFrameSelection Emotion { get; }
        public AvatarFrameSelection Body { get; }

        public AvatarAnimationFrame CharacterFrame { get; init; }
        public AvatarAnimationFrame RawCharacterFrame { get; init; }
        public AvatarAnimationFrame TamingFrame { get; init; }
        public bool FacingLeft { get; init; } = true;
        public bool RemoveBody { get; init; }
        public bool FrameRemoveBody { get; init; }
        public bool FaceAllowed { get; init; }
    }

    public sealed class AvatarLayout
    {
        public AvatarLayout(IList<AvatarLayoutLayer> layers)
            : this(layers, null, null, null)
        {
        }

        public AvatarLayout(IList<AvatarLayoutLayer> layers, IEnumerable<AvatarLayoutAnchorGroup> anchorGroups,
            AvatarLayoutFrameInfo frameInfo, IEnumerable<string> diagnostics)
            : this(layers, anchorGroups, frameInfo, diagnostics, null)
        {
        }

        public AvatarLayout(IList<AvatarLayoutLayer> layers, IEnumerable<AvatarLayoutAnchorGroup> anchorGroups,
            AvatarLayoutFrameInfo frameInfo, IEnumerable<string> diagnostics,
            IEnumerable<AvatarLayoutTransformGroup> transformGroups)
        {
            // Copy the collections, while retaining the supplied layer objects.
            this.Layers = new List<AvatarLayoutLayer>(layers);
            this.AnchorGroups = anchorGroups == null ? Array.Empty<AvatarLayoutAnchorGroup>() : new List<AvatarLayoutAnchorGroup>(anchorGroups).AsReadOnly();
            this.FrameInfo = frameInfo;
            this.Diagnostics = diagnostics == null ? Array.Empty<string>() : new List<string>(diagnostics).AsReadOnly();
            this.TransformGroups = transformGroups == null ? Array.Empty<AvatarLayoutTransformGroup>()
                : new List<AvatarLayoutTransformGroup>(transformGroups).AsReadOnly();
        }

        public IList<AvatarLayoutLayer> Layers { get; private set; }
        public IReadOnlyList<AvatarLayoutAnchorGroup> AnchorGroups { get; }
        public IReadOnlyList<AvatarLayoutTransformGroup> TransformGroups { get; }
        public AvatarLayoutFrameInfo FrameInfo { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public IReadOnlyList<string> ReservedSlots { get; internal set; } = Array.Empty<string>();

        public void SortByZ()
        {
            // TODO: in-place stable sort instead of linq
            AvatarLayoutLayer[] ordered = this.Layers.OrderBy(layer => layer.Z)
                .ThenBy(layer => layer.Order).ToArray();

            // Preserve the list instance exposed to callers.
            for (int index = 0; index < ordered.Length; index++)
            {
                this.Layers[index] = ordered[index];
            }
        }

        public void ResetVisibility()
        {
            foreach (AvatarLayoutLayer layer in this.Layers)
            {
                layer.Visible = true;
                layer.OcclusionVisible = true;
            }
        }

        public Rectangle GetBounds(bool visibleOnly)
        {
            Rectangle result = Rectangle.Empty;
            foreach (AvatarLayoutLayer layer in this.Layers)
            {
                if (visibleOnly && !layer.Visible)
                {
                    continue;
                }

                Rectangle bounds = layer.GetBounds();
                if (bounds.IsEmpty)
                {
                    continue;
                }

                result = result.IsEmpty ? bounds : Rectangle.Union(result, bounds);
            }

            return result;
        }
    }
}

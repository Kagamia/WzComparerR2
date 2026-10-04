using System;
using System.Collections.Generic;
using System.Drawing;
using WzComparerR2.WzLib;

namespace WzComparerR2.Avatar
{
    public readonly struct AvatarMapPoint
    {
        public AvatarMapPoint(string name, Point position)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Position = position;
        }

        public string Name { get; }
        public Point Position { get; }
    }

    public sealed class AvatarLayerSource
    {
        public AvatarLayerSource(
            AvatarPartContext part,
            string name,
            Wz_Node node,
            Wz_Png canvas,
            Point origin)
        {
            this.Part = part;
            this.Name = name ?? string.Empty;
            this.Node = node;
            this.Canvas = canvas;
            this.Origin = origin;
            this.MapAnchors = new List<AvatarMapPoint>();
        }

        public AvatarPartContext Part { get; private set; }
        public string Name { get; private set; }
        public Wz_Node Node { get; private set; }

        public Wz_Png Canvas { get; private set; }
        public Point Origin { get; private set; }
        public IList<AvatarMapPoint> MapAnchors { get; private set; }
        public Point Offset { get; set; }

        public string ZSlot { get; set; }
        public int? ExplicitZ { get; set; }
    }

    public sealed class AvatarLayoutLayer
    {
        public AvatarLayoutLayer(AvatarLayerSource source, Point position, int z, int order)
            : this(source, position, z, order, null, null)
        {
        }

        public AvatarLayoutLayer(AvatarLayerSource source, Point position, int z, int order,
            IEnumerable<AvatarMapPoint> mapPoints, int? anchorGroupId)
            : this(source, position, z, order, mapPoints, anchorGroupId, null)
        {
        }

        public AvatarLayoutLayer(AvatarLayerSource source, Point position, int z, int order,
            IEnumerable<AvatarMapPoint> mapPoints, int? anchorGroupId, int? transformGroupId)
        {
            this.Source = source;
            this.Position = position;
            this.Z = z;
            this.Order = order;

            // Capture calculated points; later Position edits do not recompute this snapshot.
            this.MapPoints = mapPoints == null ? Array.Empty<AvatarMapPoint>() : new List<AvatarMapPoint>(mapPoints).AsReadOnly();
            this.AnchorGroupId = anchorGroupId;
            this.TransformGroupId = transformGroupId;
        }

        private bool displayVisible = true;

        public AvatarLayerSource Source { get; private set; }
        public Point Position { get; set; }
        public int Z { get; private set; }
        public int Order { get; private set; }

        public bool Visible
        {
            get { return this.displayVisible && this.OcclusionVisible && this.FrameDisplayVisible; }
            set { this.displayVisible = value; }
        }

        internal bool OcclusionVisible { get; set; } = true;
        internal bool FrameDisplayVisible { get; set; } = true;

        public IReadOnlyList<AvatarMapPoint> MapPoints { get; }
        public int? AnchorGroupId { get; }
        public int? TransformGroupId { get; }

        public int SlotPriority { get; internal set; } = -int.MaxValue;
        public IReadOnlyList<string> SlotMask { get; internal set; } = Array.Empty<string>();

        public Rectangle GetBounds()
        {
            // Bounds use PNG metadata only; decoding is deferred to the renderer.
            Wz_Png png = this.Source.Canvas;
            return png == null || png.Width <= 0 || png.Height <= 0
                ? Rectangle.Empty
                : new Rectangle(this.Position, new Size(png.Width, png.Height));
        }
    }
}

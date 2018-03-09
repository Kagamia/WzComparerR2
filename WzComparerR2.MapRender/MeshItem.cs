using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    class MeshItem : IComparable<MeshItem>
    {
        public object RenderObject { get; set; }
        public Vector2 Position { get; set; }
        public int Z0 { get; set; }
        public int Z1 { get; set; }

        //附加信息
        public bool FlipX { get; set; }
        public Rectangle? TileRegion { get; set; }
        public Vector2 TileOffset { get; set; }

        public int CompareTo(MeshItem other)
        {
            int comp;
            if ((comp = this.Z0.CompareTo(other.Z0)) != 0
                || (comp = this.Z1.CompareTo(other.Z1)) != 0)
            {
                return comp;
            }
            return 0;
        }
    }
}

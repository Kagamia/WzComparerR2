using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    class LineListMesh
    {
        public LineListMesh(Point from, Point to, Color color)
            : this(from, to, color, 1)
        {
        }

        public LineListMesh(Point from, Point to, Color color, int thickness)
            : this(new Point[] { from, to }, color, thickness)
        {
        }

        public LineListMesh(Point[] lines, Color color)
            : this(lines, color, 1)
        {
        }

        public LineListMesh(Point[] lines, Color color, int thickness)
        {
            this.Lines = lines;
            this.Color = color;
            this.Thickness = thickness;
        }

        public Point[] Lines { get; set; }
        public int Thickness { get; set; } = 1;
        public Color Color { get; set; }
    }
}

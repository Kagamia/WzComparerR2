using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.Rendering;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    class TextMesh
    {
        public string Text { get; set; }
        public IWcR2Font Font { get; set; }
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
        public Margins Padding { get; set; }
        public Alignment Align { get; set; }
    }

    struct Margins
    {
        public Margins(int left, int right, int top, int bottom)
        {
            this.Left = left;
            this.Right = right;
            this.Top = top;
            this.Bottom = bottom;
        }

        public int Left { get; set; }
        public int Right { get; set; }
        public int Top { get; set; }
        public int Bottom { get; set; }
    }

    enum Alignment
    {
        Near = 0,
        Center = 1,
        Far = 2
    }
}

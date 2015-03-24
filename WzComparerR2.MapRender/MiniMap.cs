using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender
{
    public class MiniMap
    {
        public MiniMap()
        {

        }

        public Texture2D Canvas { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int Mag { get; set; }

        public Texture2D MapMark { get; set; }
    }
}

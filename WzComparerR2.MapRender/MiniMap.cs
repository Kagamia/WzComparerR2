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
            this.ExtraCanvas = new Dictionary<string, Texture2D>();
        }

        public Texture2D Canvas { get; set; }
        public Dictionary<string, Texture2D> ExtraCanvas { get; private set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int Mag { get; set; }

        public Texture2D MapMark { get; set; }
    }
}

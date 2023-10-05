using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    public class MapLight
    {
        public int Mode { get; set; }
        public Color AmbientColor { get; set; }
        public Color DirectionalLightColor { get; set; }
        public float LuminanceLimit { get; set; }
        public Color BackColor { get; set; }
        public List<Light2D> Lights { get; private set; } = new List<Light2D>();
    }

    public class Light2D
    {
        public int Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Color Color { get; set; }
        public int InnerRadius { get; set; }
        public int OuterRadius { get; set; }
        public int InnerAngle { get; set; }
        public int OuterAngle { get; set; }
        public int DirectionAngle { get; set; }
    }
}

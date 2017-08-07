using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.WzLib;

namespace WzComparerR2.Avatar
{
    public class Skin
    {
        public string Name { get; set; }
        public BitmapOrigin Image { get; set; }
        public Point Offset { get; set; }
        public string Z { get; set; }
        public int ZIndex { get; set; }
    }
}

#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender.Patches
{
    public class TooltipPatch : RenderPatch
    {
        public TooltipPatch()
        {
        }

        public int X1 { get; set; }
        public int X2 { get; set; }
        public int Y1 { get; set; }
        public int Y2 { get; set; }

        public int CharX1 { get; set; }
        public int CharX2 { get; set; }
        public int CharY1 { get; set; }
        public int CharY2 { get; set; }

        public string Title { get; set; }
        public string Desc { get; set; }
    }
}
#endif
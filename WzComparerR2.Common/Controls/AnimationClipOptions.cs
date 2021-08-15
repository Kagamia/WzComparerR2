using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Controls
{
    public class AnimationClipOptions
    {
        public int? StartTime { get; set; }
        public int? StopTime { get; set; }

        public int? Left { get; set; }
        public int? Top { get; set; }
        public int? Right { get; set; }
        public int? Bottom { get; set; }

        public int? OutputWidth { get; set; }
        public int? OutputHeight { get; set; }
    }
}

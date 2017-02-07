using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WzComparerR2.Animation
{
    public class RepeatableFrameAnimationData : FrameAnimationData
    {
        public RepeatableFrameAnimationData()
        {

        }

        public RepeatableFrameAnimationData(IEnumerable<Frame> frames)
            : base(frames)
        {

        }

        public bool? Repeat { get; set; }
    }
}

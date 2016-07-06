using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Controls
{
    public abstract class AnimationItem : ICloneable
    {
        public Point Position { get; set; }

        public virtual int Length
        {
            get { return 0; }
        }

        public abstract void Update(TimeSpan elapsed);

        public virtual Rectangle Measure()
        {
            return Rectangle.Empty;
        }

        public virtual void Reset()
        {

        }

        public virtual object Clone()
        {
            var aniItem = (AnimationItem)base.MemberwiseClone();
            aniItem.Reset();
            return aniItem;
        }
    }
}

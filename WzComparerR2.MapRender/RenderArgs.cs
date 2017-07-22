#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    public class RenderArgs
    {
        public RenderArgs()
        {
            currentIndex = -1;
            currentPercent = 0f;
            culled = false;
            visible = true;
        }

        private int currentIndex;
        private float currentPercent;
        private bool culled;
        private bool visible;
        private Rectangle displayRectangle;

        public int CurrentIndex
        {
            get { return currentIndex; }
            set { currentIndex = value; }
        }

        public float CurrentPercent
        {
            get { return currentPercent; }
            set { currentPercent = value; }
        }

        public bool Culled
        {
            get { return culled; }
            set { culled = value; }
        }

        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        public Rectangle DisplayRectangle
        {
            get { return displayRectangle; }
            set { displayRectangle = value; }
        }

    }
}
#endif
#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender
{
    public class RenderFrame
    {
        public RenderFrame()
        {
            
        }

        Texture2D texture;
        Vector2 origin;
        int z;
        int delay;
        int a0;
        int a1;

        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }

        public Vector2 Origin
        {
            get { return origin; }
            set { origin = value; }
        }

        public int Z
        {
            get { return z; }
            set { z = value; }
        }
        
        public int Delay
        {
            get { return delay; }
            set { delay = value; }
        }
       
        public int A0
        {
            get { return a0; }
            set { a0 = value; }
        }

        public int A1
        {
            get { return a1; }
            set { a1 = value; }
        }
    }
}
#endif
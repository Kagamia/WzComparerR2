using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender.Patches
{
    public class FootholdPatch : RenderPatch
    {
        public FootholdPatch()
        {
        }

        private int x1;
        private int y1;
        private int x2;
        private int y2;
        private int prev;
        private int next;
        private int piece;

        public int X1
        {
            get { return x1; }
            set { x1 = value; }
        }

        public int Y1
        {
            get { return y1; }
            set { y1 = value; }
        }

        public int X2
        {
            get { return x2; }
            set { x2 = value; }
        }

        public int Y2
        {
            get { return y2; }
            set { y2 = value; }
        }

        public int Prev
        {
            get { return prev; }
            set { prev = value; }
        }

        public int Next
        {
            get { return next; }
            set { next = value; }
        }

        public int Piece
        {
            get { return piece; }
            set { piece = value; }
        }

        public override void Update(GameTime gameTime, RenderEnv env)
        {
        }

        public override void Draw(GameTime gameTime, RenderEnv env)
        {
            Vector2 origin = env.Camera.Origin;
            Point p1 = new Point(x1 - (int)origin.X, y1 - (int)origin.Y);
            Point p2 = new Point(x2 - (int)origin.X, y2 - (int)origin.Y);
            Color color = MathHelper2.Lerp(colorTable, (float)gameTime.TotalGameTime.TotalMilliseconds % 10000 / 2000);
            if (x1 != x2 && y1 != y2)
            {
            }
            env.Sprite.DrawLine(p1, p2, 2, color);
        }

        private static Color[] colorTable = new Color[] { 
            Color.Red,
            Color.Yellow,
            Color.Blue,
            Color.Purple,
            Color.Red};
    }
}

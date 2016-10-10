using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender.Patches
{
    public class LadderRopePatch : RenderPatch
    {
        public LadderRopePatch()
        {

        }

        public int X { get; set; }
        public int Y1 { get; set; }
        public int Y2 { get; set; }
        public int L { get; set; }
        public int Uf { get; set; }
        public int Page { get; set; }

        public override void Update(GameTime gameTime, RenderEnv env)
        {

        }

        public override void Draw(GameTime gameTime, RenderEnv env)
        {
            Vector2 origin = env.Camera.Origin;
            Point p1 = new Point(this.X - (int)origin.X, this.Y1 - (int)origin.Y);
            Point p2 = new Point(this.X - (int)origin.X, this.Y2 - (int)origin.Y);
            Color color = MathHelper2.Lerp(colorTable, (float)gameTime.TotalGameTime.TotalMilliseconds % 10000 / 2000);

            env.Sprite.DrawLine(p1, p2, 5, color);
        }

        private static Color[] colorTable = new Color[] { 
            Color.Red,
            Color.Yellow,
            Color.Blue,
            Color.Purple,
            Color.Red};
    }
}

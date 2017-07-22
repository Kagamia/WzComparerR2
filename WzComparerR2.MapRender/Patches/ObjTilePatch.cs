#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender.Patches
{
    public class ObjTilePatch : RenderPatch
    {
        public override void Update(GameTime gameTime, RenderEnv env)
        {
            base.Update(gameTime, env);
            this.RenderArgs.DisplayRectangle = Rectangle.Empty;
        }
    }
}
#endif
#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender.Patches
{
    public class LifePatch : RenderPatch
    {
        public LifePatch()
        {
            actions = new Dictionary<string, RenderAnimate>();
            lifeInfo = new LifeInfo();
        }

        private Dictionary<string, RenderAnimate> actions;
        private int lifeID;
        private int foothold;
        private LifeInfo lifeInfo;

        public int LifeID
        {
            get { return lifeID; }
            set { lifeID = value; }
        }

        public int Foothold
        {
            get { return foothold; }
            set { foothold = value; }
        }

        public LifeInfo LifeInfo
        {
            get { return lifeInfo; }
        }

        public Dictionary<string, RenderAnimate> Actions
        {
            get { return actions; }
        }

        public void SwitchToDefaultAction()
        {
            RenderAnimate action;
            if (!this.actions.TryGetValue("stand", out action)
                && !this.actions.TryGetValue("fly", out action))
            {
                foreach (var kv in this.actions)
                {
                    action = kv.Value;
                    break;
                }
            }
            if (this.Frames != action)
            {
                this.Frames = action;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var kv in this.actions)
                {
                    foreach (RenderFrame frame in kv.Value)
                    {
                        frame.Texture.Dispose();
                    }
                }
            }
        }
    }
}
#endif
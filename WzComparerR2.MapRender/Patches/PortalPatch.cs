#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender.Patches
{
    public class PortalPatch : RenderPatch
    {
        public PortalPatch()
        {
            this.EditMode = false;
        }

        RenderAnimate aniStart;
        RenderAnimate aniContinue;
        RenderAnimate aniExit;
        RenderAnimate aniEditor;
        string pn;
        int pt;
        int tm;
        string tn;
        string script;

        public RenderAnimate AniStart
        {
            get { return aniStart; }
            set { aniStart = value; }
        }

        public RenderAnimate AniContinue
        {
            get { return aniContinue; }
            set { aniContinue = value; }
        }

        public RenderAnimate AniExit
        {
            get { return aniExit; }
            set { aniExit = value; }
        }

        public RenderAnimate AniEditor
        {
            get { return aniEditor; }
            set { aniEditor = value; }
        }

        public string PortalName
        {
            get { return pn; }
            set { pn = value; }
        }

        public int PortalType
        {
            get { return pt; }
            set { pt = value; }
        }

        public int ToMap
        {
            get { return tm; }
            set { tm = value; }
        }

        public string ToName
        {
            get { return tn; }
            set { tn = value; }
        }

        public string Script
        {
            get { return script; }
            set { script = value; }
        }

        public bool EditMode { get; set; }

        public override void Update(GameTime gameTime, RenderEnv env)
        {
            if (this.EditMode)
            {
                this.Frames = this.aniEditor;
            }
            else
            {
                this.Frames = this.aniContinue;
            }

            base.Update(gameTime, env);

            //添加鼠标感应范围

            if (this.EditMode && this.RenderArgs.DisplayRectangle.IsEmpty)
            {
                Point p = new Point((int)this.Position.X, (int)this.Position.Y);
                this.RenderArgs.DisplayRectangle = new Rectangle(p.X - 25, p.Y - 50, 50, 50);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RenderAnimate[] animeLst = { this.aniContinue, this.aniEditor, this.aniExit, this.aniStart };
                foreach (RenderAnimate ani in animeLst)
                {
                    if (ani != null)
                    {
                        foreach (RenderFrame frame in ani)
                        {
                            frame.Texture.Dispose();
                        }
                    }
                }
            }
        }
    }
}
#endif
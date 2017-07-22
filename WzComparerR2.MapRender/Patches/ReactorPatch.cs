#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.MapRender.Patches
{
    public class ReactorPatch : RenderPatch
    {
        public ReactorPatch()
        {
            this.stages = new List<RenderAnimate>();
        }

        int reactorID;
        string reactorName;
        int reactorTime;
        List<RenderAnimate> stages;

        public int ReactorID
        {
            get { return reactorID; }
            set { reactorID = value; }
        }
        
        public string ReactorName
        {
            get { return reactorName; }
            set { reactorName = value; }
        }
        
        public int ReactorTime
        {
            get { return reactorTime; }
            set { reactorTime = value; }
        }

        public List<RenderAnimate> Stages
        {
            get { return stages; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var stage in this.stages)
                {
                    foreach (RenderFrame frame in stage)
                    {
                        frame.Texture.Dispose();
                    }
                }
            }
        }
    }
}
#endif
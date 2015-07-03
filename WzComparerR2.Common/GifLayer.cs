using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Common
{
    public class GifLayer
    {
        public GifLayer()
        {
            this.Frames = new List<GifFrame>();
        }

        public List<GifFrame> Frames { get; private set; }

        public void AddFrame(GifFrame frame)
        {
            this.Frames.Add(frame);
        }

        public void AddBlank(int delay)
        {
            this.Frames.Add(new GifFrame(null, delay));
        }
    }
}

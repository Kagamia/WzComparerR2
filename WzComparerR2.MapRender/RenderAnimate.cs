#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.MapRender
{
    public class RenderAnimate : IEnumerable<RenderFrame>
    {
        public RenderAnimate(RenderFrame[] frames)
        {
            this.frames = frames;
            UpdateTimeLine();
        }

        RenderFrame[] frames;
        int repeat;
        int[] timeLine;

        public RenderFrame this[int index]
        {
            get { return frames[index]; }
        }

        public int Length
        {
            get { return frames == null ? -1 : frames.Length; }
        }

        public int Repeat
        {
            get { return repeat; }
            set { repeat = value; }
        }

        public int[] TimeLine
        {
            get { return timeLine; }
        }

        private void UpdateTimeLine()
        {
            if (this.frames == null || this.frames.Length == 0)
            {
                timeLine = new int[] { 0 };
                return;
            }
            if (this.timeLine == null || (this.timeLine.Length != this.frames.Length + 1))
            {
                this.timeLine = new int[this.frames.Length + 1];
            }
            for (int i = 0; i < this.frames.Length; i++)
            {
                this.timeLine[i + 1] = this.timeLine[i] + this.frames[i].Delay;
            }
        }

        public IEnumerator<RenderFrame> GetEnumerator()
        {
            return new System.Collections.ObjectModel.ReadOnlyCollection<RenderFrame>(this.frames).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
#endif
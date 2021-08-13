using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using WzComparerR2.Controls;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Animation
{
    public class FrameAnimator : AnimationItem
    {
        public FrameAnimator(FrameAnimationData data)
        {
            this.Data = data;
            this.Load();
        }

        public FrameAnimationData Data { get; private set; }

        public Frame CurrentFrame { get; protected set; }

        public override int Length
        {
            get { return this._length; }
        }

        public int CurrentTime
        {
            get { return _timeOffset; }
            protected set { _timeOffset = value; }
        }

        private int _timeOffset;
        private int _length;
        private int[] _timeline;

        public override void Update(TimeSpan elapsedTime)
        {
            if (_length <= 0)
            {
                _timeOffset = 0;
            }
            else
            {
                _timeOffset += (int)elapsedTime.TotalMilliseconds;
                _timeOffset %= _length;
            }
            this.UpdateFrame();
        }

        public override void Reset()
        {
            _timeOffset = 0;
            this.UpdateFrame();
        }

        public override Rectangle Measure()
        {
            return CurrentFrame?.Rectangle ?? Rectangle.Empty;
        }

        public KeyFrame[] GetKeyFrames()
        {
            return this.Data.Frames.Select(f =>
                new KeyFrame() { Length = f.Delay, Animated = f.A0 != f.A1 }
                ).ToArray();
        }

        protected virtual void Load()
        {
            _timeline = CreateTimeline(this.Data.Frames.Select(f => f.Delay));
            _length = _timeline.Last();
            _timeOffset = 0;
            this.UpdateFrame();
        }

        protected virtual void UpdateFrame()
        {
            if (this.Data.Frames.Count <= 0)
            {
                this.CurrentFrame = null;
                return;
            }

            float progress;
            int index = GetProcessFromTimeline(_timeline, _timeOffset, out progress);

            var frame = this.Data.Frames[index];
            if (this.CurrentFrame == null)
            {
                this.CurrentFrame = new Frame();
            }
            this.CurrentFrame.Texture = frame.Texture;
            this.CurrentFrame.AtlasRect = frame.AtlasRect;
            this.CurrentFrame.Z = frame.Z;
            this.CurrentFrame.Origin = frame.Origin;
            this.CurrentFrame.A0 = (int)MathHelper.Lerp(frame.A0, frame.A1, progress);
            this.CurrentFrame.Blend = frame.Blend;
        }

        public override object Clone()
        {
            return new FrameAnimator(this.Data);
        }

        public static int[] CreateTimeline(IEnumerable<int> delays)
        {
            var timeLine = new List<int>() { 0 };
            foreach (var ms in delays)
            {
                timeLine.Add(timeLine[timeLine.Count - 1] + ms);
            }
            return timeLine.ToArray();
        }

        public static int GetProcessFromTimeline(int[] timeline, int timeOffset, out float progress)
        {
            int index = Array.BinarySearch(timeline, timeOffset);
            progress = 0;
            if (index < 0)
            {
                index = ~index - 1;
                progress = (float)(timeOffset - timeline[index]) / (timeline[index + 1] - timeline[index]);
            }
            return index;
        }
    }
}

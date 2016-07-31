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
                new KeyFrame() { Length = f.Delay, Animated = f.A0 == f.A1 }
                ).ToArray();
        }

        protected virtual void Load()
        {
            _timeline = new int[this.Data.Frames.Count + 1];
            for (int i = 0; i < this.Data.Frames.Count; i++)
            {
                _timeline[i + 1] = _timeline[i] + this.Data.Frames[i].Delay;
            }
            _length = _timeline[_timeline.Length - 1];
            _timeOffset = 0;
            this.UpdateFrame();
        }

        protected virtual void UpdateFrame()
        {
            float progress;
            int index = GetProcessFromTimeline(_timeline, _timeOffset, out progress);

            var frame = this.Data.Frames[index];
            this.CurrentFrame = new Frame(frame.Texture, frame.AtlasRect)
            {
                Z = frame.Z,
                Origin = frame.Origin,
                A0 = (int)MathHelper.Lerp(frame.A0, frame.A1, progress),
            };
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

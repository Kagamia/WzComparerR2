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
            this._drawOrder = new List<Frame>();

            this.Load();
        }

        public FrameAnimationData Data { get; private set; }

        public Frame CurrentFrame { get; private set; }

        public override int Length
        {
            get { return this._length; }
        }

        public int CurrentTime
        {
            get { return _timeOffset; }
        }

        private List<Frame> _drawOrder;

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

        private void Load()
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

        private void UpdateFrame()
        {
            int index = Array.BinarySearch(_timeline, _timeOffset);
            float progress = 0;
            if (index < 0)
            {
                index = ~index - 1;
                progress = (float)(_timeOffset - _timeline[index]) / (_timeline[index + 1] - _timeline[index]);
            }

            var frame = this.Data.Frames[index];
            this.CurrentFrame = new Frame(frame.Texture)
            {
                Origin = frame.Origin,
                A0 = (int)MathHelper.Lerp(frame.A0, frame.A1, progress),
            };
        }
    }
}

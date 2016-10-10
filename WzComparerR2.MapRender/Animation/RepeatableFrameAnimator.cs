using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Animation
{
    public class RepeatableFrameAnimator : FrameAnimator
    {
        public RepeatableFrameAnimator(RepeatableFrameAnimationData data)
            : base(data)
        {
            this.Data = data;
        }

        public new RepeatableFrameAnimationData Data { get; private set; }

        public bool IsRepeating { get; private set; }

        public int RepeatLength { get; private set; }

        private int[] repeatTimeLine;

        public override void Update(TimeSpan elapsedTime)
        {
            int _timeOffset = base.CurrentTime;
            if (this.Length <= 0)
            {
                _timeOffset = 0;
            }
            else
            {
                _timeOffset += (int)elapsedTime.TotalMilliseconds;
                if (!this.IsRepeating)
                {
                    if (_timeOffset >= this.Length)
                    {
                        _timeOffset -= this.Length;
                        this.IsRepeating = true;
                        _timeOffset %= this.RepeatLength;
                    }
                }
                else
                {
                    _timeOffset %= this.RepeatLength;
                }
            }

            base.CurrentTime = _timeOffset;
            this.UpdateFrame();
        }

        public override void Reset()
        {
            base.Reset();
            IsRepeating = false;
        }

        protected override void Load()
        {
            if (this.Data.Repeat > 0)
            {
                this.repeatTimeLine = CreateTimeline(this.Data.Frames.Skip(Data.Repeat).Select(f => f.Delay));
                this.RepeatLength = repeatTimeLine.Last();
            }

            base.Load();
        }

        protected override void UpdateFrame()
        {
            if (IsRepeating && this.Data.Repeat > 0)
            {
                float progress;
                int index = GetProcessFromTimeline(repeatTimeLine, this.CurrentTime, out progress);

                var frame = this.Data.Frames[index + this.Data.Repeat];
                this.CurrentFrame = new Frame(frame.Texture, frame.AtlasRect)
                {
                    Z = frame.Z,
                    Origin = frame.Origin,
                    A0 = (int)MathHelper.Lerp(frame.A0, frame.A1, progress),
                };
            }
            else
            {
                base.UpdateFrame();
            }
        }
    }
}

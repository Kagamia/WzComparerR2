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
            this.IsLoop = this.Data.Repeat ?? true;
        }

        public new RepeatableFrameAnimationData Data { get; private set; }

        public bool IsStopped { get; private set; }
        public bool IsLoop { get; set; }

        public override void Update(TimeSpan elapsedTime)
        {
            if (!IsLoop)
            {
                int _timeOffset = base.CurrentTime;
                if (this.Length <= 0)
                {
                    _timeOffset = 0;
                }
                else
                {
                    _timeOffset += (int)elapsedTime.TotalMilliseconds;
                    if (!this.IsStopped)
                    {
                        if (_timeOffset >= this.Length)
                        {
                            _timeOffset = this.Length;
                            this.IsStopped = true;
                        }
                    }
                    else
                    {
                        _timeOffset = this.Length;
                    }
                }

                base.CurrentTime = _timeOffset;
                this.UpdateFrame();
            }
            else
            {
                base.Update(elapsedTime);
            }
        }

        public override void Reset()
        {
            base.Reset();
            IsStopped = false;
        }

        protected override void Load()
        {
            base.Load();
        }

        protected override void UpdateFrame()
        {
            if (!IsLoop && this.IsStopped)
            {
                var frame = this.Data.Frames.Last();
                if (this.CurrentFrame == null)
                {
                    this.CurrentFrame = new Frame();
                }
                this.CurrentFrame.Texture = frame.Texture;
                this.CurrentFrame.AtlasRect = frame.AtlasRect;
                this.CurrentFrame.Z = frame.Z;
                this.CurrentFrame.Origin = frame.Origin;
                this.CurrentFrame.A0 = frame.A1;
                this.CurrentFrame.Blend = frame.Blend;
            }
            else
            {
                base.UpdateFrame();
            }
        }
    }
}

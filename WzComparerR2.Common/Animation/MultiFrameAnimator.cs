using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using WzComparerR2.Controls;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Animation
{
    public class MultiFrameAnimator : AnimationItem
    {
        public MultiFrameAnimator(MultiFrameAnimationData data)
        {
            this.Data = data;
            this._selectedAniIndex = 0;
            this.Load();
        }

        public MultiFrameAnimationData Data { get; private set; }

        public ReadOnlyCollection<string> Animations { get; private set; }

        public Frame CurrentFrame { get; protected set; }

        public int SelectedAnimationIndex
        {
            get
            {
                return this._selectedAniIndex;
            }
            set
            {
                if (value > -1)
                {
                    this._selectedAniIndex = value;
                }
                else
                {
                    this._selectedAniIndex = -1;
                }

                this._timeOffset = 0;
            }
        }

        public string SelectedAnimationName
        {
            get
            {
                if (this._selectedAniIndex > -1)
                {
                    return this.Animations[this._selectedAniIndex];
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    this.SelectedAnimationIndex = this.Animations.IndexOf(value);
                }
                else
                {
                    this.SelectedAnimationIndex = -1;
                }
            }
        }

        public override int Length
        {
            get { return SelectedAnimationName != null ? this._length : 0; }
        }

        public int CurrentTime
        {
            get { return _timeOffset; }
            protected set { _timeOffset = value; }
        }

        private int _timeOffset;
        private int _length;
        private int[] _timeline;
        private int _selectedAniIndex;

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
            return this.Data.Frames[SelectedAnimationName].Select(f =>
                new KeyFrame() { Length = f.Delay, Animated = f.A0 == f.A1 }
                ).ToArray();
        }

        protected virtual void Load()
        {
            IList<string> aniNames = this.Data.Frames.Keys.ToList();
            this.Animations = new ReadOnlyCollection<string>(aniNames);

            if (this.Animations.Count > 0)
            {
                this.SelectedAnimationIndex = 0;
            }
            else
            {
                this.SelectedAnimationIndex = -1;
            }

            _timeline = CreateTimeline(this.Data.Frames[SelectedAnimationName].Select(f => f.Delay));
            _length = _timeline.Last();
            _timeOffset = 0;
            this.UpdateFrame();
        }

        protected virtual void UpdateFrame()
        {
            if (SelectedAnimationName == null || this.Data.Frames[SelectedAnimationName].Count <= 0)
            {
                this.CurrentFrame = null;
                return;
            }

            float progress;
            int index = GetProcessFromTimeline(_timeline, _timeOffset, out progress);

            var frame = this.Data.Frames[SelectedAnimationName][index];
            this.CurrentFrame = new Frame(frame.Texture, frame.AtlasRect)
            {
                Z = frame.Z,
                Origin = frame.Origin,
                A0 = (int)MathHelper.Lerp(frame.A0, frame.A1, progress),
            };
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

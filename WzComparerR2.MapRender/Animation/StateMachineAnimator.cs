using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Animation
{
    public class StateMachineAnimator
    {
        public StateMachineAnimator(IDictionary<string, RepeatableFrameAnimationData> data)
            : this(new FrameStateMachineData(data))
        {
        }

        public StateMachineAnimator(ISpineAnimationData data)
        {
            throw new NotImplementedException();
        }

        private StateMachineAnimator(IStateMachineAnimationData data)
        {
            this.Data = data;
            this.Data.AnimationEnd += Data_AnimationEnd;
        }

        public IStateMachineAnimationData Data { get; private set; }

        public event EventHandler<AnimationEndEventArgs> AnimationEnd;


        public void SetAnimation(string aniName)
        {
            int idx = this.Data.States.IndexOf(aniName);
            this.Data.SelectedStateIndex = idx;
        }

        public string GetCurrent()
        {
            return this.Data.SelectedState;
        }

        public void Update(TimeSpan elapsedTime)
        {
            this.Data.Update(elapsedTime);
        }

        protected virtual void OnAnimationEnd(AnimationEndEventArgs e)
        {
            this.AnimationEnd?.Invoke(this, e);

            if (!e.IsHandled)
            {
                if (e.CurrentState != e.NextState)
                {
                    this.SetAnimation(e.NextState);
                }
            }
        }

        private void Data_AnimationEnd(object sender, EventArgs e)
        {
            string state = GetCurrent();
            var ev = new AnimationEndEventArgs(state);
            ev.NextState = state;
            this.OnAnimationEnd(ev);
        }

        public class AnimationEndEventArgs : EventArgs
        {
            public AnimationEndEventArgs(string currentState)
            {
                this.CurrentState = currentState;
            }

            public bool IsHandled { get; set; }
            public string CurrentState { get; }
            public string NextState { get; set; }
        }

        /// <summary>
        /// 用于帧动画的状态机数据。
        /// </summary>
        private class FrameStateMachineData : IStateMachineAnimationData
        {
            public FrameStateMachineData(IDictionary<string, RepeatableFrameAnimationData> data)
            {
                this.data = new Dictionary<string, RepeatableFrameAnimationData>(data);
                this.States = new ReadOnlyCollection<string>(new List<string>(this.data.Keys));
                this.SelectedStateIndex = -1;
            }

            private IDictionary<string, RepeatableFrameAnimationData> data;
            private int selectedIndex;
            private RepeatableFrameAnimator selectedData;

            public ReadOnlyCollection<string> States { get; private set; }

            public int SelectedStateIndex
            {
                get { return this.selectedIndex; }
                set
                {
                    if (value < 0 || value >= this.States.Count)
                    {
                        this.selectedIndex = -1;
                        this.selectedData = null;
                    }
                    else
                    {
                        this.selectedIndex = value;
                        this.selectedData = new RepeatableFrameAnimator(this.data[SelectedState]);
                    }
                }
            }

            public string SelectedState
            {
                get { return this.selectedIndex < 0 ? null : this.States[selectedIndex]; }
            }

            public event EventHandler AnimationEnd;

            protected virtual void OnAnimationEnd(EventArgs e)
            {
                this.AnimationEnd?.Invoke(this, e);
            }

            public void Update(TimeSpan elapsedTime)
            {
                if (this.selectedData == null)
                    return;
                if (this.selectedData.CurrentTime + elapsedTime.TotalMilliseconds >= selectedData.Length)
                {
                    this.selectedData.Update(elapsedTime);
                    //this.Update(TimeSpan.FromMilliseconds(selectedData.Length - selectedData.CurrentTime));
                    OnAnimationEnd(EventArgs.Empty);
                }
                else
                {
                    this.selectedData.Update(elapsedTime);
                }
            }

            public object GetMesh()
            {
                return this.selectedData?.CurrentFrame;
            }
        }

        /// <summary>
        /// 用于骨骼动画的状态机数据。
        /// </summary>
        private class SpineStateMachineData : IStateMachineAnimationData
        {
            public string SelectedState
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public int SelectedStateIndex
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public ReadOnlyCollection<string> States
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public event EventHandler AnimationEnd;

            public object GetMesh()
            {
                throw new NotImplementedException();
            }

            public void Update(TimeSpan deltaTime)
            {
                throw new NotImplementedException();
            }
        }
    }
}

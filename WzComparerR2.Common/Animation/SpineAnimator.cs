using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Spine;
using WzComparerR2.WzLib;
using WzComparerR2.Controls;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Animation
{
    public class SpineAnimator : AnimationItem
    {
        public SpineAnimator(SpineAnimationData data)
        {
            this.Data = data;
            this._selectedAniIndex = -1;
            this.Load();
        }

        public SpineAnimationData Data { get; private set; }

        public Skeleton Skeleton { get; private set; }

        public ReadOnlyCollection<string> Animations { get; private set; }

        public ReadOnlyCollection<string> Skins { get; private set; }

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
                    string aniName = this.Animations[value];
                    var ani = this.Data.SkeletonData.FindAnimation(aniName);
                    this._animationState.SetAnimation(0, aniName, true);
                    this._selectedAniIndex = value;
                }
                else
                {
                    this._animationState.ClearTracks();
                    this._selectedAniIndex = -1;
                }
                
                this.Skeleton.SetToSetupPose();
                this._animationState.Apply(this.Skeleton);
                this.Skeleton.UpdateWorldTransform();
            }
        }

        public string SelectedAnimationName
        {
            get
            {
                if( this._selectedAniIndex > -1)
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

        public string SelectedSkin
        {
            get { return this.Skeleton.Skin?.Name; }
            set { this.Skeleton.SetSkin(value); }
        }

        public int CurrentTime
        {
            get { return (int)((this._animationState?.GetCurrent(0)?.Time ?? 0) * 1000); }
        }

        internal Spine.Animation SelectedAnimation
        {
            get
            {
                if (this._selectedAniIndex > -1)
                {
                    return this.Data.SkeletonData.FindAnimation(SelectedAnimationName);
                }
                return null;
            }
        }

        public override void Reset()
        {
            this.SelectedAnimationIndex = this.SelectedAnimationIndex;
        }

        public override int Length
        {
            get
            {
                return (int)((this.SelectedAnimation?.Duration ?? 0f) * 1000);
            }
        }

        private int _selectedAniIndex;
        private AnimationState _animationState;

        public override void Update(TimeSpan elapsedTime)
        {
            this._animationState.Update((float)elapsedTime.TotalSeconds);
            this._animationState.Apply(Skeleton);
            this.Skeleton.UpdateWorldTransform();
        }

        public override Rectangle Measure()
        {
            ModelBound bound = ModelBound.Empty;
            UpdateBounds(ref bound, this.Skeleton);
            return bound.GetBound();
        }

        private void UpdateBounds(ref ModelBound bound, Skeleton skeleton)
        {
            float[] vertices = new float[8];
            var drawOrder = skeleton.DrawOrder;
            for (int i = 0, n = drawOrder.Count; i < n; i++)
            {
                Slot slot = drawOrder.Items[i];
                Attachment attachment = slot.Attachment;
                if (attachment is RegionAttachment)
                {
                    RegionAttachment region = (RegionAttachment)attachment;
                    region.ComputeWorldVertices(slot.Bone, vertices);
                    bound.Update(vertices, 8);
                }
                else if (attachment is MeshAttachment)
                {
                    MeshAttachment mesh = (MeshAttachment)attachment;
                    int vertexCount = mesh.Vertices.Length;
                    if (vertices.Length < vertexCount) vertices = new float[vertexCount];
                    mesh.ComputeWorldVertices(slot, vertices);
                    bound.Update(vertices, vertexCount);
                }
                else if (attachment is SkinnedMeshAttachment)
                {
                    SkinnedMeshAttachment mesh = (SkinnedMeshAttachment)attachment;
                    int vertexCount = mesh.UVs.Length;
                    if (vertices.Length < vertexCount) vertices = new float[vertexCount];
                    mesh.ComputeWorldVertices(slot, vertices);
                    bound.Update(vertices, vertexCount);
                }
            }
        }

        public KeyFrame[] GetKeyFrames()
        {
            //放弃了 算法太麻烦还不如直接对比。。
            return null;
            var frames = new LinkedList<KeyFrame>();
            var track = this._animationState.GetCurrent(0);
            if (track != null)
            {
                foreach (var timeLine in track.Animation.Timelines)
                {
                    var tlFrames = GetTimeLineKeyFrames(timeLine);
                    if (tlFrames.Count > 0)
                    {
                        if (frames.Count <= 0) //直接加入
                        {
                            foreach(var frame in tlFrames)
                            {
                                frames.AddLast(frame);
                            }
                        }
                        else //合并关键帧
                        {

                        }
                    }
                }
            }
            return frames.ToArray();
        }

        private LinkedList<KeyFrame> GetTimeLineKeyFrames(Timeline timeLine)
        {
            float[] frameTimes;
            int interval = 0;
            bool animated = false;
            try
            {
                //懒得反射了。。。
                dynamic m = timeLine;
                frameTimes = (float[])((dynamic)m).Frames;
            }
            catch
            {
                frameTimes = null;
            }

            if (timeLine is AttachmentTimeline)
            {
                interval = 1;
                animated = false;
            }
            else if (timeLine is CurveTimeline)
            {
                animated = true;

                if (timeLine is ColorTimeline)
                {
                    interval = 5;
                }
                else if (timeLine is FFDTimeline)
                {
                    interval = 1;
                }
                else if (timeLine is IkConstraintTimeline)
                {
                    interval = 3;
                }
                else if (timeLine is RotateTimeline)
                {
                    interval = 2;
                }
                else if (timeLine is TranslateTimeline)
                {
                    interval = 3;
                }
            }
            else if (timeLine is DrawOrderTimeline)
            {
                interval = 1;
                animated = false;
            }
            else if (timeLine is EventTimeline)
            {
                //对模型好像没啥变化 忽略
            }
            else if (timeLine is FlipXTimeline)
            {
                interval = 2;
                animated = false;
            }

            var frameList = new LinkedList<KeyFrame>();

            if (frameTimes != null && frameTimes.Length > 0 && interval > 0)
            {
                if (frameTimes[0] > 0)
                {
                    frameList.AddFirst(new KeyFrame() { Length = (int)(frameTimes[0] * 1000), Animated = false });
                }

                for (int i = 0; i < frameTimes.Length; i += interval)
                {
                    float length = i < frameTimes.Length - interval ? (frameTimes[i + interval] - frameTimes[i]) : 0;
                    frameList.AddLast(new KeyFrame() { Length = (int)(length * 1000), Animated = animated });
                }
            }

            return frameList;
        }

        public override object Clone()
        {
            var clonedAnimator = new SpineAnimator(this.Data);
            clonedAnimator.SelectedAnimationIndex = this.SelectedAnimationIndex;
            if (this.SelectedSkin != null)
            {
                clonedAnimator.SelectedSkin = this.SelectedSkin;
            }
            return clonedAnimator;
        }

        private void Load()
        {
            this.Skeleton = new Skeleton(this.Data.SkeletonData);
            IList<string> aniNames = this.Data.SkeletonData.Animations.Select(ani => ani.Name).ToList();
            this.Animations = new ReadOnlyCollection<string>(aniNames);
            this._animationState = new AnimationState(new AnimationStateData(this.Data.SkeletonData));
            this.Skins = new ReadOnlyCollection<string>(this.Data.SkeletonData.Skins.Select(skin => skin.Name).ToList());

            if (!string.IsNullOrEmpty(this.Data.DefaultSkin))
            {
                var skin = this.Skeleton.Data.FindSkin(this.Data.DefaultSkin);
                if (skin != null)
                {
                    this.Skeleton.SetSkin(skin);
                }
            }

            if (this.Animations.Count > 0)
            {
                this.SelectedAnimationIndex = 0;
            }
            else
            {
                this.SelectedAnimationIndex = -1;
            }
        }
    }
}

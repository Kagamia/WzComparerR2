using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WzComparerR2.Controls;

using Microsoft.Xna.Framework;
using Spine;

namespace WzComparerR2.Animation
{
    public class SpineAnimatorV4 : AnimationItem, ISpineAnimator
    {
        public SpineAnimatorV4(SpineAnimationDataV4 data)
        {
            this.Data = data;
            this._selectedAniIndex = -1;
            this.Load();
        }

        public SpineAnimationDataV4 Data { get; private set; }

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
                    this._animationState.SetAnimation(0, ani, true);
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

        public string SelectedSkin
        {
            get { return this.Skeleton.Skin?.Name; }
            set { this.Skeleton.SetSkin(value); }
        }

        public int CurrentTime
        {
            get { return (int)((this._animationState?.GetCurrent(0)?.TrackTime ?? 0) * 1000); }
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
                    region.ComputeWorldVertices(slot, vertices, 0);
                    bound.Update(vertices, 8);
                }
                else if (attachment is MeshAttachment)
                {
                    MeshAttachment mesh = (MeshAttachment)attachment;
                    int vertexCount = mesh.WorldVerticesLength;
                    if (vertices.Length < vertexCount) vertices = new float[vertexCount];
                    mesh.ComputeWorldVertices(slot, vertices);
                    bound.Update(vertices, vertexCount);
                }
                else if (attachment is ClippingAttachment)
                {
                    // ignore, don't know how it works
                }
            }
        }

        public override object Clone()
        {
            var clonedAnimator = new SpineAnimatorV4(this.Data);
            clonedAnimator.SelectedAnimationIndex = this.SelectedAnimationIndex;
            if (this.SelectedSkin != null)
            {
                clonedAnimator.SelectedSkin = this.SelectedSkin;
            }
            return clonedAnimator;
        }

        private void Load()
        {
            var skeletonData = this.Data.SkeletonData;
            this.Skeleton = new Skeleton(skeletonData);
            this.Animations = new ReadOnlyCollection<string>(skeletonData.Animations.Select(ani => ani.Name).ToList());
            this.Skins = new ReadOnlyCollection<string>(skeletonData.Skins.Select(skin => skin.Name).ToList());
            this._animationState = new AnimationState(new AnimationStateData(skeletonData));

            if (this.Animations.Count > 0)
            {
                this.SelectedAnimationIndex = 0;
            }
            else
            {
                this.SelectedAnimationIndex = -1;
            }
        }

        #region ISpineAnimator
        ISpineAnimationData ISpineAnimator.Data => this.Data;
        object ISpineAnimator.Skeleton => this.Skeleton;
        ReadOnlyCollection<string> ISpineAnimator.Animations => this.Animations;
        ReadOnlyCollection<string> ISpineAnimator.Skins => this.Skins;
        int ISpineAnimator.SelectedAnimationIndex { get => this.SelectedAnimationIndex; set => this.SelectedAnimationIndex = value; }
        string ISpineAnimator.SelectedAnimationName { get => this.SelectedAnimationName; set => this.SelectedAnimationName = value; }
        string ISpineAnimator.SelectedSkin { get => this.SelectedSkin; set => this.SelectedSkin = value; }
        int ISpineAnimator.CurrentTime { get => this.CurrentTime; }
        void ISpineAnimator.Render(Spine.SkeletonRenderer renderer) => renderer.Draw(this.Skeleton);
        #endregion
    }
}

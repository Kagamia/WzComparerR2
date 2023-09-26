using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WzComparerR2.Animation
{
    public interface ISpineAnimator
    {
        ISpineAnimationData Data { get; }
        object Skeleton { get; }
        ReadOnlyCollection<string> Animations { get; }
        ReadOnlyCollection<string> Skins { get; }
        int SelectedAnimationIndex { get; set; }
        string SelectedAnimationName { get; set; }
        string SelectedSkin { get; set; }
        int CurrentTime { get; }
        void Render(Spine.SkeletonRenderer renderer);
    }
}

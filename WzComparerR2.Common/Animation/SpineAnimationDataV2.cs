using System;
using WzComparerR2.Common;
using WzComparerR2.WzLib;

using Microsoft.Xna.Framework.Graphics;
using Spine.V2;

namespace WzComparerR2.Animation
{
    public class SpineAnimationDataV2 : ISpineAnimationData
    {
        private SpineAnimationDataV2()
        {
        }

        public bool PremultipliedAlpha { get; set; }
        public SkeletonData SkeletonData { get; private set; }

        public static SpineAnimationDataV2 CreateFromNode(Wz_Node atlasOrSkelNode, GraphicsDevice graphicsDevice, GlobalFindNodeFunction findNode)
        {
            var textureLoader = new WzSpineTextureLoader(atlasOrSkelNode.ParentNode, graphicsDevice, findNode);
            return CreateFromNode(atlasOrSkelNode, textureLoader);
        }

        public static SpineAnimationDataV2 CreateFromNode(Wz_Node atlasOrSkelNode, TextureLoader textureLoader)
        {
            return Create(SpineLoader.Detect(atlasOrSkelNode), textureLoader);
        }

        public static SpineAnimationDataV2 Create(SpineDetectionResult detectionResult, TextureLoader textureLoader)
        {
            var skeletonData = SpineLoader.LoadSkeletonV2(detectionResult, textureLoader);

            if (skeletonData == null)
            {
                return null;
            }

            bool pma = detectionResult.SourceNode.ParentNode.FindNodeByPath("PMA").GetValueEx<int>(0) != 0;

            var anime = new SpineAnimationDataV2();
            anime.SkeletonData = skeletonData;
            anime.PremultipliedAlpha = pma;
            return anime;
        }

        #region ISpineAnimationData
        bool ISpineAnimationData.PremultipliedAlpha => this.PremultipliedAlpha;
        object ISpineAnimationData.SkeletonData => this.SkeletonData;
        SpineVersion ISpineAnimationData.SpineVersion => SpineVersion.V2;
        ISpineAnimator ISpineAnimationData.CreateAnimator() => new SpineAnimatorV2(this);
        #endregion
    }
}

using System;
using WzComparerR2.Common;
using WzComparerR2.WzLib;

using Microsoft.Xna.Framework.Graphics;
using Spine;

namespace WzComparerR2.Animation
{
    public class SpineAnimationDataV4 : ISpineAnimationData
    {
        private SpineAnimationDataV4()
        {
        }

        public bool PremultipliedAlpha { get; set; }
        public SkeletonData SkeletonData { get; private set; }

        public static SpineAnimationDataV4 CreateFromNode(Wz_Node atlasOrSkelNode, GraphicsDevice graphicsDevice, GlobalFindNodeFunction findNode)
        {
            var textureLoader = new WzSpineTextureLoader(atlasOrSkelNode.ParentNode, graphicsDevice, findNode);
            return CreateFromNode(atlasOrSkelNode, textureLoader);
        }

        public static SpineAnimationDataV4 CreateFromNode(Wz_Node atlasOrSkelNode, TextureLoader textureLoader)
        {
            return Create(SpineLoader.Detect(atlasOrSkelNode), textureLoader);
        }

        public static SpineAnimationDataV4 Create(SpineDetectionResult detectionResult, TextureLoader textureLoader)
        {
            var skeletonData = SpineLoader.LoadSkeletonV4(detectionResult, textureLoader);

            if (skeletonData == null)
            {
                return null;
            }

            bool pma = detectionResult.SourceNode.ParentNode.FindNodeByPath("PMA").GetValueEx<int>(0) != 0;

            var anime = new SpineAnimationDataV4();
            anime.SkeletonData = skeletonData;
            anime.PremultipliedAlpha = pma;
            return anime;
        }

        #region ISpineAnimationData
        bool ISpineAnimationData.PremultipliedAlpha => this.PremultipliedAlpha;
        object ISpineAnimationData.SkeletonData => this.SkeletonData;
        SpineVersion ISpineAnimationData.SpineVersion => SpineVersion.V4;
        ISpineAnimator ISpineAnimationData.CreateAnimator() => new SpineAnimatorV4(this);
        #endregion
    }
}

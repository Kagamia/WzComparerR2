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

        public static SpineAnimationDataV4 CreateFromNode(Wz_Node atlasNode, GraphicsDevice graphicsDevice, GlobalFindNodeFunction findNode)
        {
            var textureLoader = new WzSpineTextureLoader(atlasNode.ParentNode, graphicsDevice, findNode);
            return CreateFromNode(atlasNode, textureLoader);
        }

        public static SpineAnimationDataV4 CreateFromNode(Wz_Node atlasNode, TextureLoader textureLoader)
        {
            var skeletonData = SpineLoader.LoadSkeletonV4(atlasNode, textureLoader);

            if (skeletonData == null)
            {
                return null;
            }

            bool pma = atlasNode.ParentNode.FindNodeByPath("PMA").GetValueEx<int>(0) != 0;

            var anime = new SpineAnimationDataV4();
            anime.SkeletonData = skeletonData;
            anime.PremultipliedAlpha = pma;
            return anime;
        }

        #region ISpineAnimationData
        bool ISpineAnimationData.PremultipliedAlpha => this.PremultipliedAlpha;
        object ISpineAnimationData.SkeletonData => this.SkeletonData;
        SpineVersion ISpineAnimationData.SpineVersion => SpineVersion.V4;
        #endregion
    }
}

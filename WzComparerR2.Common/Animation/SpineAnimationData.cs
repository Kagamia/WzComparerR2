using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spine;
using WzComparerR2.Common;
using WzComparerR2.WzLib;
using WzComparerR2.Rendering;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Animation
{
    public class SpineAnimationData
    {
        private SpineAnimationData()
        {

        }

        public bool PremultipliedAlpha { get; set; }
        public SkeletonData SkeletonData { get; private set; }

        public static SpineAnimationData CreateFromNode(Wz_Node atlasNode, bool? useJson, GraphicsDevice graphicsDevice, GlobalFindNodeFunction findNode)
        {
            var parentNode = atlasNode.ParentNode;

            var loadType = SkeletonLoadType.Auto;
            if (useJson != null)
            {
                loadType = useJson.Value ? SkeletonLoadType.Json : SkeletonLoadType.Binary;
            }

            var textureLoader = new WzSpineTextureLoader(parentNode, graphicsDevice, findNode);
            var skeletonData = SpineLoader.LoadSkeleton(atlasNode, loadType, textureLoader);

            if (skeletonData == null)
            {
                return null;
            }

            bool pma = parentNode.FindNodeByPath("PMA").GetValueEx<int>(0) != 0;

            var anime = new SpineAnimationData();
            anime.SkeletonData = skeletonData;
            anime.PremultipliedAlpha = pma;
            return anime;
        }
    }
}

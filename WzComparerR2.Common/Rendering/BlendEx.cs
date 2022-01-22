using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Rendering
{
    public static class StateEx
    {

        public static BlendState NonPremultipled_Hidef() => new BlendState()
        {
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            AlphaBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Add,
        };

        public static BlendState SrcAlphaMask() => new BlendState()
        {
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            AlphaBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Add,
        };

        public static RasterizerState Scissor() => new RasterizerState()
        {
            ScissorTestEnable = true,
            CullMode = CullMode.None,
            MultiSampleAntiAlias = false,
            FillMode = FillMode.Solid
        };
    }
}

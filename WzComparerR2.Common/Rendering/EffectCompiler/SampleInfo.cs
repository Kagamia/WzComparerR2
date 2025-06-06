using System;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Rendering.EffectCompiler
{
    public class SamplerInfo
    {
        public string Name { get; set; }
        public string TextureName { get; set; }
        public SamplerType Type { get; set; }
        public int TextureSlot { get; set; }
        public int SamplerSlot { get; set; }
        public SamplerState State { get; set; }
    }

    public enum SamplerType
    {
        Sampler2D,
        SamplerCube,
        SamplerVolume,
        Sampler1D
    }
}

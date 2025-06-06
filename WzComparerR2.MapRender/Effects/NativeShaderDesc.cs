using System;
using System.Collections.Generic;
using System.Linq;
using WzComparerR2.Rendering.EffectCompiler;

namespace WzComparerR2.MapRender.Effects
{
    public class NativeShaderDesc
    {
        public string Name { get; set; }
        public string OriginFileName { get; set; }
        public int FileLength { get; set; }
        public string Version { get; set; }
        public ShaderStage Stage { get; set; }
        public List<ConstantBuffer> ConstantBuffers { get; set; }
        public List<SamplerInfo> Samplers { get; set; }
    }
}

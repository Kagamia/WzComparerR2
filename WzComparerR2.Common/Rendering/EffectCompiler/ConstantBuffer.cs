using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.Rendering.EffectCompiler
{
    public class ConstantBuffer
    {
        public string Name { get; set; }
        public int Slot { get; set; }
        public int SizeInBytes { get; set; }
        public List<ShaderParameter> Parameters { get; set; } = new();
    }

    public class ShaderParameter
    {
        public ShaderParameter() 
        { 
        }

        public ShaderParameter(string name, string semantic, int bufferOffset, EffectParameterClass parameterClass, EffectParameterType parameterType, int rowCount, int columnCount, int elementCount)
        {
            Name = name;
            Semantic = semantic;
            BufferOffset = bufferOffset;
            ParameterClass = parameterClass;
            ParameterType = parameterType;
            RowCount = rowCount;
            ColumnCount = columnCount;
            ElementCount = elementCount;
        }

        public string Name { get; set; }
        public string Semantic { get; set; }
        public int BufferOffset { get; set; }
        public EffectParameterClass ParameterClass { get; set; }
        public EffectParameterType ParameterType { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public int ElementCount { get; set; }
    }
}

// Port from https://github.com/MonoGame/MonoGame/blob/develop/Tools/MonoGame.Effect.Compiler/Effect/ConstantBufferData.cs

using System;
using System.Collections.Generic;
using System.IO;

namespace WzComparerR2.Rendering.EffectCompiler.Internal
{
    public class ConstantBufferData
    {
        public string Name;

        public int Size;

        public List<int> ParameterIndex = new List<int>();

        public List<int> ParameterOffset = new List<int>();

        public List<d3dx_parameter> Parameters = new List<d3dx_parameter>();

        public void Write(BinaryWriter writer)
        {
            writer.Write(Name);

            writer.Write((ushort)Size);

            writer.MgfxWriteElementCount(ParameterIndex.Count);
            for (var i = 0; i < ParameterIndex.Count; i++)
            {
                writer.MgfxWriteElementCount(ParameterIndex[i]);
                writer.Write((ushort)ParameterOffset[i]);
            }
        }
    }

}

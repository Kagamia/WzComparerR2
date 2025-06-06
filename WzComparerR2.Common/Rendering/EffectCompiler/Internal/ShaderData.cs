// Port from https://github.com/MonoGame/MonoGame/blob/develop/Tools/MonoGame.Effect.Compiler/Effect/ShaderData.cs

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Rendering.EffectCompiler.Internal
{
    public class ShaderData
    {
        public int[] _cbuffers;

        public Sampler[] _samplers;

        public Attribute[] _attributes;

        public byte[] ShaderCode;

        public bool IsVertexShader;

        public struct Sampler
        {
            public SamplerType type;
            public int textureSlot;
            public int samplerSlot;
            public string samplerName;
            public string parameterName;
            public int parameter;
            public SamplerState state;
        }

        public struct Attribute
        {
            public string name;
            public VertexElementUsage usage;
            public int index;
            public int location;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(IsVertexShader);

            writer.Write(ShaderCode.Length);
            writer.Write(ShaderCode);

            writer.Write((byte)_samplers.Length);
            foreach (var sampler in _samplers)
            {
                writer.Write((byte)sampler.type);
                writer.Write((byte)sampler.textureSlot);
                writer.Write((byte)sampler.samplerSlot);

                if (sampler.state != null)
                {
                    writer.Write(true);
                    writer.Write((byte)sampler.state.AddressU);
                    writer.Write((byte)sampler.state.AddressV);
                    writer.Write((byte)sampler.state.AddressW);
                    writer.Write(sampler.state.BorderColor.R);
                    writer.Write(sampler.state.BorderColor.G);
                    writer.Write(sampler.state.BorderColor.B);
                    writer.Write(sampler.state.BorderColor.A);
                    writer.Write((byte)sampler.state.Filter);
                    writer.Write(sampler.state.MaxAnisotropy);
                    writer.Write(sampler.state.MaxMipLevel);
                    writer.Write(sampler.state.MipMapLevelOfDetailBias);
                }
                else
                    writer.Write(false);

                writer.Write(sampler.samplerName);

                writer.Write((byte)sampler.parameter);
            }

            writer.Write((byte)_cbuffers.Length);
            foreach (var cb in _cbuffers)
                writer.Write((byte)cb);

            writer.Write((byte)_attributes.Length);
            foreach (var attrib in _attributes)
            {
                writer.Write(attrib.name);
                writer.Write((byte)attrib.usage);
                writer.Write((byte)attrib.index);
                writer.Write((short)attrib.location);
            }
        }
    }
}

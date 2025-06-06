using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Rendering.EffectCompiler.Internal;

namespace WzComparerR2.Rendering.EffectCompiler
{
    public static class ShaderConverter
    {
        public static byte[] D3DShaderByteCodeToMgfx(ReadOnlySpan<byte> shaderByteCode, ShaderStage shaderStage, IReadOnlyList<ConstantBuffer> constantBuffers, IReadOnlyList<SamplerInfo> samplers)
        {
            var cbuffers = new List<ConstantBufferData>();

            if (constantBuffers != null)
            {
                foreach (var constantBuffer in constantBuffers)
                {
                    while (cbuffers.Count <= constantBuffer.Slot)
                    {
                        cbuffers.Add(null);
                    }

                    cbuffers[constantBuffer.Slot] = ConvertConstantBuffer(constantBuffer);
                }
            }

            // fill default constant buffer
            for (int i = 0; i < cbuffers.Count; i++)
            {
                if (cbuffers[i] == null)
                {
                    cbuffers[i] = new ConstantBufferData()
                    {
                        Name = $"dummy_{i}",
                        Size = 16,
                        Parameters = new List<d3dx_parameter>(),
                    };
                }
            }

            var rawSamplers = new List<ShaderData.Sampler>();
            if (samplers != null)
            {
                foreach (var sampler in samplers)
                {
                    rawSamplers.Add(ConvertSampler(sampler));
                }
            }

            var shaderData = new ShaderData()
            {
                _attributes = Array.Empty<ShaderData.Attribute>(),
                _samplers = rawSamplers.ToArray(),
                _cbuffers = Enumerable.Range(0, cbuffers.Count).Select(i=>i).ToArray(),
                ShaderCode = shaderByteCode.ToArray(),
                IsVertexShader = shaderStage == ShaderStage.Vertex,
            };

            var effectObject = new EffectObject();
            effectObject.ConstantBuffers = cbuffers;
            effectObject.Shaders = new List<ShaderData>() { shaderData };
            effectObject.Techniques = new d3dx_technique[1]
            {
                new d3dx_technique()
                {
                    name = "tech0",
                    pass_count = 1,
                    pass_handles = new d3dx_pass[1]
                    {
                        new d3dx_pass()
                        {
                            name = "pass0",
                            blendState = null,
                            depthStencilState = null,
                            rasterizerState = null,
                            state_count = 2,
                            states = new d3dx_state[2]
                            {
                                new d3dx_state()
                                {
                                    type = STATE_TYPE.CONSTANT,
                                    operation = (int)STATE_CLASS.VERTEXSHADER,
                                    parameter = new d3dx_parameter()
                                    {
                                        data = shaderStage == ShaderStage.Vertex ? 0 : -1,
                                    }
                                },
                                new d3dx_state()
                                {
                                    type = STATE_TYPE.CONSTANT,
                                    operation = (int)STATE_CLASS.PIXELSHADER,
                                    parameter = new d3dx_parameter()
                                    {
                                        data = shaderStage == ShaderStage.Pixel ? 0 : -1,
                                    }
                                }
                            },
                        }
                    },
                }
            };

            var parameters = new List<d3dx_parameter>();
            foreach (var cb in effectObject.ConstantBuffers)
            {
                foreach (var param in cb.Parameters)
                {
                    var match = parameters.FindIndex(e => e.name == param.name);
                    if (match == -1)
                    {
                        cb.ParameterIndex.Add(parameters.Count);
                        parameters.Add(param);
                    }
                    else
                    {
                        if (param.type != parameters[match].type
                            || param.rows != parameters[match].rows
                            || param.columns != parameters[match].columns
                            || param.element_count != parameters[match].element_count)
                        {
                            throw new Exception($"Parameter {param.name} conflicts with existing parameter.");
                        }

                        cb.ParameterIndex.Add(match);
                    }
                }
            }

            foreach (var shader in effectObject.Shaders)
            {
                for (var s = 0; s < shader._samplers.Length; s++)
                {
                    var sampler = shader._samplers[s];

                    var match = parameters.FindIndex(e => e.name == sampler.parameterName);
                    if (match == -1)
                    {
                        shader._samplers[s].parameter = parameters.Count;
                        parameters.Add(CreateTextureParamater(sampler));
                    }
                    else
                    {
                        // TODO: Make sure the type and size of the parameter match up!
                        shader._samplers[s].parameter = match;
                    }
                }
            }

            effectObject.Parameters = parameters.ToArray();

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            effectObject.Write(writer);
            return ms.ToArray();
        }
    
        private static ConstantBufferData ConvertConstantBuffer(ConstantBuffer constantBuffer)
        {
            var cb = new ConstantBufferData()
            {
                Name = constantBuffer.Name,
                Size = constantBuffer.SizeInBytes,
                ParameterIndex = new List<int>(constantBuffer.Parameters.Count),
                ParameterOffset = new List<int>(constantBuffer.Parameters.Count),
                Parameters = new List<d3dx_parameter>(constantBuffer.Parameters.Count),
            };

            for (int i = 0; i < constantBuffer.Parameters.Count; i++)
            {
                var d3dxParam = ConvertShaderParameter(constantBuffer.Parameters[i]);
                cb.ParameterOffset.Add(d3dxParam.bufferOffset);
                cb.Parameters.Add(d3dxParam);
            }

            return cb;
        }

        private static d3dx_parameter ConvertShaderParameter(ShaderParameter parameter)
        {
            var d3dxParam = new d3dx_parameter()
            {
                name = parameter.Name,
                semantic = parameter.Semantic,
                bufferOffset = parameter.BufferOffset,
                rows = (uint)parameter.RowCount,
                columns = (uint)parameter.ColumnCount,
                class_ = parameter.ParameterClass,
                type = parameter.ParameterType,
                element_count = 0,
                member_count = 0,
            };

            if (parameter.ElementCount > 0) // array
            {
                d3dxParam.element_count = (uint)parameter.ElementCount;
                d3dxParam.member_handles = new d3dx_parameter[parameter.ElementCount];
                for (int i = 0; i < d3dxParam.member_handles.Length; i++)
                {
                    d3dxParam.member_handles[i] = new d3dx_parameter()
                    {
                        name = string.Empty,
                        semantic = string.Empty,
                        rows = d3dxParam.rows,
                        columns = d3dxParam.columns,
                        class_ = parameter.ParameterClass,
                        type = parameter.ParameterType,
                        data = new byte[d3dxParam.rows * d3dxParam.columns * 4],
                    };
                }
                d3dxParam.data = new byte[d3dxParam.rows * d3dxParam.columns * 4];
            }
            else
            {
                d3dxParam.member_handles = Array.Empty<d3dx_parameter>();
                d3dxParam.data = new byte[d3dxParam.rows * d3dxParam.columns * 4];
            }

            return d3dxParam;
        }

        private static ShaderData.Sampler ConvertSampler(SamplerInfo samplerInfo)
        {
            var sampler = new ShaderData.Sampler()
            {
                samplerName = samplerInfo.Name,
                textureSlot = samplerInfo.TextureSlot,
                samplerSlot = samplerInfo.SamplerSlot,
                parameterName = samplerInfo.TextureName,
                parameter = -1,
                type = samplerInfo.Type,
                state = samplerInfo.State,
            };
            return sampler;
        }

        private static d3dx_parameter CreateTextureParamater(ShaderData.Sampler sampler)
        {
            var d3dxParam = new d3dx_parameter();
            d3dxParam.class_ = EffectParameterClass.Object;
            d3dxParam.name = sampler.parameterName;
            d3dxParam.semantic = string.Empty;

            switch (sampler.type)
            {
                case SamplerType.Sampler1D:
                    d3dxParam.type = EffectParameterType.Texture1D;
                    break;

                case SamplerType.Sampler2D:
                    d3dxParam.type = EffectParameterType.Texture2D;
                    break;

                case SamplerType.SamplerVolume:
                    d3dxParam.type = EffectParameterType.Texture3D;
                    break;

                case SamplerType.SamplerCube:
                    d3dxParam.type = EffectParameterType.TextureCube;
                    break;
            }
            return d3dxParam;
        }
    }
}

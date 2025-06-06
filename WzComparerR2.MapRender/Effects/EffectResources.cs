using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using WzComparerR2.Rendering.EffectCompiler;

namespace WzComparerR2.MapRender.Effects
{
    public static class EffectResources
    {
        static EffectResources()
        {
            nativeShaderDescriptions = new Dictionary<string, NativeShaderDesc>();
            nativeShaderMgfxCache = new Dictionary<string, byte[]>();
            nativeShaderDescriptions.Add("default", new NativeShaderDesc()
            {
                Name = "default",
                OriginFileName = "g_module_default_ps",
                FileLength = 796,
                Version = "KMST1186",
                Stage = ShaderStage.Pixel,
                ConstantBuffers = new List<ConstantBuffer>(),
                Samplers = new List<SamplerInfo>()
                {
                    new SamplerInfo()
                    {
                        Name = "src_sampler_sampler_s",
                        SamplerSlot = 0,
                        TextureName = "src_sampler",
                        TextureSlot = 0,
                        Type = SamplerType.Sampler2D,
                    }
                },
            });
            nativeShaderDescriptions.Add("light", new NativeShaderDesc
            {
                Name = "light",
                OriginFileName = "g_module_light_ps",
                FileLength = 1828,
                Version = "KMST1186",
                Stage = ShaderStage.Pixel,
                ConstantBuffers = new List<ConstantBuffer>()
                {
                    new ConstantBuffer
                    {
                        Name = "_2",
                        Slot = 2,
                        SizeInBytes = 16,
                        Parameters = new List<ShaderParameter>()
                        {
                            new ShaderParameter("z", null, 0, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                        }
                    },
                    new ConstantBuffer
                    {
                        Name = "_4",
                        Slot = 4,
                        SizeInBytes = 96,
                        Parameters = new List<ShaderParameter>()
                        {
                            new ShaderParameter("player_pos", "cb1[0].xy", 0, EffectParameterClass.Vector, EffectParameterType.Single, 1, 2, 0),
                            new ShaderParameter("light_inner_radius", "cb1[0].z", 8, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("light_outer_radius", "cb1[0].w", 12, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("player_light_color", "cb1[1].xyzw", 16, EffectParameterClass.Vector, EffectParameterType.Single, 1, 4, 0),
                            new ShaderParameter("top_color", "cb1[2].xyzw", 32, EffectParameterClass.Vector, EffectParameterType.Single, 1, 4, 0),
                            new ShaderParameter("bottom_color", "cb1[3].xyzw", 48, EffectParameterClass.Vector, EffectParameterType.Single, 1, 4, 0),
                            new ShaderParameter("min_y", "cb1[4].x", 64, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("max_y", "cb1[4].y", 68, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                        }
                    },
                },
                Samplers = new List<SamplerInfo>()
                {
                    new SamplerInfo()
                    {
                        Name = "src_tex_sampler_s",
                        SamplerSlot = 0,
                        TextureName = "src_tex",
                        TextureSlot = 0,
                        Type = SamplerType.Sampler2D,
                    }
                },
            });
            nativeShaderDescriptions.Add("waterBack", new NativeShaderDesc
            {
                Name = "waterBack",
                OriginFileName = "g_module_water_back_ps",
                FileLength = 1536,
                Version = "KMST1186",
                Stage = ShaderStage.Pixel,
                ConstantBuffers = new List<ConstantBuffer>()
                {
                    new ConstantBuffer
                    {
                        Name = "_2",
                        Slot = 2,
                        SizeInBytes = 176,
                        Parameters = new List<ShaderParameter>()
                        {
                            new ShaderParameter("min_y", null, 0, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("max_y", null, 4, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("color0", null, 16, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("level0", null, 28, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("color1", null, 32, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("level1", null, 44, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("color2", null, 48, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("level2", null, 60, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("color3", null, 64, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("level3", null, 76, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("color4", null, 80, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("level4", null, 92, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("color5", null, 96, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("level5", null, 108, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("color6", null, 112, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("level6", null, 124, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("color7", null, 128, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("level7", null, 140, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("color8", null, 144, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("level8", null, 156, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("color9", null, 160, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("level9", null, 172, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                        }
                    }
                }
            });
            nativeShaderDescriptions.Add("waterFront", new NativeShaderDesc
            {
                Name = "waterFront",
                OriginFileName = "g_module_water_front_ps",
                FileLength = 3872,
                Version = "KMST1186",
                Stage = ShaderStage.Pixel,
                ConstantBuffers = new List<ConstantBuffer>()
                {
                    new ConstantBuffer()
                    {
                        Name = "_0",
                        Slot = 0,
                        SizeInBytes = 144,
                        Parameters = new List<ShaderParameter>()
                        {
                            new ShaderParameter("vp", null, 0, EffectParameterClass.Matrix, EffectParameterType.Single, 4, 4, 0),
                            new ShaderParameter("vp_inv", null, 64, EffectParameterClass.Matrix, EffectParameterType.Single, 4, 4, 0),
                            new ShaderParameter("resolution_time", null, 128, EffectParameterClass.Vector, EffectParameterType.Single, 1, 4, 0),
                        }
                    },
                    new ConstantBuffer()
                    {
                        Name = "_2",
                        Slot = 2,
                        SizeInBytes = 112,
                        Parameters = new List<ShaderParameter>()
                        {
                            new ShaderParameter("value", null, 0, EffectParameterClass.Vector, EffectParameterType.Single, 1, 2, 0),
                            new ShaderParameter("cgrad", null, 8, EffectParameterClass.Vector, EffectParameterType.Single, 1, 2, 0),
                            new ShaderParameter("octave", null, 16, EffectParameterClass.Vector, EffectParameterType.Single, 1, 2, 0),
                            new ShaderParameter("strength", null, 24, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("edge", null, 28, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("godray_min_y", null, 32, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("godray_max_y", null, 36, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("godray_move", null, 40, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("godray_bright", null, 44, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("godray_color", null, 48, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("godray_uv_rot", null, 60, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("godray_uv_scale", null, 64, EffectParameterClass.Vector, EffectParameterType.Single, 1, 2, 0),
                            new ShaderParameter("aberration", null, 72, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("dist_strength", null, 76, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("diffuse", null, 80, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                            new ShaderParameter("screen_min_y", null, 92, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("screen_max_y", null, 96, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("screen_color", null, 100, EffectParameterClass.Vector, EffectParameterType.Single, 1, 3, 0),
                        },
                    },
                    new ConstantBuffer()
                    {
                        Name = "_4",
                        Slot = 4,
                        SizeInBytes = 96,
                        Parameters = new List<ShaderParameter>()
                        {
                            new ShaderParameter("player_pos", "cb1[0].xy", 0, EffectParameterClass.Vector, EffectParameterType.Single, 1, 2, 0),
                            new ShaderParameter("distance_factor1", "cb1[1].x", 16, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("min_y", "cb1[4].x", 64, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("max_y", "cb1[4].y", 68, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                            new ShaderParameter("dist_center_pos", "cb1[4].zw", 72, EffectParameterClass.Vector, EffectParameterType.Single, 1, 2, 0),
                            new ShaderParameter("distance_factor2", "cb1[5].x", 80, EffectParameterClass.Scalar, EffectParameterType.Single, 1, 1, 0),
                        }
                    },
                },
                Samplers = new List<SamplerInfo>()
                {
                    new SamplerInfo()
                    {
                        Name = "noise_tex_sampler_s",
                        SamplerSlot = 1,
                        TextureName = "noise_tex",
                        TextureSlot = 1,
                        Type = SamplerType.Sampler2D,
                    },
                    new SamplerInfo()
                    {
                        Name = "godray_noise_tex_sampler_s",
                        SamplerSlot = 2,
                        TextureName = "godray_noise_tex",
                        TextureSlot = 2,
                        Type = SamplerType.Sampler2D,
                    },
                    new SamplerInfo()
                    {
                        Name = "bg_tex_sampler_s",
                        SamplerSlot = 3,
                        TextureName = "bg_tex",
                        TextureSlot = 3,
                        Type = SamplerType.Sampler2D,
                    }
                },
            });
            nativeShaderDescriptions.Add("vs_position_color_texture", new NativeShaderDesc
            {
                Name = "vs_position_color_texture",
                OriginFileName = "off_42b030",
                FileLength = 1668,
                Version = "KMST1186",
                Stage = ShaderStage.Vertex,
                ConstantBuffers = new List<ConstantBuffer>()
                {
                    new ConstantBuffer()
                    {
                        Name = "_0",
                        Slot = 0,
                        SizeInBytes = 144,
                        Parameters = new List<ShaderParameter>()
                        {
                            new ShaderParameter("vp", null, 0, EffectParameterClass.Matrix, EffectParameterType.Single, 4, 4, 0),
                            new ShaderParameter("vp_inv", null, 64, EffectParameterClass.Matrix, EffectParameterType.Single, 4, 4, 0),
                            new ShaderParameter("resolution_time", null, 128, EffectParameterClass.Vector, EffectParameterType.Single, 1, 4, 0),
                        }
                    },
                    new ConstantBuffer()
                    {
                        Name = "_1",
                        Slot = 1,
                        SizeInBytes = 64,
                        Parameters = new List<ShaderParameter>()
                        {
                            new ShaderParameter("world", null, 0, EffectParameterClass.Matrix, EffectParameterType.Single, 4, 4, 0),
                        }
                    },
                },
            });
        }

        private static Dictionary<string, NativeShaderDesc> nativeShaderDescriptions;
        private static Dictionary<string, byte[]> nativeShaderMgfxCache;

        public static ReadOnlySpan<byte> GetNativeShaderEffectBytes(string shaderName)
        {
            return GetNativeShaderEffectBytesInternal(shaderName);
        }

        public static Effect CreateNativeShader(GraphicsDevice graphicsDevice, string shaderName)
        {
            return new Effect(graphicsDevice, GetNativeShaderEffectBytesInternal(shaderName));
        }

        private static byte[] GetNativeShaderEffectBytesInternal(string shaderName)
        {
            if (!nativeShaderMgfxCache.TryGetValue(shaderName, out byte[] effectFile))
            {
                effectFile = CompileNativeShader(shaderName);
                nativeShaderMgfxCache.Add(shaderName, effectFile);
            }

            return effectFile;
        }

        private static byte[] CompileNativeShader(string shaderName)
        {
            if (!nativeShaderDescriptions.TryGetValue(shaderName, out var nativeShaderDesc))
            {
                throw new ArgumentException($"Can't find shader description of '{shaderName}'.", nameof(shaderName));
            }

            var asm = Assembly.GetAssembly(typeof(EffectResources));
            byte[] shaderByteCode;
            using (var input = asm.GetManifestResourceStream($"WzComparerR2.MapRender.Effects.Resources.Native.{shaderName}"))
            {
                shaderByteCode = new byte[input.Length];
                input.Read(shaderByteCode, 0, shaderByteCode.Length);
            }

            var effectFile = ShaderConverter.D3DShaderByteCodeToMgfx(shaderByteCode, 
                nativeShaderDesc.Stage, 
                nativeShaderDesc.ConstantBuffers,
                nativeShaderDesc.Samplers);
            return effectFile;
        }
    }
}

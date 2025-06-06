// Port from https://github.com/MonoGame/MonoGame/blob/develop/Tools/MonoGame.Effect.Compiler/Effect/EffectObject.writer.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Rendering.EffectCompiler.Internal
{
    public class EffectObject
    {
        static EffectObject()
        {
            Type mgfxHeaderType = typeof(GraphicsDevice).Assembly.GetType("Microsoft.Xna.Framework.Graphics.Effect+MGFXHeader", true);
            MGFXSignature = (int)mgfxHeaderType.GetField("MGFXSignature", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            MGFXVersion = (int)mgfxHeaderType.GetField("MGFXVersion", BindingFlags.Static | BindingFlags.Public).GetValue(null);

            Type hashType = typeof(GraphicsDevice).Assembly.GetType("MonoGame.Framework.Utilities.Hash", true);
            var computeHashMethod = hashType.GetMethod("ComputeHash", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Stream) }, null);
            ComputeHash = (Func<Stream, int>)computeHashMethod.CreateDelegate(typeof(Func<Stream, int>));
        }

        public static readonly int MGFXSignature;
        public static readonly int MGFXVersion;
        public static readonly int ShaderProfile = 1; //Directx_11
        public static readonly Func<Stream, int> ComputeHash;


        public d3dx_parameter[] Parameters;
        public d3dx_technique[] Techniques;
        public List<ShaderData> Shaders;
        public List<ConstantBufferData> ConstantBuffers;

        /// <summary>
        /// Writes the effect for loading later.
        /// </summary>
        public void Write(BinaryWriter writer)
        {
            writer.Write(MGFXSignature);
            writer.Write((byte)MGFXVersion);
            writer.Write((byte)ShaderProfile);

            // Write the rest to a memory stream.
            using (MemoryStream memStream = new MemoryStream())
            using (BinaryWriter memWriter = new BinaryWriter(memStream))
            {
                // Write all the constant buffers.
                memWriter.MgfxWriteElementCount(ConstantBuffers.Count);
                foreach (var cbuffer in ConstantBuffers)
                    cbuffer.Write(memWriter);

                // Write all the shaders.
                memWriter.MgfxWriteElementCount(Shaders.Count);
                foreach (var shader in Shaders)
                    shader.Write(memWriter);

                // Write the parameters.
                WriteParameters(memWriter, Parameters, Parameters.Length);

                // Write the techniques.
                memWriter.MgfxWriteElementCount(Techniques.Length);
                foreach (var technique in Techniques)
                {
                    memWriter.Write(technique.name);
                    WriteAnnotations(memWriter, technique.annotation_handles);

                    // Write the passes.
                    memWriter.MgfxWriteElementCount((int)technique.pass_count);
                    for (var p = 0; p < technique.pass_count; p++)
                    {
                        var pass = technique.pass_handles[p];

                        memWriter.Write(pass.name);
                        WriteAnnotations(memWriter, pass.annotation_handles);

                        // Write the index for the vertex and pixel shaders.
                        memWriter.MgfxWriteElementCount(GetShaderIndex(STATE_CLASS.VERTEXSHADER, pass.states));
                        memWriter.MgfxWriteElementCount(GetShaderIndex(STATE_CLASS.PIXELSHADER, pass.states));

                        // Write the state objects too!
                        if (pass.blendState != null)
                        {
                            memWriter.Write(true);
                            memWriter.Write((byte)pass.blendState.AlphaBlendFunction);
                            memWriter.Write((byte)pass.blendState.AlphaDestinationBlend);
                            memWriter.Write((byte)pass.blendState.AlphaSourceBlend);
                            memWriter.Write(pass.blendState.BlendFactor.R);
                            memWriter.Write(pass.blendState.BlendFactor.G);
                            memWriter.Write(pass.blendState.BlendFactor.B);
                            memWriter.Write(pass.blendState.BlendFactor.A);
                            memWriter.Write((byte)pass.blendState.ColorBlendFunction);
                            memWriter.Write((byte)pass.blendState.ColorDestinationBlend);
                            memWriter.Write((byte)pass.blendState.ColorSourceBlend);
                            memWriter.Write((byte)pass.blendState.ColorWriteChannels);
                            memWriter.Write((byte)pass.blendState.ColorWriteChannels1);
                            memWriter.Write((byte)pass.blendState.ColorWriteChannels2);
                            memWriter.Write((byte)pass.blendState.ColorWriteChannels3);
                            memWriter.Write(pass.blendState.MultiSampleMask);
                        }
                        else
                            memWriter.Write(false);

                        if (pass.depthStencilState != null)
                        {
                            memWriter.Write(true);
                            memWriter.Write((byte)pass.depthStencilState.CounterClockwiseStencilDepthBufferFail);
                            memWriter.Write((byte)pass.depthStencilState.CounterClockwiseStencilFail);
                            memWriter.Write((byte)pass.depthStencilState.CounterClockwiseStencilFunction);
                            memWriter.Write((byte)pass.depthStencilState.CounterClockwiseStencilPass);
                            memWriter.Write(pass.depthStencilState.DepthBufferEnable);
                            memWriter.Write((byte)pass.depthStencilState.DepthBufferFunction);
                            memWriter.Write(pass.depthStencilState.DepthBufferWriteEnable);
                            memWriter.Write(pass.depthStencilState.ReferenceStencil);
                            memWriter.Write((byte)pass.depthStencilState.StencilDepthBufferFail);
                            memWriter.Write(pass.depthStencilState.StencilEnable);
                            memWriter.Write((byte)pass.depthStencilState.StencilFail);
                            memWriter.Write((byte)pass.depthStencilState.StencilFunction);
                            memWriter.Write(pass.depthStencilState.StencilMask);
                            memWriter.Write((byte)pass.depthStencilState.StencilPass);
                            memWriter.Write(pass.depthStencilState.StencilWriteMask);
                            memWriter.Write(pass.depthStencilState.TwoSidedStencilMode);
                        }
                        else
                            memWriter.Write(false);

                        if (pass.rasterizerState != null)
                        {
                            memWriter.Write(true);
                            memWriter.Write((byte)pass.rasterizerState.CullMode);
                            memWriter.Write(pass.rasterizerState.DepthBias);
                            memWriter.Write((byte)pass.rasterizerState.FillMode);
                            memWriter.Write(pass.rasterizerState.MultiSampleAntiAlias);
                            memWriter.Write(pass.rasterizerState.ScissorTestEnable);
                            memWriter.Write(pass.rasterizerState.SlopeScaleDepthBias);
                        }
                        else
                            memWriter.Write(false);
                    }
                }

                // Calculate a hash code from memory stream
                // and write it to the header.
                
                var effectKey = ComputeHash(memStream);
                writer.Write(effectKey);

                //write content from memory stream to final stream.
                memStream.WriteTo(writer.BaseStream);
            }

            // Write a tail to be used by the reader for validation.
            if (MGFXVersion >= 10)
                writer.Write(MGFXSignature);
        }

        private static void WriteParameters(BinaryWriter writer, d3dx_parameter[] parameters, int count)
        {
            if (MGFXVersion < 10) writer.Write7BitEncodedInt(count);
            else writer.Write(count);
            for (var i = 0; i < count; i++)
                WriteParameter(writer, parameters[i]);
        }

        private static void WriteParameter(BinaryWriter writer, d3dx_parameter param)
        {
            writer.Write((byte)param.class_);
            writer.Write((byte)param.type);

            writer.Write(param.name);
            writer.Write(param.semantic ?? string.Empty);
            WriteAnnotations(writer, param.annotation_handles);

            writer.Write((byte)param.rows);
            writer.Write((byte)param.columns);

            // Write the elements or struct members.
            WriteParameters(writer, param.member_handles, (int)param.element_count);
            WriteParameters(writer, param.member_handles, (int)param.member_count);

            if (param.element_count == 0 && param.member_count == 0)
            {
                switch (param.type)
                {
                    case EffectParameterType.Bool:
                    case EffectParameterType.Int32:
                    case EffectParameterType.Single:
                        writer.Write((byte[])param.data);
                        break;
                }
            }
        }

        private static void WriteAnnotations(BinaryWriter writer, d3dx_parameter[] annotations)
        {
            // annotation is not supported yet
            writer.MgfxWriteElementCount(0);
            //var count = annotations == null ? 0 : annotations.Length;
            //writer.Write(count);
            //for (var i = 0; i < count; i++)
            //	WriteParameter(writer, annotations[i]);
        }

        internal static int GetShaderIndex(STATE_CLASS type, d3dx_state[] states)
        {
            foreach (var state in states)
            {
                var class_ = (STATE_CLASS)state.operation;
                if (class_ != type)
                    continue;

                if (state.type != STATE_TYPE.CONSTANT)
                    throw new NotSupportedException("We do not support shader expressions!");

                return (int)state.parameter.data;
            }

            return -1;
        }
    }

    internal static class EffectWriterExtensions
    {
        public static void MgfxWriteElementCount(this BinaryWriter writer, int value)
        {
            if (EffectObject.MGFXVersion < 10)
            {
                writer.Write((byte)value);
            }
            else
            {
                writer.Write(value);
            }
        }

#if NETFRAMEWORK
        private static Action<BinaryWriter, int> write7BitEncodedIntFunc;
        public static void Write7BitEncodedInt(this BinaryWriter writer, int value)
        {
            if (write7BitEncodedIntFunc == null)
            {
                MethodInfo method = typeof(BinaryWriter).GetMethod("Write7BitEncodedInt", BindingFlags.Instance | BindingFlags.NonPublic);
                write7BitEncodedIntFunc = (Action<BinaryWriter, int>)method.CreateDelegate(typeof(Action<BinaryWriter, int>));
            }
            write7BitEncodedIntFunc(writer, value);
        }
#endif
    }
}

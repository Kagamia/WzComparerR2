// Port from https://github.com/MonoGame/MonoGame/blob/develop/Tools/MonoGame.Effect.Compiler/Effect/EffectObject.cs

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Rendering.EffectCompiler.Internal
{
    public class d3dx_parameter
    {
        public string name;
        public string semantic;
        public object data;
        public EffectParameterClass class_;
        public EffectParameterType type;
        public uint rows;
        public uint columns;
        public uint element_count;
        public uint annotation_count = 0;
        public uint member_count;
        public uint flags = 0;
        public uint bytes = 0;

        public int bufferIndex = -1;
        public int bufferOffset = -1;

        public d3dx_parameter[] annotation_handles = null;
        public d3dx_parameter[] member_handles;

        public override string ToString()
        {
            if (rows > 0 || columns > 0)
                return string.Format("{0} {1}{2}x{3} {4} : cb{5},{6}", class_, type, rows, columns, name, bufferIndex, bufferOffset);
            else
                return string.Format("{0} {1} {2}", class_, type, name);
        }
    }

    public class d3dx_state
    {
        public uint operation;
        public uint index;
        public STATE_TYPE type;
        public d3dx_parameter parameter;
    }

    public class d3dx_sampler
    {
        public uint state_count = 0;
        public d3dx_state[] states = null;
    }

    public enum STATE_TYPE
    {
        CONSTANT,
        PARAMETER,
        EXPRESSION,
        EXPRESSIONINDEX,
    }

    public enum STATE_CLASS
    {
        LIGHTENABLE,
        FVF,
        LIGHT,
        MATERIAL,
        NPATCHMODE,
        PIXELSHADER,
        RENDERSTATE,
        SETSAMPLER,
        SAMPLERSTATE,
        TEXTURE,
        TEXTURESTAGE,
        TRANSFORM,
        VERTEXSHADER,
        SHADERCONST,
        UNKNOWN,
    };

    public class d3dx_pass
    {
        public string name;
        public uint state_count;
        public uint annotation_count = 0;

        public BlendState blendState;
        public DepthStencilState depthStencilState;
        public RasterizerState rasterizerState;

        public d3dx_state[] states;
        public d3dx_parameter[] annotation_handles = null;
    }

    public class d3dx_technique
    {
        public string name;
        public uint pass_count;
        public uint annotation_count = 0;

        public d3dx_parameter[] annotation_handles = null;
        public d3dx_pass[] pass_handles;
    }

}

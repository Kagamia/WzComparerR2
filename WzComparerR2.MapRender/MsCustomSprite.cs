using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.MapRender.Effects;

namespace WzComparerR2.MapRender
{
    public class MsCustomSpriteData
    {
        public MsCustomTexture[] Textures { get; set; }
        public MsShader Shader { get; set; }
    }

    public class MsShader
    {
        public string ID { get; set; }
        public Dictionary<string, MsShaderConstant> Constants { get; private set; } = new();
    }

    public struct MsCustomTexture
    {
        public Texture2D Texture { get; set; }
        public Texture2D[] Textures { get; set; }
        public int AddressU { get; set; }
        public int AddressV { get; set; }
    }

    public struct MsShaderConstant
    {
        public MsShaderConstant(float value)
        {
            this.ColumnCount = 1;
            this.X = value;
            this.Y = 0;
            this.Z = 0;
        }

        public MsShaderConstant(float x, float y)
        {
            this.ColumnCount = 2;
            this.X = x;
            this.Y = y;
            this.Z = 0;
        }

        public MsShaderConstant(float x, float y, float z)
        {
            this.ColumnCount = 3;
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public int ColumnCount { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public float ToScalar() => this.X;
        public Vector2 ToVector2() => new Vector2(this.X, this.Y);
        public Vector3 ToVector3() => new Vector3(this.X, this.Y, this.Z);
    }

    public class MsCustomSprite
    {
        public Vector2 Size { get; set; }
        public ShaderMaterial Material { get; set; }
    }
}

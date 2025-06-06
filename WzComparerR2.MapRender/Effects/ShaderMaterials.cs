using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender.Effects
{
    public static class ShaderMaterialFactory
    {
        public static ShaderMaterial Create(MsCustomSpriteData msSprite)
        {
            var shaderMaterial = Create(msSprite.Shader.ID);
            shaderMaterial.LoadFromMsSprite(msSprite);
            return shaderMaterial;
        }

        public static ShaderMaterial Create(string shaderID)
        {
            return shaderID switch
            {
                "light" => new LightPixelShaderMaterial(),
                "waterBack" => new WaterBackPixelShaderMaterial(),
                "waterFront" => new WaterFrontPixelShaderMaterial(),
                _ => throw new Exception($"Unsupported shader: {shaderID}"),
            };
        }
    }

    public abstract class ShaderMaterial : IEquatable<ShaderMaterial>
    {
        public ShaderMaterial(string shaderID)
        {
            this.ShaderID = shaderID;
        }

        public string ShaderID { get; protected set; }
        public abstract void ApplyParameters(Effect effect);
        public virtual void ApplySamplerStates(GraphicsDevice graphicsDevice) { }
        public virtual void LoadFromMsSprite(MsCustomSpriteData msSprite) { }
        public virtual bool Equals(ShaderMaterial other)
        {
            // TODO: check if all exported shader parameters are identical.
            return Object.ReferenceEquals(this, other);
        }

        protected void LoadBindingParameters(MsCustomSpriteData msCustomSprite)
        {
            foreach (var propInfo in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var bindingAttr = propInfo.GetCustomAttribute<ShaderParameterBindingAttribute>();
                if (bindingAttr != null)
                {
                    if (bindingAttr.TextureIndex > -1 && bindingAttr.TextureIndex < msCustomSprite.Textures.Length)
                    {
                        var msCustomTexture = msCustomSprite.Textures[bindingAttr.TextureIndex];
                        if (propInfo.PropertyType == typeof(Texture2D))
                        {
                            propInfo.SetValue(this, msCustomTexture.Texture);
                        }
                        else if (propInfo.PropertyType == typeof(SamplerState))
                        {
                            var samplerState = new SamplerState();
                            samplerState.AddressU = msCustomTexture.AddressU switch
                            {
                                1 => TextureAddressMode.Wrap,
                                _ => TextureAddressMode.Clamp,
                            };
                            samplerState.AddressV = msCustomTexture.AddressV switch
                            {
                                1 => TextureAddressMode.Wrap,
                                _ => TextureAddressMode.Clamp,
                            };
                            propInfo.SetValue(this, samplerState);
                        }
                    }
                    else if (msCustomSprite.Shader.Constants.TryGetValue(bindingAttr.ParameterName, out var shaderConstant))
                    {
                        if (propInfo.PropertyType == typeof(float))
                        {
                            propInfo.SetValue(this, shaderConstant.ToScalar());
                        }
                        else if (propInfo.PropertyType == typeof(Vector2))
                        {
                            propInfo.SetValue(this, shaderConstant.ToVector2());
                        }
                        else if (propInfo.PropertyType == typeof(Vector3))
                        {
                            propInfo.SetValue(this, shaderConstant.ToVector3());
                        }
                    }
                }
            }
        }

        protected void ApplyBindingParameters(Effect effect)
        {
            foreach (var propInfo in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var bindingAttr = propInfo.GetCustomAttribute<ShaderParameterBindingAttribute>();
                if (bindingAttr != null && !string.IsNullOrEmpty(bindingAttr.ParameterName))
                {
                    switch (propInfo.GetValue(this))
                    {
                        case float _float:
                            effect.Parameters[bindingAttr.ParameterName].SetValue(_float);
                            break;
                        case Vector2 vec2:
                            effect.Parameters[bindingAttr.ParameterName].SetValue(vec2);
                            break;
                        case Vector3 vec3:
                            effect.Parameters[bindingAttr.ParameterName].SetValue(vec3);
                            break;
                        case Vector4 vec4:
                            effect.Parameters[bindingAttr.ParameterName].SetValue(vec4);
                            break;
                        case Matrix matrix:
                            effect.Parameters[bindingAttr.ParameterName].SetValue(matrix);
                            break;
                        case Texture tex:
                            effect.Parameters[bindingAttr.ParameterName].SetValue(tex);
                            break;
                    }
                }
            }
        }

        protected void ApplyBindingSamplers(GraphicsDevice graphicsDevice)
        {
            foreach (var propInfo in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var bindingAttr = propInfo.GetCustomAttribute<ShaderParameterBindingAttribute>();
                if (bindingAttr != null && bindingAttr.TextureIndex > -1)
                {
                    switch (propInfo.GetValue(this))
                    {
                        case SamplerState samplerState:
                            graphicsDevice.SamplerStates[bindingAttr.TextureIndex] = samplerState;
                            break;
                    }
                }
            }
        }
    }

    internal class ShaderParameterBindingAttribute : Attribute
    {
        public ShaderParameterBindingAttribute(string parameterName, int textureIndex = -1)
        {
            this.ParameterName = parameterName;
            this.TextureIndex = textureIndex;
        }

        public string ParameterName { get; protected set; }
        public int TextureIndex { get; protected set; }
    }

    public interface IMaplestoryEffectMatrices
    {
        Matrix ViewProjection { get; set; }
        Matrix ViewProjectionInverse { get; set; }
        Vector4 ResolutionTime { get; set; }
    }

    public interface IBackgroundCaptureEffect
    {
        Texture2D BackgroundTexture { get; set; }
    }

    public class LightPixelShaderMaterial : ShaderMaterial
    {
        public LightPixelShaderMaterial() : base("light")
        {
        }

        [ShaderParameterBinding("z")]
        public float Z { get; set; }
        [ShaderParameterBinding("src_tex", 0)]
        public Texture2D SrcTexture { get; set; }
        [ShaderParameterBinding(null, 0)]
        public SamplerState SrcTextureSamplerState { get; set; }

        [ShaderParameterBinding("player_pos")]
        public Vector2 PlayerPos { get; set; }
        [ShaderParameterBinding("light_inner_radius")]
        public float LightInnerRadius { get; set; }
        [ShaderParameterBinding("light_outer_radius")]
        public float LightOuterRadius { get; set; }
        [ShaderParameterBinding("player_light_color")]
        public Vector4 PlayerLightColor { get; set; }
        [ShaderParameterBinding("top_color")]
        public Vector4 TopColor { get; set; }
        [ShaderParameterBinding("bottom_color")]
        public Vector4 BottomColor { get; set; }
        [ShaderParameterBinding("min_y")]
        public float MinY { get; set; }
        [ShaderParameterBinding("max_y")]
        public float MaxY { get; set; }
        
        public override void LoadFromMsSprite(MsCustomSpriteData sprite)
        {
            base.LoadBindingParameters(sprite);
        }

        public override void ApplyParameters(Effect effect)
        {
            base.ApplyBindingParameters(effect);
        }

        public override void ApplySamplerStates(GraphicsDevice graphicsDevice)
        {
            base.ApplyBindingSamplers(graphicsDevice);
        }
    }

    public class WaterBackPixelShaderMaterial : ShaderMaterial
    {
        public WaterBackPixelShaderMaterial() : base("waterBack")
        {
        }

        [ShaderParameterBinding("min_y")]
        public float MinY { get; set; }
        [ShaderParameterBinding("max_y")]
        public float MaxY { get; set; }
        [ShaderParameterBinding("color0")]
        public Vector3 Color0 { get; set; }
        [ShaderParameterBinding("level0")]
        public float Level0 { get; set; }
        [ShaderParameterBinding("color1")]
        public Vector3 Color1 { get; set; }
        [ShaderParameterBinding("level1")]
        public float Level1 { get; set; }
        [ShaderParameterBinding("color2")]
        public Vector3 Color2 { get; set; }
        [ShaderParameterBinding("level2")]
        public float Level2 { get; set; }
        [ShaderParameterBinding("color3")]
        public Vector3 Color3 { get; set; }
        [ShaderParameterBinding("level3")]
        public float Level3 { get; set; }
        [ShaderParameterBinding("color4")]
        public Vector3 Color4 { get; set; }
        [ShaderParameterBinding("level4")]
        public float Level4 { get; set; }
        [ShaderParameterBinding("color5")]
        public Vector3 Color5 { get; set; }
        [ShaderParameterBinding("level5")]
        public float Level5 { get; set; }
        [ShaderParameterBinding("color6")]
        public Vector3 Color6 { get; set; }
        [ShaderParameterBinding("level6")]
        public float Level6 { get; set; }
        [ShaderParameterBinding("color7")]
        public Vector3 Color7 { get; set; }
        [ShaderParameterBinding("level7")]
        public float Level7 { get; set; }
        [ShaderParameterBinding("color8")]
        public Vector3 Color8 { get; set; }
        [ShaderParameterBinding("level8")]
        public float Level8 { get; set; }
        [ShaderParameterBinding("color9")]
        public Vector3 Color9 { get; set; }
        [ShaderParameterBinding("level9")]
        public float Level9 { get; set; }

        public override void LoadFromMsSprite(MsCustomSpriteData sprite)
        {
            base.LoadBindingParameters(sprite);
        }

        public override void ApplyParameters(Effect effect)
        {
            base.ApplyBindingParameters(effect);
        }
    }

    public class WaterFrontPixelShaderMaterial : ShaderMaterial, IMaplestoryEffectMatrices, IBackgroundCaptureEffect
    {
        public WaterFrontPixelShaderMaterial() : base("waterFront")
        {
        }

        [ShaderParameterBinding("vp")]
        public Matrix ViewProjection { get; set; }
        [ShaderParameterBinding("vp_inv")]
        public Matrix ViewProjectionInverse { get; set; }
        [ShaderParameterBinding("resolution_time")]
        public Vector4 ResolutionTime { get; set; }

        [ShaderParameterBinding("value")]
        public Vector2 Value { get; set; }
        [ShaderParameterBinding("cgrad")]
        public Vector2 Cgrad { get; set; }
        [ShaderParameterBinding("octave")]
        public Vector2 Octave { get; set; }
        [ShaderParameterBinding("strength")]
        public float Strength { get; set; }
        [ShaderParameterBinding("edge")]
        public float Edge { get; set; }
        [ShaderParameterBinding("godray_min_y")]
        public float GodrayMinY { get; set; }
        [ShaderParameterBinding("godray_max_y")]
        public float GodrayMaxY { get; set; }
        [ShaderParameterBinding("godray_move")]
        public float GodrayMove { get; set; }
        [ShaderParameterBinding("godray_bright")]
        public float GodrayBright { get; set; }
        [ShaderParameterBinding("godray_color")]
        public Vector3 GodrayColor { get; set; }
        [ShaderParameterBinding("godray_uv_rot")]
        public float GodrayUVRot { get; set; }
        [ShaderParameterBinding("godray_uv_scale")]
        public Vector2 GodrayUVScale { get; set; }
        [ShaderParameterBinding("aberration")]
        public float Aberration { get; set; }
        [ShaderParameterBinding("dist_strength")]
        public float DistStrength { get; set; }
        [ShaderParameterBinding("diffuse")]
        public Vector3 Diffuse { get; set; }
        [ShaderParameterBinding("screen_min_y")]
        public float ScreenMinY { get; set; }
        [ShaderParameterBinding("screen_max_y")]
        public float ScreenMaxY { get; set; }
        [ShaderParameterBinding("screen_color")]
        public Vector3 ScreenColor { get; set; }
        [ShaderParameterBinding("noise_tex", 1)]
        public Texture2D NoiseTexture { get; set; }
        [ShaderParameterBinding(null, 1)]
        public SamplerState NoiseTextureSamplerState { get; set; }
        [ShaderParameterBinding("godray_noise_tex", 2)]
        public Texture2D GodrayNoiseTexture { get; set; }
        [ShaderParameterBinding(null, 2)]
        public SamplerState GodrayNoiseTextureSamplerState { get; set; }
        [ShaderParameterBinding("bg_tex")]
        public Texture2D BackgroundTexture { get; set; }

        [ShaderParameterBinding("player_pos")]
        public Vector2 PlayerPos { get; set; }
        [ShaderParameterBinding("distance_factor1")]
        public float Factor1 { get; set; }
        [ShaderParameterBinding("min_y")]
        public float MinY { get; set; }
        [ShaderParameterBinding("max_y")]
        public float MaxY { get; set; }
        [ShaderParameterBinding("dist_center_pos")]
        public Vector2 DistNoiseCenterPos { get; set; }
        [ShaderParameterBinding("distance_factor2")]
        public float Factor2 { get; set; }

        public override void LoadFromMsSprite(MsCustomSpriteData sprite)
        {
            base.LoadBindingParameters(sprite);
        }

        public override void ApplyParameters(Effect effect)
        {
            base.ApplyBindingParameters(effect);
        }

        public override void ApplySamplerStates(GraphicsDevice graphicsDevice)
        {
            base.ApplyBindingSamplers(graphicsDevice);
        }
    }
}
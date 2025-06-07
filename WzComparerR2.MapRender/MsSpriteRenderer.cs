using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.MapRender.Effects;
using WzComparerR2.Rendering;

namespace WzComparerR2.MapRender
{
    public class MsSpriteRenderer : IDisposable
    {
        public MsSpriteRenderer(GraphicsDevice graphicsDevice) : this(graphicsDevice, 128)
        {
        }

        public MsSpriteRenderer(GraphicsDevice graphicsDevice, int capacity)
        {
            this.GraphicsDevice = graphicsDevice;
            this.loadedPixelShaders = new Dictionary<string, Effect>();
            this.vertexShader = EffectResources.CreateNativeShader(graphicsDevice, "vs_position_color_texture");
            this.capacity = capacity;
            this.vertices = new VertexPosition4ColorTexture[capacity * 4];
            this.indices = new short[capacity * 6];
        }

        public GraphicsDevice GraphicsDevice { get; private set; }
        private Effect vertexShader;
        private readonly Dictionary<string, Effect> loadedPixelShaders;
        private Texture2D cachedBackBufferTexture;

        private ShaderMaterial lastShaderMaterial;
        private VertexPosition4ColorTexture[] vertices;
        private short[] indices;
        private int capacity;
        private int primitiveCount;
        private bool isInBeginEndPair;
        private bool isDisposed;

        private Matrix vp;
        private Matrix vp_inv;
        private Vector4 resolution_time;
        private Matrix world;

        public void Begin(Vector2 cameraOrigin, float gameTime)
        {
            if (this.isInBeginEndPair)
            {
                throw new InvalidOperationException("Begin cannot be called again until End has been successfully called.");
            }

            var viewPort = this.GraphicsDevice.Viewport;
            Matrix.CreateOrthographicOffCenter(cameraOrigin.X, cameraOrigin.X + viewPort.Width, cameraOrigin.Y + viewPort.Height, cameraOrigin.Y, 0, -1, out this.vp);
            Matrix.Invert(ref this.vp, out this.vp_inv);
            this.resolution_time = new Vector4(viewPort.Width, viewPort.Height, gameTime, 0);
            this.world = Matrix.Identity;
            this.TrySetCommonParameters(this.vertexShader);

            this.isInBeginEndPair = true;
        }

        public void Draw(Vector2 worldPosition, Vector2 size, ShaderMaterial shaderMaterial)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(LightRenderer));
            }
            if (!this.isInBeginEndPair)
            {
                throw new InvalidOperationException("Begin must be called successfully before you can call Draw.");
            }

            if (this.lastShaderMaterial != null && !this.lastShaderMaterial.Equals(shaderMaterial))
            {
                this.Flush();
            }
            if (this.capacity - this.primitiveCount < 2)
            {
                this.Flush();
            }

            this.lastShaderMaterial = shaderMaterial;
            int vertexOffset = this.primitiveCount * 2;
            int indexOffset = this.primitiveCount * 3;
            Vector2 lt = worldPosition;
            Vector2 rb = worldPosition + size;
            this.vertices[vertexOffset + 0] = new VertexPosition4ColorTexture(new Vector4(lt.X, lt.Y, 0, 1), Color.White, new Vector2(0, 0));
            this.vertices[vertexOffset + 1] = new VertexPosition4ColorTexture(new Vector4(rb.X, lt.Y, 0, 1), Color.White, new Vector2(1, 0));
            this.vertices[vertexOffset + 2] = new VertexPosition4ColorTexture(new Vector4(lt.X, rb.Y, 0, 1), Color.White, new Vector2(0, 1));
            this.vertices[vertexOffset + 3] = new VertexPosition4ColorTexture(new Vector4(rb.X, rb.Y, 0, 1), Color.White, new Vector2(1, 1));
            this.indices[indexOffset + 0] = (short)(vertexOffset + 0);
            this.indices[indexOffset + 1] = (short)(vertexOffset + 1);
            this.indices[indexOffset + 2] = (short)(vertexOffset + 2);
            this.indices[indexOffset + 3] = (short)(vertexOffset + 1);
            this.indices[indexOffset + 4] = (short)(vertexOffset + 3);
            this.indices[indexOffset + 5] = (short)(vertexOffset + 2);
            this.primitiveCount += 2;
        }

        public void End()
        {
            if (!this.isInBeginEndPair)
            {
                throw new InvalidOperationException("Begin must be called before calling End.");
            }

            this.Flush();
            this.lastShaderMaterial = null;
            this.isInBeginEndPair = false;
        }

        private void Flush()
        {
            var shaderMaterial = this.lastShaderMaterial;
            var primitiveCount = this.primitiveCount;
            if (shaderMaterial == null || primitiveCount == 0)
            {
                return;
            }

            if (!this.loadedPixelShaders.TryGetValue(shaderMaterial.ShaderID, out Effect pixelShader))
            {
                pixelShader = EffectResources.CreateNativeShader(this.GraphicsDevice, shaderMaterial.ShaderID);
                this.loadedPixelShaders.Add(shaderMaterial.ShaderID, pixelShader);
            }
            this.vertexShader.CurrentTechnique.Passes[0].Apply();

            // pixel shader pre-process
            if (shaderMaterial is IMaplestoryEffectMatrices m)
            {
                m.ViewProjection = this.vp;
                m.ViewProjectionInverse = this.vp_inv;
                m.ResolutionTime = this.resolution_time;
            }
            IBackgroundCaptureEffect bgTexEffect = shaderMaterial as IBackgroundCaptureEffect;
            if (bgTexEffect != null)
            {
                var pp = this.GraphicsDevice.PresentationParameters;
                Texture2D bgTex;
                if (this.cachedBackBufferTexture == null 
                    || this.cachedBackBufferTexture.Width != pp.BackBufferWidth 
                    || this.cachedBackBufferTexture.Height != pp.BackBufferHeight 
                    || this.cachedBackBufferTexture.Format != pp.BackBufferFormat)
                {
                    bgTex = new Texture2D(this.GraphicsDevice,
                        this.GraphicsDevice.PresentationParameters.BackBufferWidth,
                        this.GraphicsDevice.PresentationParameters.BackBufferHeight,
                        false,
                        this.GraphicsDevice.PresentationParameters.BackBufferFormat);
                    this.cachedBackBufferTexture?.Dispose();
                    this.cachedBackBufferTexture = bgTex;
                }
                else
                {
                    bgTex = this.cachedBackBufferTexture;
                }
                this.GraphicsDevice.CopyBackBuffer(bgTex);
                bgTexEffect.BackgroundTexture = bgTex;
            }
            shaderMaterial.ApplyParameters(pixelShader);
            pixelShader.CurrentTechnique.Passes[0].Apply();
            shaderMaterial.ApplySamplerStates(this.GraphicsDevice);

            // draw primitives
            this.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            this.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            this.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, this.vertices, 0, primitiveCount * 2, this.indices, 0, primitiveCount);
            this.primitiveCount = 0;

            // clean-up shader parameters
            if (bgTexEffect != null)
            {
                bgTexEffect.BackgroundTexture = null;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.vertexShader != null)
                {
                    this.vertexShader.Dispose();
                    this.vertexShader = null;
                }
                if (this.loadedPixelShaders != null)
                {
                    foreach (var effect in this.loadedPixelShaders)
                    {
                        effect.Value.Dispose();
                    }
                    this.loadedPixelShaders.Clear();
                }
                if (this.cachedBackBufferTexture != null)
                {
                    this.cachedBackBufferTexture.Dispose();
                    this.cachedBackBufferTexture = null;
                }
            }

            this.lastShaderMaterial = null;
            this.vertices = null;
            this.indices = null;
            this.isDisposed = true;
        }

        private void TrySetCommonParameters(Effect effect)
        {
            effect.Parameters["vp"]?.SetValue(this.vp);
            effect.Parameters["vp_inv"]?.SetValue(this.vp_inv);
            effect.Parameters["resolution_time"]?.SetValue(this.resolution_time);
            effect.Parameters["world"]?.SetValue(this.world);
        }

        public struct VertexPosition4ColorTexture : IVertexType
        {
            public static readonly int Size = 28;

            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(Size,
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
                new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(20, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            );

            public VertexPosition4ColorTexture(Vector4 position, Color color, Vector2 textureCoordinate)
            {
                this.Position = position;
                this.Color = color;
                this.TextureCoordinate = textureCoordinate;
            }

            public Vector4 Position { get; set; }
            public Color Color { get; set; }
            public Vector2 TextureCoordinate { get; set; }

            public override string ToString() => $"{{position: {this.Position}, color: {this.Color}, texcoord: {this.TextureCoordinate}}}";
            VertexDeclaration IVertexType.VertexDeclaration => VertexPosition4ColorTexture.VertexDeclaration;
        }
    }
}

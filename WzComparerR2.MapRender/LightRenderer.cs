using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender
{
    public class LightRenderer : IDisposable
    {
        public LightRenderer(GraphicsDevice graphicsDevice) : this(graphicsDevice, 2048)
        {

        }

        public LightRenderer(GraphicsDevice graphicsDevice, int capacity)
        {
            this.graphicsDevice = graphicsDevice;
            this.effect = new BasicEffect(graphicsDevice)
            {
                FogEnabled = false,
                LightingEnabled = false,
                VertexColorEnabled = true,
            };
            this.blendState = new BlendState()
            {
                AlphaSourceBlend = Blend.Zero,
                AlphaDestinationBlend = Blend.One,
                AlphaBlendFunction = BlendFunction.Add,
                ColorSourceBlend = Blend.One,
                ColorDestinationBlend = Blend.One,
                ColorBlendFunction = BlendFunction.Add,
            };

            if (capacity > 0)
            {
                this.vertices = new VertexPosition2ColorTexture[capacity];
                this.indices = new int[capacity * 3];
            }
        }

        private readonly GraphicsDevice graphicsDevice;
        private readonly BasicEffect effect;
        private readonly BlendState blendState;
        private bool isInBeginEndPair = false;
        private VertexPosition2ColorTexture[] vertices;
        private int numVertices;
        private int[] indices;
        private int primitiveCount;
        private Texture2D texture;
        private Matrix world;
        private bool isDisposed;

        public void Begin(Matrix world)
        {
            if (this.isInBeginEndPair)
            {
               throw new InvalidOperationException("Begin cannot be called again until End has been successfully called.");
            }
            
            this.world = world;
            this.isInBeginEndPair = true;
        }

        public void DrawSpotLight(Light2D light)
        {
            if (light == null)
            {
                throw new ArgumentNullException(nameof(light));
            }
            this.ValidCheck();

            const int arcPoints = 36;
            float innerAngle = light.InnerAngle;
            float outerAngle = light.OuterAngle;
            bool isCircle = (innerAngle == outerAngle);

            if (innerAngle <= outerAngle)
            {
                innerAngle += 360f;
            }
            var radStart = MathHelper.ToRadians(outerAngle);
            var radEnd = MathHelper.ToRadians(innerAngle);
            float rotationIncrement = (radEnd - radStart) / arcPoints;

            // angle: up=0, right=90, down=180, left=-90
            // x=sin(d), y=-cos(d)

            var center = new Vector2(light.X, light.Y);
            var color = light.Color;
            var radiusSteps = light.InnerRadius == 0 ? new float[] { light.OuterRadius } : new float[] { light.InnerRadius, light.OuterRadius };
            var colorSteps = light.InnerRadius == 0 ? new[] { Color.Transparent } : new[] { light.Color, Color.Transparent };
            int numPointsPerArc = arcPoints + (isCircle ? 0 : 1);

            int numVertices = 1 + numPointsPerArc * radiusSteps.Length;
            int primitiveCount = arcPoints * (radiusSteps.Length * 2 - 1);
            this.AcquireBuffer(numVertices, primitiveCount, null,
                out Span<VertexPosition2ColorTexture> vertexBuffer,
                out Span<int> indexBuffer,
                out int vertexIndexStart);
            int indexCur = 0;

            vertexBuffer[0] = new VertexPosition2ColorTexture(center, color);

            for (int i = 0; i < arcPoints; i++)
            {
                var angle = radStart + rotationIncrement * i;
                Vector2 direction = new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));
                vertexBuffer[i + 1] = new VertexPosition2ColorTexture(center + direction * radiusSteps[0], colorSteps[0]);
                indexBuffer[indexCur++] = 0;
                indexBuffer[indexCur++] = i + 1;
                indexBuffer[indexCur++] = i + 2;

                for (int s = 1; s < radiusSteps.Length; s++)
                {
                    vertexBuffer[i + 1 + numPointsPerArc] = new VertexPosition2ColorTexture(center + direction * radiusSteps[s], colorSteps[s]);
                    int baseVertexIndex = i + numPointsPerArc * (s - 1) + 1;
                    indexBuffer[indexCur++] = baseVertexIndex;
                    indexBuffer[indexCur++] = baseVertexIndex + numPointsPerArc;
                    indexBuffer[indexCur++] = baseVertexIndex + 1;
                    indexBuffer[indexCur++] = baseVertexIndex + numPointsPerArc;
                    indexBuffer[indexCur++] = baseVertexIndex + 1 + numPointsPerArc;
                    indexBuffer[indexCur++] = baseVertexIndex + 1;
                }
            }

            if (isCircle)
            {
                int lastLoopStartIndex = 3 * (arcPoints - 1) * (radiusSteps.Length * 2 - 1);
                indexBuffer[lastLoopStartIndex + 2] = 1;
                for (int s = 1; s < radiusSteps.Length; s++)
                {
                    int baseIndex = lastLoopStartIndex + 3 + (s - 1) * 6;
                    indexBuffer[baseIndex + 2] = 1 + numPointsPerArc * (s - 1);
                    indexBuffer[baseIndex + 4] = 1 + numPointsPerArc * s;
                    indexBuffer[baseIndex + 5] = indexBuffer[baseIndex + 2];
                }
            }
            else
            {
                Vector2 direction = new Vector2((float)Math.Sin(radEnd), -(float)Math.Cos(radEnd));
                vertexBuffer[numPointsPerArc] = new VertexPosition2ColorTexture(center + direction * radiusSteps[0], colorSteps[0]);

                for (int s = 1; s < radiusSteps.Length; s++)
                {
                    vertexBuffer[numPointsPerArc * (s + 1)] = new VertexPosition2ColorTexture(center + direction * radiusSteps[s], colorSteps[s]);
                }
            }

            // indexBuffer add vertex offset
            for (int i = 0; i < indexBuffer.Length; i++)
            {
                indexBuffer[i] += vertexIndexStart;
            }
        }

        public void DrawTextureLight(Texture2D texture, Vector2 position, Rectangle? srcRect = default, Vector2 origin = default, bool flipX = false, Color? color = default)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }
            this.ValidCheck();

            this.AcquireBuffer(4, 2, texture,
               out Span<VertexPosition2ColorTexture> vertexBuffer,
               out Span<int> indexBuffer,
               out int vertexIndexStart);

            var sourceRect = srcRect ?? new Rectangle(0, 0, texture.Width, texture.Height);
            float srcLeft, srcTop, srcRight, srcBottom;
            float dstLeft, dstTop, dstRight, dstBottom;
            if (flipX)
            {
                srcLeft = (float)sourceRect.Right / texture.Width;
                srcRight = (float)sourceRect.Left / texture.Width;
                dstRight = position.X + origin.X;
                dstLeft = dstRight - sourceRect.Width;
            }
            else
            {
                srcLeft = (float)sourceRect.Left / texture.Width;
                srcRight = (float)sourceRect.Right / texture.Width;
                dstLeft = position.X - origin.X;
                dstRight = dstLeft + sourceRect.Width;
            }
            srcTop = (float)sourceRect.Top / texture.Height;
            srcBottom = (float)sourceRect.Bottom / texture.Height;
            dstTop = position.Y - origin.Y;
            dstBottom = dstTop + sourceRect.Height;

            Color vertexColor = color ?? Color.White;
            vertexBuffer[0] = new VertexPosition2ColorTexture(new Vector2(dstLeft, dstTop), vertexColor, new Vector2(srcLeft, srcTop));
            vertexBuffer[1] = new VertexPosition2ColorTexture(new Vector2(dstRight, dstTop), vertexColor, new Vector2(srcRight, srcTop));
            vertexBuffer[2] = new VertexPosition2ColorTexture(new Vector2(dstLeft, dstBottom), vertexColor, new Vector2(srcLeft, srcBottom));
            vertexBuffer[3] = new VertexPosition2ColorTexture(new Vector2(dstRight, dstBottom), vertexColor, new Vector2(srcRight, srcBottom));
            indexBuffer[0] = vertexIndexStart + 0;
            indexBuffer[1] = vertexIndexStart + 1;
            indexBuffer[2] = vertexIndexStart + 2;
            indexBuffer[3] = vertexIndexStart + 2;
            indexBuffer[4] = vertexIndexStart + 1;
            indexBuffer[5] = vertexIndexStart + 3;
        }

        public void End()
        {
            if (!this.isInBeginEndPair)
            {
                throw new InvalidOperationException("Begin must be called before calling End.");
            }

            this.ValidCheck();
            this.Flush();
            this.isInBeginEndPair = false;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    this.effect?.Dispose();
                    this.blendState?.Dispose();
                }

                this.vertices = null;
                this.indices = null;
                isDisposed = true;
            }
        }

        private void ValidCheck()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(LightRenderer));
            }
            if (!this.isInBeginEndPair)
            {
                throw new InvalidOperationException("Begin must be called successfully before you can call Draw.");
            }
        }

        private void AcquireBuffer(int vertexCount, int primitiveCount, Texture2D texture, out Span<VertexPosition2ColorTexture> vertexBuffer, out Span<int> indexBuffer, out int vertexIndexStart)
        {
            int requireIndexBufferSize = primitiveCount * 3;
            if (this.vertices == null || this.indices == null)
            {
                this.vertices = new VertexPosition2ColorTexture[vertexCount];
                this.indices = new int[requireIndexBufferSize];
                vertexBuffer = this.vertices;
                indexBuffer = this.indices;
                vertexIndexStart = 0;
                this.numVertices = vertexCount;
                this.primitiveCount = primitiveCount;
                return;
            }

            if (this.vertices.Length - this.numVertices < vertexCount 
                || this.indices.Length - this.primitiveCount * 3 < requireIndexBufferSize
                || this.texture != texture)
            {
                this.Flush();

                // if still not enough, resize buffer
                if (this.vertices.Length < vertexCount)
                {
                    Array.Resize(ref this.vertices, vertexCount);
                }
                if (this.indices.Length < requireIndexBufferSize)
                {
                    Array.Resize(ref this.indices, requireIndexBufferSize);
                }
            }

            vertexBuffer = this.vertices.AsSpan().Slice(this.numVertices, vertexCount);
            indexBuffer = this.indices.AsSpan().Slice(this.primitiveCount * 3, requireIndexBufferSize);
            vertexIndexStart = this.numVertices;
            this.numVertices += vertexCount;
            this.primitiveCount += primitiveCount;
            this.texture = texture;
        }

        private void Flush()
        {
            if (this.vertices == null || this.indices == null || this.numVertices == 0 || this.primitiveCount == 0)
            {
                return;
            }

            Rectangle viewPort = this.graphicsDevice.Viewport.Bounds;
            this.effect.World = this.world;
            this.effect.Projection = Matrix.CreateOrthographicOffCenter(viewPort, 1, 0);
            if (this.texture != null)
            {
                this.effect.Texture = this.texture;
                this.effect.TextureEnabled = true;
            }
            else
            {
                this.effect.Texture = null;
                this.effect.TextureEnabled = false;
            }
            this.graphicsDevice.BlendState = this.blendState;
            this.graphicsDevice.RasterizerState = RasterizerState.CullNone;

            foreach (var pass in this.effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                this.graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, this.vertices, 0, this.numVertices, this.indices, 0, this.primitiveCount);
            }

            // reset buffer
            Array.Clear(this.vertices, 0, this.numVertices);
            Array.Clear(this.indices, 0, this.primitiveCount * 3);
            this.numVertices = 0;
            this.primitiveCount = 0;
            this.effect.Texture = null;
            this.texture = null;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct VertexPosition2ColorTexture : IVertexType
        {
            public static readonly int Size = 16;

            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

            public Vector2 Position;
            public Color Color;
            public Vector2 TextureCoordinate;

            public VertexPosition2ColorTexture(Vector2 position, Color color) : this(position, color, Vector2.Zero)
            {
            }

            public VertexPosition2ColorTexture(Vector2 position, Color color, Vector2 texcoord)
            {
                Position = position;
                Color = color;
                TextureCoordinate = texcoord;
            }

            public override string ToString() => $"{{position: {this.Position}, color: {this.Color}, texcoord: {this.TextureCoordinate}}}";

            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
        }
    }
}

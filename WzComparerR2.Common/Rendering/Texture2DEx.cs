using System;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D11;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace WzComparerR2.Rendering
{
    public static class Texture2DEx
    {
        public static Texture2D Create_BC7(GraphicsDevice graphicsDevice, int width, int height)
        {
            var t2d = new Texture2D(graphicsDevice, width, height, false, SurfaceFormatEx.BC7);

            Texture2DDescription description = new Texture2DDescription
            {
                Width = t2d.Width,
                Height = t2d.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.BC7_UNorm,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription()
                {
                    Count = 1,
                    Quality = 0,
                }
            };
            var _device = t2d.GraphicsDevice._d3dDevice();

            var _textureField = typeof(Texture).GetField("_texture", BindingFlags.Instance | BindingFlags.NonPublic);

            Resource texture = new SharpDX.Direct3D11.Texture2D(_device, description);
            Resource oldTexture = _textureField.GetValue(t2d) as Resource;
            SharpDX.Utilities.Dispose(ref oldTexture);
            _textureField.SetValue(t2d, texture);

            return t2d;
        }

        public static unsafe void SetDataBC7(this Texture2D texture, Span<byte> data)
        {
            if (texture.Format != SurfaceFormatEx.BC7)
                throw new ArgumentException($"{nameof(SetDataBC7)} can only be used for BC7 format texture.", nameof(texture));

            int w = texture.Width;
            int h = texture.Height;
            w = (w + 3) & ~3;
            h = (h + 3) & ~3;
            int pitch = w * 4; //(width+3)/4*16
            int subresourceIndex = 0;
            int expectedDataSize = w * h; // (w/4)*(h/4)*16
            if (data.Length < expectedDataSize)
                throw new ArgumentException($"Incorrect data length ({data.Length} < {expectedDataSize}).", nameof(data));

            fixed (byte* pData = data)
            {
                var dataPtr = new IntPtr(pData);
                var region = new ResourceRegion();
                region.Top = 0;
                region.Front = 0;
                region.Back = 1;
                region.Bottom = h;
                region.Left = 0;
                region.Right = w;

                var d3dContext = texture.GraphicsDevice._d3dContext();
                lock (d3dContext)
                    d3dContext.UpdateSubresource(texture.GetTexture(), subresourceIndex, region, dataPtr, pitch, 0);
            }
        }
    }
}

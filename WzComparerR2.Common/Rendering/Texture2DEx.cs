using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D11;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace WzComparerR2.Rendering
{
    public static class Texture2DEx
    {
        public static Texture2D CreateEx(GraphicsDevice graphicsDevice, int width, int height, SurfaceFormat format)
        {
            SharpDX.DXGI.Format dxgiFormat = format switch
            {
                SurfaceFormatEx.BC7 => SharpDX.DXGI.Format.BC7_UNorm,
                SurfaceFormatEx.R16 => SharpDX.DXGI.Format.R16_UNorm,
                _ => throw new ArgumentException($"Unsupported texture format {format}")
            };
            var texture = new Texture2D(graphicsDevice, width, height, false, format);
            TextureInitialize(texture, dxgiFormat);
            return texture;
        }

        public static unsafe void SetDataEx(this Texture2D texture, ReadOnlySpan<byte> data, int pitch)
        {
            var region = new Rectangle(0, 0, texture.Width, texture.Height);
            int expectedDataSize = texture.Format switch
            {
                SurfaceFormatEx.BC7 => pitch * (region.Height / 4), // (w/4)*(h/4)*16
                SurfaceFormatEx.R16 => region.Height * pitch,
                _ => throw new ArgumentException($"Unsupported texture format {texture.Format}")
            };

            if (data.Length < expectedDataSize)
                throw new ArgumentException($"Incorrect data length ({data.Length} < {expectedDataSize}).", nameof(data));
            TextureSetData(texture, region, data, pitch);
        }

        private static void TextureInitialize(Texture2D texture2D, SharpDX.DXGI.Format format)
        {
            Texture2DDescription description = new Texture2DDescription
            {
                Width = texture2D.Width,
                Height = texture2D.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = format,
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
            var _device = texture2D.GraphicsDevice._d3dDevice();
            var _textureField = typeof(Texture).GetField("_texture", BindingFlags.Instance | BindingFlags.NonPublic);
            Resource dx11Texture = new SharpDX.Direct3D11.Texture2D(_device, description);
            Resource oldTexture = _textureField.GetValue(texture2D) as Resource;
            _textureField.SetValue(texture2D, dx11Texture);
            SharpDX.Utilities.Dispose(ref oldTexture);
        }

        private static unsafe void TextureSetData(Texture2D texture2D, Rectangle region, ReadOnlySpan<byte> data, int pitch)
        {
            fixed (byte* pData = data)
            {
                var dataPtr = new IntPtr(pData);
                var resourceRegion = new ResourceRegion()
                {
                    Top = region.Top,
                    Front = 0,
                    Back = 1,
                    Bottom = region.Bottom,
                    Left = region.Left,
                    Right = region.Right,
                };
                var d3dContext = texture2D.GraphicsDevice._d3dContext();
                lock (d3dContext)
                    d3dContext.UpdateSubresource(texture2D.GetTexture(), 0, resourceRegion, dataPtr, pitch, 0);
            }
        }
    }
}

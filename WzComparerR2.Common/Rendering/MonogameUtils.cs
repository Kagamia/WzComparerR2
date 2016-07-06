using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GdipColor = System.Drawing.Color;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D11;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace WzComparerR2.Rendering
{
    public static class MonogameUtils
    {
        public static Color ToXnaColor(this GdipColor color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        public static Texture2D CreateMosaic(GraphicsDevice device, Color c0, Color c1, int blockSize)
        {
            var t2d = new Texture2D(device, blockSize * 2, blockSize * 2, false, SurfaceFormat.Color);
            Color[] colorData = new Color[blockSize * blockSize * 4];
            int offset = blockSize * blockSize * 2;
            for (int i = 0; i < blockSize; i++)
            {
                colorData[i] = c0;
                colorData[blockSize + i] = c1;
                colorData[offset + i] = c1;
                colorData[offset + blockSize + i] = c0;
            }
            for (int i = 1; i < blockSize; i++)
            {
                Array.Copy(colorData, 0, colorData, blockSize * 2 * i, blockSize * 2);
                Array.Copy(colorData, offset, colorData, offset + blockSize * 2 * i, blockSize * 2);
            }
            t2d.SetData(colorData);
            return t2d;
        }

        public static Texture2D CreateTexture_BGRA4444(GraphicsDevice graphicsDevice, int width, int height)
        {
            var t2d = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Bgra4444);

            Texture2DDescription description = new Texture2DDescription
            {
                Width = t2d.Width,
                Height = t2d.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = (SharpDX.DXGI.Format)115, //DXGI_FORMAT_B4G4R4A4_UNORM
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None
            };
            var _device = (Device)typeof(GraphicsDevice)
                .GetField("_d3dDevice", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(t2d.GraphicsDevice);

            var _textureField = typeof(Texture)
                .GetField("_texture", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            description.SampleDescription.Count = 1;
            description.SampleDescription.Quality = 0;
            description.Usage = ResourceUsage.Default;
            description.OptionFlags = ResourceOptionFlags.None;

            Resource res = new SharpDX.Direct3D11.Texture2D(_device, description);
            Resource oldRes = _textureField.GetValue(t2d) as Resource;
            SharpDX.Utilities.Dispose(ref oldRes);
            _textureField.SetValue(t2d, res);

            return t2d;
        }

        public static void BgraToColor(byte[] pixelData)
        {
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                byte temp = pixelData[i];
                pixelData[i] = pixelData[i + 2];
                pixelData[i + 2] = temp;
            }
        }
    }
}

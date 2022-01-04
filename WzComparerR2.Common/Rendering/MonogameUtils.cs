using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using GdipColor = System.Drawing.Color;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D11;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using System.IO;

namespace WzComparerR2.Rendering
{
    public static class MonogameUtils
    {
        internal const SharpDX.DXGI.Format DXGI_FORMAT_B4G4R4A4_UNORM = (SharpDX.DXGI.Format)115;

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

        public static Texture2D ToTexture(this System.Drawing.Bitmap bitmap, GraphicsDevice device)
        {
            var t2d = new Texture2D(device, bitmap.Width, bitmap.Height, false, SurfaceFormat.Bgra32);
            bitmap.ToTexture(t2d, Point.Zero);
            return t2d;
        }

        public static void ToTexture(this System.Drawing.Bitmap bitmap, Texture2D texture, Point origin)
        {
            var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] buffer = new byte[bmpData.Stride * bmpData.Height];
            Marshal.Copy(bmpData.Scan0, buffer, 0, buffer.Length);
            bitmap.UnlockBits(bmpData);

            texture.SetData(0, 0, new Rectangle(origin.X, origin.Y, rect.Width, rect.Height), buffer, 0, buffer.Length);
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

        public static Device _d3dDevice(this GraphicsDevice device)
        {
            return (Device)device.Handle;
        }

        public static bool IsSupportFormat(this GraphicsDevice device, SharpDX.DXGI.Format format)
        {
            var d3dDevice = device._d3dDevice();
            var fmtSupport = d3dDevice.CheckFormatSupport(format);
            return (fmtSupport & SharpDX.Direct3D11.FormatSupport.Texture2D) != 0;
        }

        public static bool IsSupportBgra4444(this GraphicsDevice device)
        {
            return device.IsSupportFormat(DXGI_FORMAT_B4G4R4A4_UNORM);
        }

        public static bool IsSupportBgr565(this GraphicsDevice device)
        {
            return device.IsSupportFormat(SharpDX.DXGI.Format.B5G6R5_UNorm);
        }

        public static bool IsSupportBgra5551(this GraphicsDevice device)
        {
            return device.IsSupportFormat(SharpDX.DXGI.Format.B5G5R5A1_UNorm);
        }
    }
}

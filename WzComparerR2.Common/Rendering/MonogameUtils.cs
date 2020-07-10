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

        public static Texture2D CreateTexture_BGRA4444(GraphicsDevice graphicsDevice, int width, int height)
        {
            var t2d = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Bgra4444);

            Texture2DDescription description = new Texture2DDescription
            {
                Width = t2d.Width,
                Height = t2d.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT_B4G4R4A4_UNORM,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None
            };
            var _device = t2d.GraphicsDevice._d3dDevice();

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

        public static void SaveAsPng(this Texture2D texture, Stream stream)
        {
            switch (texture.Format)
            {
                case SurfaceFormat.Bgra4444:
                    byte[] data = new byte[texture.Width * texture.Height * 2];
                    texture.GetTexture_BGRA4444(data);
                    data = WzLib.Wz_Png.GetPixelDataBgra4444(data, texture.Width, texture.Height);
                    unsafe
                    {
                        fixed (byte* pData = data)
                        {
                            using (var bmp = new System.Drawing.Bitmap(texture.Width, texture.Height, texture.Width * 4,
                                System.Drawing.Imaging.PixelFormat.Format32bppArgb, new IntPtr(pData)))
                            {
                                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                    }
                    break;

                default:
                    texture.SaveAsPng(stream, texture.Width, texture.Height);
                    break;
            }
        }

        public static void GetTexture_BGRA4444<T>(this Texture2D texture, T[] data) where T : struct
        {
            texture.GetTexture_BGRA4444<T>(0, 0, null, data, 0, data.Length);
        }

        public static void GetTexture_BGRA4444<T>(this Texture2D texture, int level, int arraySlice, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            int num = Math.Max(texture.Width >> level, 1);
            int num2 = Math.Max(texture.Height >> level, 1);
            Texture2DDescription description = new Texture2DDescription
            {
                Width = num,
                Height = num2,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT_B4G4R4A4_UNORM,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read
            };
            description.SampleDescription.Count = 1;
            description.SampleDescription.Quality = 0;
            description.Usage = ResourceUsage.Staging;
            description.OptionFlags = ResourceOptionFlags.None;

            DeviceContext context = texture.GraphicsDevice._d3dContext();

            using (SharpDX.Direct3D11.Texture2D textured = new SharpDX.Direct3D11.Texture2D(texture.GraphicsDevice._d3dDevice(), description))
            {
                lock (context)
                {
                    int width;
                    int height;
                    SharpDX.DataStream stream;
                    int sourceSubresource = 0;
                    if (rect.HasValue)
                    {
                        width = rect.Value.Width;
                        height = rect.Value.Height;
                        context.CopySubresourceRegion(texture.GetTexture(), sourceSubresource, new ResourceRegion(rect.Value.Left, rect.Value.Top, 0, rect.Value.Right, rect.Value.Bottom, 1), textured, 0, 0, 0, 0);
                    }
                    else
                    {
                        width = num;
                        height = texture.Height;
                        context.CopySubresourceRegion(texture.GetTexture(), sourceSubresource, null, textured, 0, 0, 0, 0);
                    }
                    SharpDX.DataBox box = context.MapSubresource(textured, 0, MapMode.Read, MapFlags.None, out stream);
                    int num7 = 2 * width;
                    if (num7 == box.RowPitch)
                    {
                        stream.ReadRange<T>(data, startIndex, elementCount);
                    }
                    else
                    {
                        stream.Seek((long)startIndex, SeekOrigin.Begin);
                        int num8 = Marshal.SizeOf(typeof(T));
                        for (int i = 0; i < height; i++)
                        {
                            int index = (i * num7) / num8;
                            while (index < (((i + 1) * num7) / num8))
                            {
                                data[index] = stream.Read<T>();
                                index++;
                            }
                            if (index >= elementCount)
                            {
                                break;
                            }
                            stream.Seek((long)(box.RowPitch - num7), SeekOrigin.Current);
                        }
                    }
                    stream.Dispose();
                }
            }
        }

        private static DeviceContext _d3dContext(this GraphicsDevice device)
        {
            return (DeviceContext)typeof(GraphicsDevice)
                .GetField("_d3dContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(device);
        }

        public static Device _d3dDevice(this GraphicsDevice device)
        {
            return (Device)device.Handle;
        }

        private static Resource GetTexture(this Texture texture)
        {
            return (Resource)typeof(Texture)
                .GetMethod("GetTexture", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(texture, new object[] { });
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

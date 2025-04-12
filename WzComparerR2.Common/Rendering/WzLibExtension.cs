using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Buffers;

namespace WzComparerR2.Rendering
{
    public static class WzLibExtension
    {
        public static Texture2D ToTexture(this Wz_Png png, GraphicsDevice graphicsDevice)
        {
            var format = GetTextureFormatOfPng(png.Format);
            if (format == SurfaceFormat.Bgra4444)
            {
                //检测是否支持 pre-win8
                if (!graphicsDevice.IsSupportBgra4444())
                {
                    format = SurfaceFormat.Bgra32;
                }
            }
            else if (format == SurfaceFormat.Bgr565)
            {
                //检测是否支持 pre-win8
                if (!graphicsDevice.IsSupportBgr565())
                {
                    format = SurfaceFormat.Bgra32;
                }
            }
            else if (format == SurfaceFormat.Bgra5551)
            {
                //检测是否支持 pre-win8
                if (!graphicsDevice.IsSupportBgra5551())
                {
                    format = SurfaceFormat.Bgra32;
                }
            }

            Texture2D t2d;
            if (format == SurfaceFormatEx.BC7)
            {
                t2d = Texture2DEx.Create_BC7(graphicsDevice, png.Width, png.Height);
            }
            else
            {
                t2d = new Texture2D(graphicsDevice, png.Width, png.Height, false, format);
            }
            png.ToTexture(t2d, Point.Zero);
            return t2d;
        }

        public static void ToTexture(this Wz_Png png, Texture2D texture, Point origin)
        {
            Rectangle rect = new Rectangle(origin, new Point(png.Width, png.Height));

            //检查大小
            if (rect.X < 0 || rect.Y < 0 || rect.Right > texture.Width || rect.Bottom > texture.Height)
            {
                throw new ArgumentException("Png rectangle is out of bounds.");
            }

            if (texture.Format == SurfaceFormat.Bgra32 && png.Format != Wz_TextureFormat.ARGB8888)
            {
                // soft decoding
                using (var bmp = png.ExtractPng())
                {
                    bmp.ToTexture(texture, origin);
                }
            }
            else if (texture.Format != GetTextureFormatOfPng(png.Format))
            {
                throw new ArgumentException($"Texture format({texture.Format}) does not fit the png form({png.Form}).");
            }
            else
            {
                int bufferSize = png.GetRawDataSize();
                byte[] plainData = ArrayPool<byte>.Shared.Rent(bufferSize);
                int actualBytes = png.GetRawData(plainData.AsSpan(0, bufferSize));
                if (actualBytes != bufferSize)
                {
                    throw new ArgumentException($"Not enough bytes have been read. (actual:{actualBytes}, expected:{bufferSize})");
                }

                switch (png.Form)
                {
                    case 1:
                    case 2:
                    case 257:
                    case 513:
                    case 1026:
                    case 2050:
                    case 2562:
                        texture.SetData(0, 0, rect, plainData, 0, bufferSize);
                        break;

                    case 3:
                        var pixel = Wz_Png.GetPixelDataForm3(plainData, png.Width, png.Height);
                        texture.SetData(0, 0, rect, pixel, 0, pixel.Length);
                        break;

                    case 517:
                        pixel = Wz_Png.GetPixelDataForm517(plainData, png.Width, png.Height);
                        texture.SetData(0, 0, rect, pixel, 0, pixel.Length);
                        break;

                    case 4098:
                        texture.SetDataBC7(plainData.AsSpan(0, bufferSize));
                        break;

                    default:
                        throw new Exception($"unknown png form ({png.Form}).");
                }

                ArrayPool<byte>.Shared.Return(plainData);
            }
        }

        public static SurfaceFormat GetTextureFormatOfPng(Wz_TextureFormat textureFormat)
        {
            switch (textureFormat)
            {
                case Wz_TextureFormat.ARGB4444: return SurfaceFormat.Bgra4444;
                case Wz_TextureFormat.ARGB8888: return SurfaceFormat.Bgra32;
                case Wz_TextureFormat.ARGB1555: return SurfaceFormat.Bgra5551;
                case Wz_TextureFormat.RGB565: return SurfaceFormat.Bgr565;
                case Wz_TextureFormat.DXT3: return SurfaceFormat.Dxt3;
                case Wz_TextureFormat.DXT5: return SurfaceFormat.Dxt5;
                case Wz_TextureFormat.A8: return SurfaceFormat.Alpha8;
                case Wz_TextureFormat.RGBA1010102: return SurfaceFormat.Rgba1010102;
                case Wz_TextureFormat.DXT1: return SurfaceFormat.Dxt1;
                case Wz_TextureFormat.BC7: return SurfaceFormatEx.BC7;
                case Wz_TextureFormat.RGBA32Float: return SurfaceFormat.Vector4;

                default: return SurfaceFormat.Bgra32;
            }
        }

        public static Point ToPoint(this Wz_Vector vector)
        {
            return new Point(vector.X, vector.Y);
        }
    }
}

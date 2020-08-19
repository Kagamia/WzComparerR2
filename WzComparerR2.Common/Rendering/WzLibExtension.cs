using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Rendering
{
    public static class WzLibExtension
    {
        public static Texture2D ToTexture(this Wz_Png png, GraphicsDevice graphicsDevice)
        {
            var format = GetTextureFormatOfPng(png.Form);
            Texture2D t2d = null;
            if (format == SurfaceFormat.Bgra4444)
            {
                //检测是否支持 pre-win8
                if (graphicsDevice.IsSupportBgra4444())
                {
                    t2d = MonogameUtils.CreateTexture_BGRA4444(graphicsDevice, png.Width, png.Height);
                }
                else
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

            if (t2d == null)
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

            //检查像素格式
            var format = GetTextureFormatOfPng(png.Form);

            if (texture.Format == SurfaceFormat.Bgra32)
            {
                using (var bmp = png.ExtractPng())
                {
                    bmp.ToTexture(texture, origin);
                }
            }
            else if (texture.Format != format)
            {
                throw new ArgumentException($"Texture format({texture.Format}) does not fit the png form({png.Form}).");
            }
            else
            {
                byte[] plainData = png.GetRawData();
                if (plainData == null)
                {
                    throw new Exception("png decoding failed.");
                }

                switch (png.Form)
                {
                    case 1:
                    case 2:
                    case 257:
                    case 513:
                    case 1026:
                    case 2050:
                        texture.SetData(0, 0, rect, plainData, 0, plainData.Length);
                        break;

                    case 3:
                        var pixel = Wz_Png.GetPixelDataForm3(plainData, png.Width, png.Height);
                        texture.SetData(0, 0, rect, pixel, 0, pixel.Length);
                        break;

                    case 517:
                        pixel = Wz_Png.GetPixelDataForm517(plainData, png.Width, png.Height);
                        texture.SetData(0, 0, rect, pixel, 0, pixel.Length);
                        break;

                    default:
                        throw new Exception($"unknown png form ({png.Form}).");
                }
            }
        }

        public static SurfaceFormat GetTextureFormatOfPng(int pngform)
        {
            switch (pngform)
            {
                case 1: return SurfaceFormat.Bgra4444;
                case 2:
                case 3: return SurfaceFormat.Bgra32;
                case 257: return SurfaceFormat.Bgra5551;
                case 513: 
                case 517: return SurfaceFormat.Bgr565;
                case 1026: return SurfaceFormat.Dxt3;
                case 2050: return SurfaceFormat.Dxt5;
                default: return SurfaceFormat.Bgra32;
            }
        }


        public static Point ToPoint(this Wz_Vector vector)
        {
            return new Point(vector.X, vector.Y);
        }
    }
}

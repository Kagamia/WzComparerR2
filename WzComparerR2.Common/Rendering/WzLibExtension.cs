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
            byte[] plainData = png.GetRawData();
            if (plainData == null)
            {
                return null;
            }

            Texture2D t2d;

            switch (png.Form)
            {
                case 1:
                    t2d = null;
                    try
                    {
                        t2d = MonogameUtils.CreateTexture_BGRA4444(graphicsDevice, png.Width, png.Height);
                        t2d.SetData(plainData);
                    }
                    catch  //monogame并不支持这个format 用gdi+转
                    {
                        if (t2d != null) t2d.Dispose();
                        goto default;
                    }
                    break;

                case 2:
                    t2d = new Texture2D(graphicsDevice, png.Width, png.Height, false, SurfaceFormat.Bgra32);
                    t2d.SetData(plainData);
                    break;

                case 513:
                    t2d = new Texture2D(graphicsDevice, png.Width, png.Height, false, SurfaceFormat.Bgr565);
                    t2d.SetData(plainData);
                    break;

                case 517:
                    t2d = new Texture2D(graphicsDevice, png.Width, png.Height, false, SurfaceFormat.Bgr565);
                    byte[] texData = new byte[png.Width * png.Height * 2];
                    for (int j0 = 0, j1 = png.Height / 16; j0 < j1; j0++)
                    {
                        int idxTex = j0 * 16 * png.Width * 2;
                        for (int i0 = 0, i1 = png.Width / 16; i0 < i1; i0++)
                        {
                            int idx = (i0 + j0 * i1) * 2;

                            for (int k = 0; k < 16; k++)
                            {
                                texData[idxTex + i0 * 32 + k * 2] = plainData[idx];
                                texData[idxTex + i0 * 32 + k * 2 + 1] = plainData[idx + 1];
                            }
                        }
                        for (int k = 1; k < 16; k++)
                        {
                            System.Buffer.BlockCopy(texData, idxTex, texData, idxTex + k * png.Width * 2, png.Width * 2);
                        }
                    }
                    t2d.SetData(texData);
                    break;

                case 1026:
                    t2d = new Texture2D(graphicsDevice, png.Width, png.Height, false, SurfaceFormat.Dxt3);
                    t2d.SetData(plainData);
                    break;

                case 2050:
                    t2d = new Texture2D(graphicsDevice, png.Width, png.Height, false, SurfaceFormat.Dxt5);
                    t2d.SetData(plainData);
                    break;

                default: //默认从bitmap复制 二次转换
                    var bitmap = png.ExtractPng();
                    t2d = bitmap.ToTexture(graphicsDevice);
                    bitmap.Dispose();
                    break;

            }

            return t2d;
        }

        public static Point ToPoint(this Wz_Vector vector)
        {
            return new Point(vector.X, vector.Y);
        }
    }
}

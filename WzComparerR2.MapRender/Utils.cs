using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender
{
    static class Utils
    {
        public static Texture2D BitmapToTexture(GraphicsDevice graphicsDevice, Bitmap bitmap)
        {
            Texture2D texture = new Texture2D(graphicsDevice, bitmap.Width, bitmap.Height, 1, TextureUsage.None, SurfaceFormat.Color);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] buffer = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            texture.SetData<byte>(buffer);
            bitmap.UnlockBits(data);
            return texture;
        }
    }
}

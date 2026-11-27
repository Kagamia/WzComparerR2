using System;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>RGBA8 image (the caller decides whether rows are top-down or bottom-up).</summary>
    public sealed class UnityRgbaImage
    {
        public UnityRgbaImage(int width, int height, byte[] rgba)
        {
            if (rgba == null)
            {
                throw new ArgumentNullException(nameof(rgba));
            }
            if (width <= 0 || height <= 0 || rgba.Length != width * height * 4)
            {
                throw new UnityException(string.Format(
                    "RGBA image length mismatch: {0}x{1} requires {2} bytes but got {3}",
                    width, height, (long)width * height * 4, rgba.Length));
            }
            this.Width = width;
            this.Height = height;
            this.Rgba = rgba;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] Rgba { get; private set; }

        public byte[] ToPng()
        {
            return UnityTexture.ToPng(this.Width, this.Height, this.Rgba);
        }
    }
}

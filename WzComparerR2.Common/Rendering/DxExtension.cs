using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Rendering
{
    public static class DxExtension
    {
        public static Color DxToXnaColor(this SharpDX.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        public static SharpDX.Color XnaToDxColor(this Color color)
        {
            return SharpDX.Color.FromRgba(color.PackedValue);
        }

        public static SharpDX.RectangleF XnaToDxRect(this Rectangle rect)
        {
            return new SharpDX.RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}

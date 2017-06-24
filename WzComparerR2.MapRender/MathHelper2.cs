using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    public static class MathHelper2
    {
        public static Vector2 Round(Vector2 vector2)
        {
            var rounding = MidpointRounding.AwayFromZero;
            return new Vector2((float)Math.Round(vector2.X, rounding), (float)Math.Round(vector2.Y, rounding));
        }

        public static Rectangle Transform(Rectangle rectangle, Matrix matrix)
        {
            Vector2 lt = new Vector2(rectangle.Left, rectangle.Top);
            Vector2 rb = new Vector2(rectangle.Right, rectangle.Bottom);
            Vector2.Transform(ref lt, ref matrix, out lt);
            Vector2.Transform(ref rb, ref matrix, out rb);
            return new Rectangle((int)lt.X, (int)lt.Y, (int)(rb.X - lt.X), (int)(rb.Y - lt.Y));
        }

        public static float Max(params float[] values)
        {
            if (values != null && values.Length > 0)
            {
                float maxValue = values[0];
                for (int i = 1; i < values.Length; i++)
                {
                    maxValue = Math.Max(maxValue, values[i]);
                }
                return maxValue;
            }
            else
            {
                return 0;
            }
        }

        public static Color Lerp(Color[] colors, float amount)
        {
            if (colors == null || colors.Length <= 0)
            {
                return Color.TransparentBlack;
            }
            if (colors.Length == 1)
            {
                return colors[0];
            }
            amount = amount % (colors.Length - 1);
            int index = (int)amount;
            amount = amount - index;
            return new Color(
                (byte)MathHelper.Lerp(colors[index].R, colors[index + 1].R, amount),
                (byte)MathHelper.Lerp(colors[index].G, colors[index + 1].G, amount),
                (byte)MathHelper.Lerp(colors[index].B, colors[index + 1].B, amount),
                (byte)MathHelper.Lerp(colors[index].A, colors[index + 1].A, amount)
                );
        }

        public static Color HSVtoColor(float hue, float saturation, float value)
        {
            Vector3 color = Vector3.Zero;
            if (saturation == 0)
            {
                color = new Vector3(value);
            }
            else
            {
                hue /= 60;
                var i = (int)hue;
                var f = hue - i;
                var a = value * (1 - saturation);
                var b = value * (1 - saturation * f);
                var c = value * (1 - saturation * (1 - f));
                switch (i)
                {
                    case 0: color = new Vector3(value, c, a); break;
                    case 1: color = new Vector3(b, value, a); break;
                    case 2: color = new Vector3(a, value, c); break;
                    case 3: color = new Vector3(a, b, value); break;
                    case 4: color = new Vector3(c, a, value); break;
                    case 5: color = new Vector3(value, a, b); break;
                }
            }
            return new Color(color);
        }
    }
}

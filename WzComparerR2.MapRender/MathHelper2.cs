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
    }
}

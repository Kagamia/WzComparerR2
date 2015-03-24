using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    internal static class Methods
    {
        public static IEnumerable<char> CreateCharEnumerator(string text, int startIndex, int length)
        {
            if (text == null || startIndex < 0)
                yield break;
            for (int i = startIndex, j = Math.Min(text.Length, startIndex + length); i < j; i++)
            {
                yield return text[i];
            }
        }

        public static IEnumerable<char> CreateCharEnumerator(StringBuilder stringBuilder, int startIndex, int length)
        {
            if (stringBuilder == null)
                yield break;
            for (int i = startIndex, j = Math.Min(stringBuilder.Length, startIndex + length); i < j; i++)
            {
                yield return stringBuilder[i];
            }
        }

        public static Color LerpColor(Color[] colors, float amount)
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
    }
}

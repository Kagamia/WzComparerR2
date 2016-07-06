using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WzComparerR2.Rendering
{
    internal static class TextUtils
    {
        public static IEnumerable<char> CreateCharEnumerator(string text, int startIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                yield return text[startIndex + i];
            }
        }

        public static IEnumerable<char> CreateCharEnumerator(StringBuilder stringBuilder, int startIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                yield return stringBuilder[startIndex + i];
            }
        }
    }
}

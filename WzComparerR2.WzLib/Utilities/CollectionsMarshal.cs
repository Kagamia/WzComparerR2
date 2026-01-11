using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WzComparerR2.WzLib.Utilities
{
#if NETFRAMEWORK
    internal static class CollectionsMarshal
    {
        public static Span<T> AsSpan<T>(List<T> list)
        {
            Span<T> result = default(Span<T>);
            if (list != null)
            {
                int size = list.Count;
                T[] items = typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(list) as T[];
                if (items == null || (uint)size > (uint)items.Length)
                {
                    throw new InvalidOperationException();
                }
                return items.AsSpan(0, size);
            }
            return result;
        }
    }
#endif
}

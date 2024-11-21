using System;

namespace WzComparerR2.WzLib.Utilities
{
    public interface IWzStringPool
    {
        bool TryGet(long offset, out string s);
        string GetOrAdd(long offset, ReadOnlySpan<Char> chars);
        void Reset();
    }
}

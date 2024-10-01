using System;
using System.Collections.Generic;
using System.Linq;

namespace WzComparerR2.WzLib.Utilities
{
    internal class SimpleWzStringPool : IWzStringPool
    {
        public SimpleWzStringPool() 
        {
            this.stringTable = new Dictionary<long, string>();
            this.asciiStringTable = new Dictionary<long, string>();
        }

        private Dictionary<long, string> stringTable;
        private Dictionary<long, string> asciiStringTable;

        public string GetOrAdd(long offset, ReadOnlySpan<char> chars)
        {
            if (this.TryGet(offset, out string s))
            {
                if (!s.AsSpan().SequenceEqual(chars))
                {
                    throw new Exception("The cached string does not match the input string");
                }
            }
            else if (chars == null || chars.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                // usually wz won't compress string with length less than 8 into reference.
                if (chars.Length < 8 && IsAsciiString(chars))
                {
                    long hash = 0;
                    for (int i = 0; i < chars.Length; i++)
                    {
                        hash |= (long)(chars[i] & 0xff) << (i * 8);
                    }
                    if (!this.asciiStringTable.TryGetValue(hash, out s))
                    {
                        s = chars.ToString();
                        this.asciiStringTable.Add(hash, s);
                    }
                }
                else
                {
                    s = chars.ToString();
                }
                this.stringTable.Add(offset, s);
            }
            return s;
        }

        public bool TryGet(long offset, out string s)
        {
            return this.stringTable.TryGetValue(offset, out s);
        }

        public void Reset()
        {
            this.stringTable.Clear();
        }

        private static bool IsAsciiString(ReadOnlySpan<char> chars)
        {
            for(int i = 0; i < chars.Length; i++)
            {
                if ((uint)(chars[i] - 0x20) >= 0x60)
                {
                    return false;
                }
            }
            return true;
        }
    }
}

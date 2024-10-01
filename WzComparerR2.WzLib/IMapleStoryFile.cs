using System;
using System.IO;

namespace WzComparerR2.WzLib
{
    public interface IMapleStoryFile : IDisposable
    {
        public Wz_Structure WzStructure { get; }
        public Stream FileStream { get; }
        public object ReadLock { get; }
    }
}

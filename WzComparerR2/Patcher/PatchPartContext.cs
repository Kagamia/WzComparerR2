using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Patcher
{
    public class PatchPartContext
    {
        public PatchPartContext(string fileName, long offset, int type)
        {
            this.Offset = offset;
            this.FileName = fileName;
            this.Type = type;
        }

        private readonly HashSet<string> dependencyFiles = new HashSet<string>();

        public long Offset { get; set; }
        public string FileName { get; private set; }
        public int Type { get; private set; }
        public int NewFileLength { get; set; }
        public uint? OldChecksum { get; set; }
        public uint? OldChecksumActual { get; set; }
        public uint NewChecksum { get; set; }
        public string TempFilePath { get; set; }
        public string OldFilePath { get; set; }
        public int Action0 { get; set; }
        public int Action1 { get; set; }
        public int Action2 { get; set; }
        public ISet<string> DependencyFiles { get; private set; } = new HashSet<string>();
    }
}

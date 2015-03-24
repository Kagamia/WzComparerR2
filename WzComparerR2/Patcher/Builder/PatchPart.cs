using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Patcher.Builder
{
    public class PatchPart
    {
        public PatchPart()
        {
            this.Type = PatchType.Unknown;
        }

        public string FileName { get; set; }
        public PatchType Type { get; set; }
        public int FileLength { get; set; }
        public uint Checksum { get; set; }
        public uint OldChecksum { get; set; }
    }
}

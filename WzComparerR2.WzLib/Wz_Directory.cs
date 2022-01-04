using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WzComparerR2.WzLib
{
    public class Wz_Directory
    {
        public Wz_Directory(string name, int size, int cs32, uint hashOff, uint hashPos, Wz_File wz_f)
        {
            this.Name = name;
            this.WzFile = wz_f;
            this.Size = size;
            this.Checksum = cs32;
            this.HashedOffset = hashOff;
            this.HashedOffsetPosition = hashPos;
        }

        public string Name { get; set; }
        public Wz_File WzFile { get; set; }
        public int Size { get; set; }
        public int Checksum { get; set; }
        public uint HashedOffset { get; set; }
        public uint HashedOffsetPosition { get; set; }
        public long Offset { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.WzLib
{
    public class Ms_Entry
    {
        public Ms_Entry(string name, int checkSum, int flags, int startPos, int size, int sizeAligned, int unk1, int unk2, byte[] key)
        {
            this.Name = name;
            this.CheckSum = checkSum;
            this.Flags = flags;
            this.StartPos = startPos;
            this.Size = size;
            this.SizeAligned = sizeAligned;
            this.Unknown1 = unk1;
            this.Unknown2 = unk2;
            this.Key = key;
        }

        public string Name { get; internal set; }
        public int CheckSum { get; internal set; }
        public int Flags { get; internal set; }
        public long StartPos { get; internal set; }
        public int Size { get; internal set; }
        public int SizeAligned { get; internal set; }
        public int Unknown1 { get; internal set; }
        public int Unknown2 { get; internal set; }
        public byte[] Key { get; internal set; }
    }
}

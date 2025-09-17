using System;

namespace WzComparerR2.WzLib
{
    public class Ms_Entry
    {
        public Ms_Entry(string name, int checkSum, int flags, int startPos, int size, int sizeAligned, int unk1, int unk2, byte[] key)
            : this(name, checkSum, flags, startPos, size, sizeAligned, unk1, unk2, key, 0, 0)
        {
        }

        public Ms_Entry(string name, int checkSum, int flags, int startPos, int size, int sizeAligned, int unk1, int unk2, byte[] key, int unk3, int unk4)
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
            this.Unknown3 = unk3;
            this.Unknown4 = unk4;
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
        public int Unknown3 { get; internal set; }  // for ms file v2 only
        public int Unknown4 { get; internal set; }  // for ms file v2 only

        public int CalculatedCheckSum { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.WzLib
{
    public class Wz_RawData
    {
        public Wz_RawData(uint offset, int length, Wz_Image wz_Image)
        {
            this.Offset = offset;
            this.Length = length;
            this.WzImage = wz_Image;
        }

        public uint Offset { get; set; }
        public int Length { get; set; }
        public Wz_Image WzImage { get; set; }
        public Wz_File WzFile => this.WzImage?.WzFile;
    }
}

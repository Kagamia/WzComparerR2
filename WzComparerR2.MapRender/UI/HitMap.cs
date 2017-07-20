using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.MapRender.UI
{
    class HitMap
    {
        public HitMap(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.bitArray = new byte[this.Stride * this.Height];
        }

        public HitMap(bool defaultHit)
        {
            this.DefaultHit = defaultHit;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool DefaultHit { get; set; }

        public int Stride
        {
            get
            {
                return (this.Width + 7) / 8;
            }
        }

        private byte[] bitArray;

        public bool this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= this.Width || y < 0 || y >= this.Height || this.bitArray == null)
                {
                    return this.DefaultHit;
                }
                return bitArray[this.Stride * y + x / 8] >> (x % 8) != 0;
            }
            set
            {
                if (x < 0 || x >= this.Width || y < 0 || y >= this.Height || this.bitArray == null)
                {
                    return;
                }
                int i = this.Stride * y + x / 8;
                if (value)
                {
                    bitArray[i] |= (byte)(1 << (x % 8));
                }
                else
                {
                    bitArray[i] &= (byte)~(1 << (x % 8));
                }
            }
        }

        public void SetRow(int y, bool[] isHit)
        {
            int index = 0;
            for (int i = 0; i < this.Stride; i++)
            {
                int val = 0;
                for (int j = 0; j < 8; j++)
                {
                    if (index < isHit.Length)
                    {
                        if (isHit[index])
                        {
                            val |= 1 << j;
                        }
                        index++;
                    }
                }
                this.bitArray[this.Stride * y + i] = (byte)val;
            }
        }
    }
}

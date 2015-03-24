using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WzComparerR2.WzLib
{
    public class Wz_Vector
    {
        public Wz_Vector(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        private int x;
        private int y;

        /// <summary>
        /// 获取或设置向量的X值。
        /// </summary>
        public int X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        /// 获取或设置向量的Y值。
        /// </summary>
        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        public static implicit operator Point(Wz_Vector vector)
        {
            return vector == null ? new Point() : new Point(vector.x, vector.y);
        }
    }
}

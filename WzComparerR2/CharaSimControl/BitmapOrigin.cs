using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public struct BitmapOrigin
    {
        public BitmapOrigin(Bitmap bitmap)
            : this(bitmap, new Point(0, 0))
        {
        }

        public BitmapOrigin(Bitmap bitmap, int x, int y)
            : this(bitmap, new Point(x, y))
        {
        }

        public BitmapOrigin(Bitmap bitmap, Point origin)
        {
            this.bitmap = bitmap;
            this.origin = origin;
        }

        public BitmapOrigin(Wz_Node node)
        {
            Wz_Png png = node.GetValue<Wz_Png>(null);
            //---2013-12-04 by kagamia---
            Wz_Node sourceNode = node.Nodes["source"];
            if (sourceNode != null)
            {
                string source = sourceNode.GetValue<string>();
                if (!string.IsNullOrEmpty(source))
                {
                    sourceNode = WzComparerR2.PluginBase.PluginManager.FindWz(source);
                    if (sourceNode != null)
                    {
                        png = sourceNode.GetValue<Wz_Png>();
                    }
                }
            }
            //---------------------------
            this.bitmap = (png == null) ? null : png.ExtractPng();
            Wz_Node originNode = node.FindNodeByPath("origin");
            Wz_Vector vec = (originNode == null) ? null : originNode.GetValue<Wz_Vector>();
            this.origin = (vec == null) ? new Point() : new Point(vec.X, vec.Y);
        }

        private Bitmap bitmap;
        private Point origin;

        /// <summary>
        /// 获取图片。
        /// </summary>
        public Bitmap Bitmap
        {
            get { return bitmap; }
            set { bitmap = value; }
        }

        /// <summary>
        /// 获取或设置图片的原点坐标。
        /// </summary>
        public Point Origin
        {
            get { return origin; }
            set { origin = value; }
        }

        /// <summary>
        /// 获取图片原点的相反数，一般为绘图坐标区域的实际绘图原点。
        /// </summary>
        public Point OpOrigin
        {
            get { return new Point(-origin.X, -origin.Y); }
        }

        /// <summary>
        /// 获取图片的实际绘图区域，它由图片大小和原点的相反数组成。
        /// </summary>
        public Rectangle Rectangle
        {
            get
            {
                if (this.bitmap == null)
                    return new Rectangle(this.OpOrigin, new Size());
                else
                    return new Rectangle(this.OpOrigin, this.bitmap.Size);
            }
        }
    }
}

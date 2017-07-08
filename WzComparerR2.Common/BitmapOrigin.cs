using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.WzLib;
using WzComparerR2.Common;

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

        public static BitmapOrigin CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            BitmapOrigin bp = new BitmapOrigin();
            Wz_Uol uol;
            while ((uol = node.GetValue<Wz_Uol>(null)) != null)
            {
                node = uol.HandleUol(node);
            }

            //获取linkNode
            var linkNode = node.GetLinkedSourceNode(findNode);
            Wz_Png png = linkNode?.GetValue<Wz_Png>() ?? (Wz_Png)node.Value;

            bp.Bitmap = png?.ExtractPng();
            Wz_Node originNode = node.FindNodeByPath("origin");
            Wz_Vector vec = (originNode == null) ? null : originNode.GetValue<Wz_Vector>();
            bp.Origin = (vec == null) ? new Point() : new Point(vec.X, vec.Y);

            return bp;
        }
    }
}

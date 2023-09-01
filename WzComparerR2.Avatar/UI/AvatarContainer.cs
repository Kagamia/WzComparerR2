using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace WzComparerR2.Avatar.UI
{
    public partial class AvatarContainer : Control
    {
        public AvatarContainer()
        {
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.  
            SetStyle(ControlStyles.DoubleBuffer, true); // 双缓冲  
            bmpCache = new Dictionary<string, BitmapOrigin[]>();
        }

        /// <summary>
        /// 获取或设置纸娃娃的绘图原点。
        /// </summary>
        public Point Origin { get; set; }

        //绘图相关
        Dictionary<string, BitmapOrigin[]> bmpCache;
        string currentKey;
        Bitmap bg;

        //事件相关
        bool isDragging;
        Point lastPressingPoint;

        public void ClearAllCache()
        {
            this.bmpCache.Clear();
        }

        public void AddCache(string key, BitmapOrigin[] layers)
        {
            this.bmpCache[key] = layers;
        }

        public bool HasCache(string key)
        {
            return this.bmpCache.ContainsKey(key);
        }

        public void SetKey(string key)
        {
            this.currentKey = key;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Graphics g = pe.Graphics;
            DrawBackgrnd(g);

            BitmapOrigin[] layers;
            if (currentKey != null && this.bmpCache.TryGetValue(currentKey, out layers) && layers != null)
            {
                foreach(var bmp in layers)
                {
                    if (bmp.Bitmap != null)
                    {
                        Point point = new Point(this.Origin.X - bmp.Origin.X, this.Origin.Y - bmp.Origin.Y);
                        g.DrawImage(bmp.Bitmap, point);
                    }
                }
               
            }

            base.OnPaint(pe);
        }

        private void DrawBackgrnd(Graphics g)
        {
            if (bg == null)
            {
                bg = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics g1 = Graphics.FromImage(bg);
                g1.FillRectangle(Brushes.LightGray, bg.Width / 2, 0, bg.Width / 2, bg.Height / 2);
                g1.FillRectangle(Brushes.LightGray, 0, bg.Height / 2, bg.Width / 2, bg.Height / 2);
                g1.Dispose();
            }
            TextureBrush b = new TextureBrush(bg, WrapMode.Tile);
            g.FillRectangle(b, new Rectangle(Point.Empty, this.Size));
            b.Dispose();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if ((e.Button & System.Windows.Forms.MouseButtons.Left) == System.Windows.Forms.MouseButtons.Left)
            {
                isDragging = true;
                lastPressingPoint = e.Location;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDragging)
            {
                int dx = e.X - lastPressingPoint.X;
                int dy = e.Y - lastPressingPoint.Y;
                this.Origin = new Point(Origin.X + dx, Origin.Y + dy);
                this.lastPressingPoint = new Point(e.X, e.Y);
                this.Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if ((e.Button & System.Windows.Forms.MouseButtons.Left) == System.Windows.Forms.MouseButtons.Left)
            {
                isDragging = false;
            }
            base.OnMouseUp(e);
        }
    }
}

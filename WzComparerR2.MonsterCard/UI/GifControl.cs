using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using WzComparerR2.Common;

namespace WzComparerR2.MonsterCard.UI
{
    public partial class GifControl : Control
    {
        public GifControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.  
            SetStyle(ControlStyles.DoubleBuffer, true); // 双缓冲  
        }

        /// <summary>
        /// 获取或设置GIF的绘图原点。
        /// </summary>
        public Point Origin { get; set; }

        /// <summary>
        /// 获取或设置控件正在播放的动画。
        /// </summary>
        public Gif AnimateGif
        {
            get
            {
                return this.gif;
            }
            set
            {
                this.gif = value;
                this.Play();
            }
        }

        Bitmap bg;
        Gif gif;
        int currentFrame;


        //事件相关
        bool isDragging;
        Point lastPressingPoint;

        protected override void OnPaint(PaintEventArgs pe)
        {
            Graphics g = pe.Graphics;
            DrawBackgrnd(g);
            if (gif != null && currentFrame > -1 && currentFrame < gif.Frames.Count)
            {
                var frame = gif.Frames[currentFrame];
                if (frame != null)
                {
                    frame.Draw(g, new Rectangle(-this.Origin.X, -this.Origin.Y, this.Width, this.Height));
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

        private void Play()
        {
            this.timer1.Stop();
            if (this.gif == null || this.gif.Frames.Count <= 0)
            {
                return;
            }
            PrepareFrame();
            this.timer1.Start();
        }

        private void Stop()
        {
            this.timer1.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            currentFrame++;
            PrepareFrame();
        }

        private void PrepareFrame()
        {
            if (this.gif == null || this.gif.Frames.Count <= 1)
            {
                Stop();
                currentFrame = 0;
            }
            else
            {
                currentFrame = currentFrame % this.gif.Frames.Count;
                int delay = this.gif.Frames[currentFrame].Delay;
                this.timer1.Interval = delay <= 0 ? 1 : delay;
            }
            this.Invalidate();
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace WzComparerR2.CharaSimControl
{
    public class ACtrlButton : AControl
    {
        public ACtrlButton()
        {
            this.Visible = true;
        }

        private BitmapOrigin normal;
        private BitmapOrigin pressed;
        private BitmapOrigin mouseOver;
        private BitmapOrigin disabled;

        private ButtonState state;

        public BitmapOrigin Normal
        {
            get { return normal; }
            set { normal = value; }
        }

        public BitmapOrigin Pressed
        {
            get { return pressed; }
            set { pressed = value; }
        }

        public BitmapOrigin MouseOver
        {
            get { return mouseOver; }
            set { mouseOver = value; }
        }

        public BitmapOrigin Disabled
        {
            get { return disabled; }
            set { disabled = value; }
        }

        public ButtonState State
        {
            get { return state; }
            set
            {
                if (state != value)
                {
                    state = value;
                    OnButtonStateChanged();
                }
            }
        }

        public BitmapOrigin CurrentBitmap
        {
            get
            {
                switch (this.state)
                {
                    default:
                    case ButtonState.Normal: return this.normal;
                    case ButtonState.Pressed: return this.pressed;
                    case ButtonState.MouseOver: return this.mouseOver;
                    case ButtonState.Disabled: return this.disabled;
                }
            }
        }

        /// <summary>
        /// 应用当前的ButtonState和Location，绘制对应的图像。
        /// </summary>
        /// <param Name="g">要绘制的绘图表面。</param>
        public override void Draw(Graphics g)
        {
            this.Draw(g, new Point(0, 0));
        }

        /// <summary>
        /// 应用当前的ButtonState、Location以及给定的坐标偏移，绘制对应的图像。
        /// </summary>
        /// <param Name="g">要绘制的绘图表面。</param>
        /// <param Name="Offset">表示对于绘图原点的坐标偏移。</param>
        public void Draw(Graphics g, Point offset)
        {
            if (g == null || !this.Visible)
                return;
            BitmapOrigin bmp = this.CurrentBitmap;
            if (bmp.Bitmap != null)
                g.DrawImage(bmp.Bitmap, bmp.OpOrigin.X + this.Location.X + offset.X, bmp.OpOrigin.Y + this.Location.Y + offset.Y);
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            if (this.IsMouseContains(e.Location))
            {
                if (this.State == ButtonState.Normal)
                {
                    this.State = ButtonState.MouseOver;
                }
            }
            else if (this.state != ButtonState.Disabled)
            {
                this.State = ButtonState.Normal;
            }

            base.OnMouseMove(e);
        }

        public override void OnMouseDown(MouseEventArgs e)
        {
            if (this.IsMouseContains(e.Location))
            {
                if (this.State == ButtonState.Normal || this.State == ButtonState.MouseOver)
                {
                    this.State = ButtonState.Pressed;
                }
            }
            base.OnMouseDown(e);
        }

        public override void OnMouseUp(MouseEventArgs e)
        {
            if (this.IsMouseContains(e.Location))
            {
                if (this.State == ButtonState.Pressed)
                {
                    this.State = ButtonState.MouseOver;
                }
            }

            base.OnMouseUp(e);
        }

        protected virtual void OnButtonStateChanged()
        {
            if (this.ButtonStateChanged != null)
            {
                this.ButtonStateChanged(this, EventArgs.Empty);
            }
        }

        public override void OnMouseClick(MouseEventArgs e)
        {
            if (this.state != ButtonState.Disabled)
            {
                base.OnMouseClick(e);
            }
        }

        public event EventHandler ButtonStateChanged;
    }
}

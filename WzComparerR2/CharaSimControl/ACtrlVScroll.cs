using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace WzComparerR2.CharaSimControl
{
    public class ACtrlVScroll : AControl
    {
        public ACtrlVScroll()
        {
            this.btnPrev = new ACtrlButton();
            this.btnNext = new ACtrlButton();
            this.btnThumb = new ACtrlButton();
            this.picBase = new ACtrlButton();
            this.btnPrev.ButtonStateChanged += new EventHandler(childBtn_ButtonStateChanged);
            this.btnPrev.MouseClick += new MouseEventHandler(btnPrev_MouseClick);
            this.btnNext.ButtonStateChanged += new EventHandler(childBtn_ButtonStateChanged);
            this.btnNext.MouseClick += new MouseEventHandler(btnNext_MouseClick);
            this.btnThumb.ButtonStateChanged += new EventHandler(childBtn_ButtonStateChanged);
            this.picBase.ButtonStateChanged += new EventHandler(childBtn_ButtonStateChanged);
        }

        ACtrlButton btnPrev;
        ACtrlButton btnNext;
        ACtrlButton btnThumb;
        ACtrlButton picBase;
        int minimum;
        int maximum;
        int value;
        bool isScrolling;

        public int Minimum
        {
            get { return minimum; }
            set
            {
                if (this.minimum != value)
                {
                    value = Math.Min(value, maximum);
                    minimum = value;
                    this.Value = this.Value;
                    this.OnValueChanged();
                }
            }
        }

        public int Maximum
        {
            get { return maximum; }
            set
            {
                if (this.maximum != value)
                {
                    value = Math.Max(minimum, value);
                    maximum = value;
                    this.Value = this.Value;
                    this.OnValueChanged();
                }
            }
        }

        public int Value
        {
            get
            {
                return this.value;
            }
            set
            {
                value = Math.Min(Math.Max(this.minimum, value), this.maximum);
                if (this.value != value)
                {
                    this.value = value;
                    this.OnValueChanged();
                }
            }
        }

        public ACtrlButton BtnPrev
        {
            get { return btnPrev; }
        }

        public ACtrlButton BtnNext
        {
            get { return btnNext; }
        }

        public ACtrlButton BtnThumb
        {
            get { return btnThumb; }
        }

        public ACtrlButton PicBase
        {
            get { return picBase; }
        }

        public bool Enabled
        {
            get { return !(this.minimum == this.maximum); }
        }

        private Point? scrollableLocation;
        private Size? scrollableSize;

        public Point? ScrollableLocation
        {
            get { return scrollableLocation; }
            set { scrollableLocation = value; }
        }

        public Size? ScrollableSize
        {
            get { return scrollableSize; }
            set { scrollableSize = value; }
        }

        public Rectangle ScrollableRectangle
        {
            get { return new Rectangle(this.scrollableLocation ?? this.Location, this.scrollableSize ?? this.Size); }
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            if (!this.Enabled)
            {
                setAllState(ButtonState.Disabled);
                return;
            }

            if (IsMouseContains(e.Location))
            {
                MouseEventArgs e2 = ToChildEventargs(e);

                foreach (ACtrlButton btn in this.buttons)
                {
                    btn.OnMouseMove(e2);
                }
                if (this.isScrolling)
                {
                    this.scrolling(e.Location);
                }
            }
            else
            {
                setAllState(ButtonState.Normal);
            }
            
            base.OnMouseMove(e);
        }

        public override void OnMouseDown(MouseEventArgs e)
        {
            if (!this.Enabled)
            {
                setAllState(ButtonState.Disabled);
                return;
            }

            if (IsMouseContains(e.Location))
            {
                this.isScrolling = true;
                MouseEventArgs e2 = ToChildEventargs(e);

                foreach (ACtrlButton btn in this.buttons)
                {
                    btn.OnMouseDown(e2);
                }
            }

            base.OnMouseDown(e);
        }

        public override void OnMouseUp(MouseEventArgs e)
        {
            if (!this.Enabled)
            {
                setAllState(ButtonState.Disabled);
                return;
            }

            if (IsMouseContains(e.Location))
            {
                MouseEventArgs e2 = ToChildEventargs(e);

                foreach (ACtrlButton btn in this.buttons)
                {
                    btn.OnMouseUp(e2);
                }

            }
            this.isScrolling = false;
            base.OnMouseUp(e);
        }

        public override void OnMouseClick(MouseEventArgs e)
        {
            if (!this.Enabled)
            {
                setAllState(ButtonState.Disabled);
                return;
            }

            if (IsMouseContains(e.Location))
            {
                MouseEventArgs e2 = ToChildEventargs(e);

                foreach (ACtrlButton btn in this.buttons)
                {
                    btn.OnMouseClick(e2);
                }
            }

            base.OnMouseClick(e);
        }

        public override void OnMouseWheel(MouseEventArgs e)
        {
            if (!this.Enabled)
            {
                setAllState(ButtonState.Disabled);
                return;
            }
            if (this.IsScrollableMouseContains(e.Location))
            {
                if (e.Delta > 0)
                {
                    this.Value -= 1;
                }
                else if (e.Delta < 0)
                {
                    this.Value += 1;
                }
            }

            base.OnMouseWheel(e);
        }

        private bool IsScrollableMouseContains(Point mouseLocation)
        {
            return this.Visible && this.ScrollableRectangle.Contains(mouseLocation);
        }

        private void btnPrev_MouseClick(object sender, MouseEventArgs e)
        {
            this.Value -= 1;
        }

        private void btnNext_MouseClick(object sender, MouseEventArgs e)
        {
            this.Value += 1;
        }

        private void setAllState(ButtonState buttonState)
        {
            btnPrev.State = buttonState;
            btnNext.State = buttonState;
            btnThumb.State = buttonState;
            picBase.State = buttonState;
        }

        private IEnumerable<ACtrlButton> buttons
        {
            get
            {
                yield return btnPrev;
                yield return btnNext;
                yield return btnThumb;
            }
        }

        public override void Draw(Graphics g)
        {
            if (g == null || !this.Visible)
                return;
            if (picBase != null)
            {
                BitmapOrigin curBmp = picBase.CurrentBitmap;
                g.SetClip(this.Rectangle);
                for (int h = 0; h < Size.Height; h += curBmp.Bitmap.Size.Height)
                {
                    g.DrawImage(curBmp.Bitmap, Location.X, Location.Y + h);
                }
                g.ResetClip();
            }
            if (btnPrev != null)
            {
                btnPrev.Draw(g, this.Location);
            }
            if (btnNext != null)
            {
                btnNext.Draw(g, this.Location);
            }
            if (btnThumb != null)
            {
                btnThumb.Location = new Point(0, calcThumbLocationY());
                btnThumb.Draw(g, this.Location);
            }
        }

        private int calcThumbLocationY()
        {
            if (this.minimum == this.maximum)
                return 0;
            int totalHeight = this.Size.Height - this.btnPrev.Size.Height - this.btnNext.Size.Height - this.btnThumb.Size.Height;
            int thumbY = totalHeight * this.value / (this.maximum - this.minimum);
            return thumbY + this.btnPrev.Size.Height;
        }

        private void childBtn_ButtonStateChanged(object sender, EventArgs e)
        {
            this.OnChildButtonStateChanged();
        }

        private void scrolling(Point mouseLocation)
        {
            if (this.minimum == this.maximum)
                return;
            Point origin = new Point(this.Location.X, this.Location.Y + this.btnPrev.Size.Height + this.btnThumb.Size.Height / 2);
            Size size = new Size(this.Size.Width, this.Size.Height - this.btnPrev.Size.Height - this.btnNext.Size.Height - this.btnThumb.Size.Height);
            Rectangle scrollingRect = new Rectangle(origin, size);
            this.Value = (int)Math.Round(1.0 * (this.maximum - this.minimum) * (mouseLocation.Y - scrollingRect.Y) / scrollingRect.Height);
        }

        protected virtual void OnValueChanged()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnChildButtonStateChanged()
        {
            if (this.ChildButtonStateChanged != null)
            {
                this.ChildButtonStateChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler ValueChanged;
        public event EventHandler ChildButtonStateChanged;
    }
}

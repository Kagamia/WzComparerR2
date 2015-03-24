using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace WzComparerR2.CharaSimControl
{
    public abstract class AControl
    {
        public AControl()
        {
        }

        private Point location;
        private Size size;
        private bool visible;

        public Point Location
        {
            get { return location; }
            set { location = value; }
        }

        public Size Size
        {
            get { return size; }
            set { size = value; }
        }

        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        public Rectangle Rectangle
        {
            get { return new Rectangle(this.location, this.size); }
        }

        public abstract void Draw(Graphics g);

        public virtual void OnMouseClick(MouseEventArgs e)
        {
            if (IsMouseContains(e.Location))
            {
                if (this.MouseClick != null)
                    this.MouseClick(this, e);
            }
        }

        public virtual void OnMouseDown(MouseEventArgs e)
        {
        }

        public virtual void OnMouseUp(MouseEventArgs e)
        {
        }

        public virtual void OnMouseMove(MouseEventArgs e)
        {
        }

        public virtual void OnMouseWheel(MouseEventArgs e)
        {
        }

        protected virtual bool IsMouseContains(Point mouseLocation)
        {
            return this.visible && this.Rectangle.Contains(mouseLocation);
        }

        protected MouseEventArgs ToChildEventargs(MouseEventArgs e)
        {
            return new MouseEventArgs(e.Button, e.Clicks, e.X - this.Location.X, e.Y - this.Location.Y, e.Delta);
        }

        public event MouseEventHandler MouseClick;
    }
}

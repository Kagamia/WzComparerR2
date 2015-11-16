using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace WzComparerR2.MonsterCard.UI
{
    class AnimationDrawArgs
    {
        public int OriginX { get; set; }

        public int OriginY { get; set; }

        public bool AdjustContainer { get; set; }

        private bool isDragging;
        private int lastPressingX;
        private int lastPressingY;

        public void RegisterEvents(Control container)
        {
            container.MouseDown += Container_MouseDown;
            container.MouseMove += Container_MouseMove;
            container.MouseUp += Container_MouseUp;
            container.SizeChanged += Container_SizeChanged;
        }

        public void UnRegisterEvents(Control container)
        {
            container.MouseDown -= Container_MouseDown;
            container.MouseMove -= Container_MouseMove;
            container.MouseUp -= Container_MouseUp;
            container.SizeChanged -= Container_SizeChanged;
        }

        private void Container_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & System.Windows.Forms.MouseButtons.Left) == System.Windows.Forms.MouseButtons.Left)
            {
                isDragging = true;
                lastPressingX = e.X;
                lastPressingY = e.Y;
            }
        }

        private void Container_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                int dx = e.X - lastPressingX;
                int dy = e.Y - lastPressingY;
                this.OriginX += dx;
                this.OriginY += dy;
                lastPressingX = e.X;
                lastPressingY = e.Y;
                (sender as Control).Invalidate();
            }
        }
        private void Container_MouseUp(object sender, MouseEventArgs e)
        {
            if ((e.Button & System.Windows.Forms.MouseButtons.Left) == System.Windows.Forms.MouseButtons.Left)
            {
                isDragging = false;
            }
        }

        private void Container_SizeChanged(object sender, EventArgs e)
        {

        }

    }
}

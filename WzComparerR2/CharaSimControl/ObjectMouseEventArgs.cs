using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class ObjectMouseEventArgs : MouseEventArgs
    {
        public ObjectMouseEventArgs(MouseEventArgs e, object obj)
            : this(e.Button, e.Clicks, e.X, e.Y, e.Delta, obj)
        {
        }

        public ObjectMouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta, object obj) :
            base(button, clicks, x, y, delta)
        {
            this.obj = obj;
        }

        private object obj;

        public object Obj
        {
            get { return obj; }
            set { obj = value; }
        }
    }
}
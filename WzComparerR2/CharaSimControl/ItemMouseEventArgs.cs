using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class ItemMouseEventArgs : MouseEventArgs
    {
        public ItemMouseEventArgs(MouseEventArgs e, ItemBase item)
            : this(e.Button, e.Clicks, e.X, e.Y, e.Delta, item)
        {
        }

        public ItemMouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta, ItemBase item) :
            base(button, clicks, x, y, delta)
        {
            this.item = item;
        }

        private ItemBase item;

        public ItemBase Item
        {
            get { return item; }
            set { item = value; }
        }
    }
}

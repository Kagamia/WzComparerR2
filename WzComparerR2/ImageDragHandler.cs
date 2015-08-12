using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using WzComparerR2.Common;

namespace WzComparerR2
{
    public class ImageDragHandler
    {
        public ImageDragHandler(PictureBox owner)
        {
            this.OwnerControl = owner;
            this.dragBox = Rectangle.Empty;
        }

        public PictureBox OwnerControl { get; private set; }

        private Rectangle dragBox;

        public void AttachEvents()
        {
            this.OwnerControl.MouseDown += OwnerControl_MouseDown;
            this.OwnerControl.MouseMove += OwnerControl_MouseMove;
            this.OwnerControl.MouseUp += OwnerControl_MouseUp;
        }

        void OwnerControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && OwnerControl.Image != null)
            {
                this.dragBox = new Rectangle(e.Location, SystemInformation.DragSize);
            }
        }

        void OwnerControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && OwnerControl.Image != null
                && this.dragBox != Rectangle.Empty && !this.dragBox.Contains(e.Location))
            {
                string fileName = Convert.ToString(OwnerControl.Tag);
                ImageDataObject dataObj = new ImageDataObject(OwnerControl.Image, fileName);
                var dragEff = this.OwnerControl.DoDragDrop(dataObj, DragDropEffects.Copy);
            }
        }

        void OwnerControl_MouseUp(object sender, MouseEventArgs e)
        {
            this.dragBox = Rectangle.Empty;
        }

    }
}

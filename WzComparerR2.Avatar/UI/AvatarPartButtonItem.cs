using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using System.Drawing.Imaging;

namespace WzComparerR2.Avatar.UI
{
    internal partial class AvatarPartButtonItem : ButtonItem
    {
        public AvatarPartButtonItem()
        {
            InitializeComponent();
        }

        public void SetIcon(Bitmap icon)
        {
            if (icon != null)
            {
                if (!this.ImageFixedSize.IsEmpty && icon.Size != this.ImageFixedSize)
                {
                    Bitmap newIcon = new Bitmap(this.ImageFixedSize.Width, this.ImageFixedSize.Height, PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(newIcon);
                    int x = (newIcon.Width - icon.Width) / 2;
                    int y = (newIcon.Height - icon.Height) / 2;
                    g.DrawImage(icon, x, y);
                    g.Dispose();
                    this.Image = newIcon;
                }
                else
                {
                    this.Image = icon;
                }
            }
            else
            {
                this.Image = null;
            }
        }
    }
}

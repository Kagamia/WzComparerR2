using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.Common;

namespace WzComparerR2.CharaSimControl
{
    public abstract class TooltipRender
    {
        static TooltipRender()
        {
            Rectangle screenRect = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            DefaultPicHeight = screenRect.Height * 2;
        }

        public TooltipRender()
        {
        }

        public static readonly int DefaultPicHeight;

        public StringLinker StringLinker { get; set; }

        public bool ShowObjectID { get; set; }

        public abstract Bitmap Render();

        public virtual object TargetItem { get; set; }
    }
}

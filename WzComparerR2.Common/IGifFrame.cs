using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WzComparerR2.Common
{
    public interface IGifFrame
    {
        Rectangle Region { get; }
        int Delay { get; }
        void Draw(Graphics g, Rectangle canvasRect);
    }
}

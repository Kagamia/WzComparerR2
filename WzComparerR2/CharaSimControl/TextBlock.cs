using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WzComparerR2.CharaSimControl
{
    public class TextBlock
    {
        public string Text { get; set; }
        public Brush Brush { get; set; }
        public Font Font { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
        public Rectangle Rectangle
        {
            get
            {
                return new Rectangle(Position, Size);
            }
        }
    }
}

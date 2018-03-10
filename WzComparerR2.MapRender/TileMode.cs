using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.MapRender
{
    [Flags]
    public enum TileMode
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        BothTile = Horizontal | Vertical,
        ScrollHorizontal = 4,
        ScrollVertical = 8
    }
}

#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    public interface ITooltip
    {
        Rectangle TooltipSenseRegion { get; }
        bool TooltipDisplayed { get; set; }
        string TooltipTitle { get; }
        string TooltipContent { get; }
    }
}
#endif
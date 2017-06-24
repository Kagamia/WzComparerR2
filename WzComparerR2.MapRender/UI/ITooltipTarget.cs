using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.UserInterface;

namespace WzComparerR2.MapRender.UI
{
    public interface ITooltipTarget
    {
        object GetTooltipTarget(PointF mouseLocation);
    }
}

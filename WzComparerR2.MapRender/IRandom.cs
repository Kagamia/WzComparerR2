using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    public interface IRandom
    {
        float NextVar(float baseValue, float varRange, bool nonNegative = false);
        int NextVar(int baseValue, int varRange, bool nonNegative = false);
        Vector2 NextVar(Vector2 baseValue, Vector2 varRange);
        Color NextVar(Color baseValue, Color varRange);
        int Next(int maxValue);
        bool NextPercent(float percent);
    }
}

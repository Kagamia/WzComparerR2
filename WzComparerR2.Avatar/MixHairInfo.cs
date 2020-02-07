using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WzComparerR2.Avatar
{
    public class MixHairInfo
    {
        public int MixHairColor;
        public int MixHairOpacity;
        public MixHairInfo(int _color, int _opacity)
        {
            MixHairColor = _color;
            MixHairOpacity = _opacity;
        }
        public override string ToString()
        {
            return string.Format("{0}_{1}", MixHairColor, MixHairOpacity);
        }
    }
}
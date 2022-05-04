using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.UserInterface.Media;

namespace WzComparerR2.MapRender.UI
{
    public class ColorWConverter
    {
        public static bool TryParse(string s, out ColorW colorW)
        {
            if (s != null)
            {
                if (s.Length == 6) //RGB
                {
                    if (uint.TryParse(s, System.Globalization.NumberStyles.HexNumber,null, out uint rgb))
                    {
                        colorW = new ColorW(rgb)
                        {
                            A = 255
                        };
                        return true;
                    }
                }
                else if (s.Length == 8) //ARGB
                {
                    if (uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out uint packedValue))
                    {
                        colorW = new ColorW(packedValue);
                        return true;
                    }
                }
            }

            colorW = ColorW.TransparentBlack;
            return false;
        }

        public static string ToString(ColorW colorW)
        {
            return colorW.PackedValue.ToString("x8");
        }
    }
}

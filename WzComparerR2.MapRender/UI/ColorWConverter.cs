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
                        colorW = new ColorW((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)(rgb), 0xff);
                        return true;
                    }
                }
                else if (s.Length == 8) //ARGB
                {
                    if (uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out uint argb))
                    {
                        colorW = new ColorW((byte)(argb >> 16), (byte)(argb >> 8), (byte)(argb), (byte)(argb>>24));
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

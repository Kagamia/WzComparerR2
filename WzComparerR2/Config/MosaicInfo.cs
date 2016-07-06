using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Drawing;

namespace WzComparerR2.Config
{
    public class MosaicInfo : ConfigurationElement
    {

        [ConfigurationProperty("color0")]
        public Color Color0
        {
            get { return (Color)this["color0"]; }
            set { this["color0"] = value; }
        }

        [ConfigurationProperty("color1")]
        public Color Color1
        {
            get { return (Color)this["color1"]; }
            set { this["color1"] = value; }
        }

        [ConfigurationProperty("blockSize")]
        public int BlockSize
        {
            get { return (int)this["blockSize"]; }
            set { this["blockSize"] = value; }
        }
    }
}

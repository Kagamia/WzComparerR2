using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class CharaSimMapConfig : ConfigurationElement
    {
        [ConfigurationProperty("showMiniMap", DefaultValue = true)]
        public bool ShowMiniMap
        {
            get { return (bool)this["showMiniMap"]; }
            set { this["showMiniMap"] = value; }
        }

        [ConfigurationProperty("showMiniMapMob", DefaultValue = true)]
        public bool ShowMiniMapMob
        {
            get { return (bool)this["showMiniMapMob"]; }
            set { this["showMiniMapMob"] = value; }
        }

        [ConfigurationProperty("showMiniMapNpc", DefaultValue = true)]
        public bool ShowMiniMapNpc
        {
            get { return (bool)this["showMiniMapNpc"]; }
            set { this["showMiniMapNpc"] = value; }
        }

        [ConfigurationProperty("showMiniMapPortal", DefaultValue = true)]
        public bool ShowMiniMapPortal
        {
            get { return (bool)this["showMiniMapPortal"]; }
            set { this["showMiniMapPortal"] = value; }
        }
    }
}
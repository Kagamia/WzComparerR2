using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using WzComparerR2.Config;

namespace WzComparerR2.MapRender.Config
{
    [SectionName("WcR2.MapRender")]
    public sealed class MapRenderConfig : ConfigSectionBase<MapRenderConfig>
    {
        public MapRenderConfig()
        {
            this.Volume = 1f;
            this.MuteOnLeaveFocus = true;
            this.ClipMapRegion = true;
        }

        [ConfigurationProperty("volume")]
        public ConfigItem<float> Volume
        {
            get { return (ConfigItem<float>)this["volume"]; }
            set { this["volume"] = value; }
        }

        [ConfigurationProperty("muteOnLeaveFocus")]
        public ConfigItem<bool> MuteOnLeaveFocus
        {
            get { return (ConfigItem<bool>)this["muteOnLeaveFocus"]; }
            set { this["muteOnLeaveFocus"] = value; }
        }

        [ConfigurationProperty("defaultFontIndex")]
        public ConfigItem<int> DefaultFontIndex
        {
            get { return (ConfigItem<int>)this["defaultFontIndex"]; }
            set { this["defaultFontIndex"] = value; }
        }

        [ConfigurationProperty("clipMapRegion")]
        public ConfigItem<bool> ClipMapRegion
        {
            get { return (ConfigItem<bool>)this["clipMapRegion"]; }
            set { this["clipMapRegion"] = value; }
        }

        [ConfigurationProperty("topBar.Visible")]
        public ConfigItem<bool> TopBarVisible
        {
            get { return (ConfigItem<bool>)this["topBar.Visible"]; }
            set { this["topBar.Visible"] = value; }
        }

        [ConfigurationProperty("minimap.CameraRegionVisible")]
        public ConfigItem<bool> Minimap_CameraRegionVisible
        {
            get { return (ConfigItem<bool>)this["minimap.CameraRegionVisible"]; }
            set { this["minimap.CameraRegionVisible"] = value; }
        }

        [ConfigurationProperty("worldMap.UseImageNameAsInfoName")]
        public ConfigItem<bool> WorldMap_UseImageNameAsInfoName
        {
            get { return (ConfigItem<bool>)this["worldMap.UseImageNameAsInfoName"]; }
            set { this["worldMap.UseImageNameAsInfoName"] = value; }
        }
    }
}

using System.Configuration;
using System.Windows.Forms;

namespace WzComparerR2.Config
{
    [SectionName("WcR2.UIState")]
    public sealed class UIStateConfig : ConfigSectionBase<UIStateConfig>
    {
        [ConfigurationProperty("windowState")]
        public ConfigItem<int> WindowState
        {
            get { return (ConfigItem<int>)this["windowState"]; }
            set { this["windowState"] = value; }
        }

        [ConfigurationProperty("windowWidth")]
        public ConfigItem<int> WindowWidth
        {
            get { return (ConfigItem<int>)this["windowWidth"]; }
            set { this["windowWidth"] = value; }
        }

        [ConfigurationProperty("windowHeight")]
        public ConfigItem<int> WindowHeight
        {
            get { return (ConfigItem<int>)this["windowHeight"]; }
            set { this["windowHeight"] = value; }
        }

        [ConfigurationProperty("ribbonExpanded")]
        public ConfigItem<bool> RibbonExpanded
        {
            get { return (ConfigItem<bool>)this["ribbonExpanded"]; }
            set { this["ribbonExpanded"] = value; }
        }

        [ConfigurationProperty("selectedRibbonTabIndex")]
        public ConfigItem<int> SelectedRibbonTabIndex
        {
            get { return (ConfigItem<int>)this["selectedRibbonTabIndex"]; }
            set { this["selectedRibbonTabIndex"] = value; }
        }

        [ConfigurationProperty("splitterPosition1")]
        public ConfigItem<int> SplitterPosition1
        {
            get { return (ConfigItem<int>)this["splitterPosition1"]; }
            set { this["splitterPosition1"] = value; }
        }

        [ConfigurationProperty("splitterPosition2")]
        public ConfigItem<int> SplitterPosition2
        {
            get { return (ConfigItem<int>)this["splitterPosition2"]; }
            set { this["splitterPosition2"] = value; }
        }

        [ConfigurationProperty("columnWidth3")]
        public ConfigItem<int> ColumnWidth3
        {
            get { return (ConfigItem<int>)this["columnWidth3"]; }
            set { this["columnWidth3"] = value; }
        }

        [ConfigurationProperty("columnWidth4")]
        public ConfigItem<int> ColumnWidth4
        {
            get { return (ConfigItem<int>)this["columnWidth4"]; }
            set { this["columnWidth4"] = value; }
        }

        [ConfigurationProperty("columnWidth5")]
        public ConfigItem<int> ColumnWidth5
        {
            get { return (ConfigItem<int>)this["columnWidth5"]; }
            set { this["columnWidth5"] = value; }
        }

        [ConfigurationProperty("columnWidth6")]
        public ConfigItem<int> ColumnWidth6
        {
            get { return (ConfigItem<int>)this["columnWidth6"]; }
            set { this["columnWidth6"] = value; }
        }

        [ConfigurationProperty("columnWidth7")]
        public ConfigItem<int> ColumnWidth7
        {
            get { return (ConfigItem<int>)this["columnWidth7"]; }
            set { this["columnWidth7"] = value; }
        }

        [ConfigurationProperty("columnWidth8")]
        public ConfigItem<int> ColumnWidth8
        {
            get { return (ConfigItem<int>)this["columnWidth8"]; }
            set { this["columnWidth8"] = value; }
        }

        [ConfigurationProperty("columnWidth9")]
        public ConfigItem<int> ColumnWidth9
        {
            get { return (ConfigItem<int>)this["columnWidth9"]; }
            set { this["columnWidth9"] = value; }
        }

        [ConfigurationProperty("barLayout")]
        public ConfigItem<string> BarLayout
        {
            get { return (ConfigItem<string>)this["barLayout"]; }
            set { this["barLayout"] = value; }
        }
    }
}

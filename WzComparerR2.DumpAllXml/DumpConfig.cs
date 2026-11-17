using System.Configuration;
using WzComparerR2.Config;

namespace WzComparerR2.DumpAllXml {
    [SectionName("WcR2.DumpAllXml")]
    public sealed class DumpConfig : ConfigSectionBase<DumpConfig> {

        [ConfigurationProperty("outputDirectory")]
        public ConfigItem<string> OutputDirectory {
            get { return (ConfigItem<string>)this["outputDirectory"]; }
            set { this["outputDirectory"] = value; }
        }

        [ConfigurationProperty("filterTypes")]
        public ConfigItem<string> FilterTypes {
            get { return (ConfigItem<string>)this["filterTypes"]; }
            set { this["filterTypes"] = value; }
        }

        [ConfigurationProperty("exportWzTypes")]
        public ConfigItem<string> ExportWzTypes {
            get { return (ConfigItem<string>)this["exportWzTypes"]; }
            set { this["exportWzTypes"] = value; }
        }

        [ConfigurationProperty("concurrency")]
        public ConfigItem<int> Concurrency {
            get { return (ConfigItem<int>)this["concurrency"]; }
            set { this["concurrency"] = value; }
        }
    }
}

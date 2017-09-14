using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using WzComparerR2.Config;

namespace WzComparerR2.Network
{
    [SectionName("WcR2.Network")]
    public sealed class NetworkConfig : ConfigSectionBase<NetworkConfig>
    {
        public NetworkConfig()
        {
            this.LogLevel = WzComparerR2.Network.LogLevel.Info;
        }

        [ConfigurationProperty("nickName")]
        public ConfigItem<string> NickName
        {
            get { return (ConfigItem<string>)this["nickName"]; }
            set { this["nickName"] = value; }
        }

        [ConfigurationProperty("wcID")]
        public ConfigItem<string> WcID
        {
            get { return (ConfigItem<string>)this["wcID"]; }
            set { this["wcID"] = value; }
        }

        [ConfigurationProperty("servers")]
        public ConfigItem<string> Servers
        {
            get { return (ConfigItem<string>)this["servers"]; }
            set { this["servers"] = value; }
        }

        [ConfigurationProperty("logLevel")]
        public ConfigItem<LogLevel> LogLevel
        {
            get { return (ConfigItem<LogLevel>)this["logLevel"]; }
            set { this["logLevel"] = value; }
        }
    }
}

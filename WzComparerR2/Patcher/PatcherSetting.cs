using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

using WzComparerR2.Config;

namespace WzComparerR2.Patcher
{
    public class PatcherSetting : ConfigurationElement
    {
        public PatcherSetting()
        {

        }

        public PatcherSetting(string serverName)
        {
            this.ServerName = serverName;
        }

        public PatcherSetting(string serverName, string urlFormat)
            : this(serverName)
        {
            this.UrlFormat = urlFormat;
        }

        public PatcherSetting(string serverName, string urlFormat, int ver0, int ver1)
            : this(serverName, urlFormat)
        {
            this.Version0 = ver0;
            this.Version1 = ver1;
        }

        [ConfigurationProperty("serverName", IsRequired = true)]
        public string ServerName
        {
            get { return (string)this["serverName"]; }
            set { this["serverName"] = value; }
        }

        [ConfigurationProperty("urlFormat")]
        public string UrlFormat
        {
            get { return (string)this["urlFormat"]; }
            set { this["urlFormat"] = value; }
        }

        [ConfigurationProperty("version0")]
        public int Version0
        {
            get { return (int)this["version0"]; }
            set { this["version0"] = value; }
        }

        [ConfigurationProperty("version1")]
        public int Version1
        {
            get { return (int)this["version1"]; }
            set { this["version1"] = value; }
        }

        public string Url
        {
            get
            {
                return string.Format(UrlFormat, Version0, Version1);
            }
        }

        public override string ToString()
        {
            return this.ServerName;
        }
    }
}

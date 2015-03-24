using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Patcher
{
    public class PatcherSetting
    {
        public PatcherSetting(string serverName)
        {
            this.serverName = serverName;
        }

        public PatcherSetting(string serverName, string urlFormat)
            : this(serverName)
        {
            this.urlFormat = urlFormat;
        }

        public PatcherSetting(string serverName, string urlFormat, int ver0, int ver1)
            : this(serverName, urlFormat)
        {
            this.version0 = ver0;
            this.version1 = ver1;
        }

        string serverName;
        string urlFormat;
        int version0;
        int version1;

        public string ServerName
        {
            get { return serverName; }
            set { serverName = value; }
        }

        public string UrlFormat
        {
            get { return urlFormat; }
            set { urlFormat = value; }
        }

        public int Version0
        {
            get { return version0; }
            set { version0 = value; }
        }

        public int Version1
        {
            get { return version1; }
            set { version1 = value; }
        }

        public string Url
        {
            get
            {
                return string.Format(urlFormat, version0, version1);
            }
        }

        public override string ToString()
        {
            return this.serverName;
        }
    }
}

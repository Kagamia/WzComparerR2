using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;

namespace WzComparerR2.Patcher
{
    public class PatcherSetting : ConfigurationElement
    {
        public PatcherSetting()
        {

        }

        public PatcherSetting(string serverName)
            : this(serverName, null, 1)
        {
            this.ServerName = serverName;
        }

        public PatcherSetting(string serverName, string urlFormat)
            : this(serverName, urlFormat, 1)
        {
            this.UrlFormat = urlFormat;
        }

        public PatcherSetting(string serverName, string urlFormat, int maxVersion)
        {
            this.UrlFormat = urlFormat;
            this.ServerName = serverName;
            this.MaxVersion = maxVersion;
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

        [Obsolete("Deprecated in favor of 'Versions'")]
        [ConfigurationProperty("version0")]
        public int? Version0
        {
            get { return (int?)this["version0"]; }
            set { this["version0"] = value; }
        }

        [Obsolete("Deprecated in favor of 'Versions'")]
        [ConfigurationProperty("version1")]
        public int? Version1
        {
            get { return (int?)this["version1"]; }
            set { this["version1"] = value; }
        }

        [ConfigurationProperty("maxVersion")]
        public int MaxVersion
        {
            get { return (int)this["maxVersion"]; }
            set { this["maxVersion"] = value; }
        }

        [ConfigurationProperty("versions")]
        [TypeConverter(typeof(IntArrayToStringConverter))]
        public int[] Versions
        {
            get { return (int[])this["versions"]; }
            set { this["versions"] = value; }
        }

        public string Url
        {
            get
            {
                if (this.UrlFormat != null)
                {
                    if (this.Versions != null)
                    {
                        return string.Format(this.UrlFormat, this.Versions.Cast<object>().ToArray());
                    }
                    else if (this.MaxVersion > 0)
                    {
                        return string.Format(this.UrlFormat, Enumerable.Repeat<object>(0, this.MaxVersion).ToArray());
                    }
                    else
                    {
                        return this.UrlFormat;
                    }
                }
                return null;
            }
        }

        public override string ToString()
        {
            return this.ServerName;
        }

        public class IntArrayToStringConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(int[]);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string s)
                {
                    return s.Split(',').Select(segment => int.Parse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture)).ToArray();
                }
                return null;
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (value is int[] array && destinationType == typeof(string))
                {
                    return string.Join(",", array.Select(v => v.ToString(CultureInfo.InvariantCulture)));
                }
                return null;
            }
        }
    }
}

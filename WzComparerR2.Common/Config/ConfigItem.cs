using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class ConfigItem<T> : ConfigurationElement
    {
        [ConfigurationProperty("value", IsRequired = true)]
        public T Value
        {
            get { return (T)this["value"]; }
            set { this["value"] = value; }
        }

        public static implicit operator T(ConfigItem<T> item)
        {
            return item.Value;
        }

        public static implicit operator ConfigItem<T>(T item)
        {
            return new ConfigItem<T>() { Value = item };
        }

        public override string ToString()
        {
            return nameof(ConfigItem<T>) + " [" + this.Value + "]";
        }
    }
}

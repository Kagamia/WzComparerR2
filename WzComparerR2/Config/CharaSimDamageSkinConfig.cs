using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class CharaSimDamageSkinConfig : ConfigurationElement
    {
        [ConfigurationProperty("showDamageSkinID", DefaultValue = true)]
        public bool ShowDamageSkinID
        {
            get { return (bool)this["showDamageSkinID"]; }
            set { this["showDamageSkinID"] = value; }
        }

        [ConfigurationProperty("showDamageSkin", DefaultValue = true)]
        public bool ShowDamageSkin
        {
            get { return (bool)this["showDamageSkin"]; }
            set { this["showDamageSkin"] = value; }
        }

        [ConfigurationProperty("useMiniSize", DefaultValue = false)]
        public bool UseMiniSize
        {
            get { return (bool)this["useMiniSize"]; }
            set { this["useMiniSize"] = value; }
        }

        [ConfigurationProperty("alwaysUseMseaFormat", DefaultValue = false)]
        public bool AlwaysUseMseaFormat
        {
            get { return (bool)this["alwaysUseMseaFormat"]; }
            set { this["alwaysUseMseaFormat"] = value; }
        }

        [ConfigurationProperty("displayUnitOnSingleLine", DefaultValue = false)]
        public bool DisplayUnitOnSingleLine
        {
            get { return (bool)this["displayUnitOnSingleLine"]; }
            set { this["displayUnitOnSingleLine"] = value; }
        }

        [ConfigurationProperty("damageSkinNumber", DefaultValue = (long)1234567890)]
        public long DamageSkinNumber
        {
            get { return (long)this["damageSkinNumber"]; }
            set { this["damageSkinNumber"] = value; }
        }
    }
}

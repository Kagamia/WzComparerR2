using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class CharaSimSkillConfig : ConfigurationElement
    {
        [ConfigurationProperty("showID", DefaultValue = true)]
        public bool ShowID
        {
            get { return (bool)this["showID"]; }
            set { this["showID"] = value; }
        }

        [ConfigurationProperty("showProperties", DefaultValue = true)]
        public bool ShowProperties
        {
            get { return (bool)this["showProperties"]; }
            set { this["showProperties"] = value; }
        }

        [ConfigurationProperty("showDelay", DefaultValue = true)]
        public bool ShowDelay
        {
            get { return (bool)this["showDelay"]; }
            set { this["showDelay"] = value; }
        }

        [ConfigurationProperty("showReqSkill", DefaultValue = true)]
        public bool ShowReqSkill
        {
            get { return (bool)this["showReqSkill"]; }
            set { this["showReqSkill"] = value; }
        }

        [ConfigurationProperty("defaultLevel", DefaultValue = DefaultLevel.LevelMax)]
        public DefaultLevel DefaultLevel
        {
            get { return (DefaultLevel)this["defaultLevel"]; }
            set { this["defaultLevel"] = value; }
        }

        [ConfigurationProperty("intervalLevel", DefaultValue = 10)]
        public int IntervalLevel
        {
            get { return (int)this["intervalLevel"]; }
            set { this["intervalLevel"] = value; }
        }
    }
}

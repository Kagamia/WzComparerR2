using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using WzComparerR2.Config;

namespace WzComparerR2.Patcher
{
    public class PatcherSettingCollection : ConfigItemCollectionBase<PatcherSetting>
    {
        public PatcherSettingCollection()
        {
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as PatcherSetting).ServerName;
        }
    }
}

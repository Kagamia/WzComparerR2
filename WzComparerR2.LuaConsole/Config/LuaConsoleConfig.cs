using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.Config;

namespace WzComparerR2.LuaConsole.Config
{
    [SectionName("WcR2.LuaConsole")]
    public class LuaConsoleConfig : ConfigSectionBase<LuaConsoleConfig>
    {
        public LuaConsoleConfig()
        {
        }

        /// <summary>
        /// 获取最近打开的文档列表。
        /// </summary>
        [ConfigurationProperty("recentDocuments")]
        [ConfigurationCollection(typeof(ConfigArrayList<string>.ItemElement))]
        public ConfigArrayList<string> RecentDocuments
        {
            get { return (ConfigArrayList<string>)this["recentDocuments"]; }
        }
    }
}

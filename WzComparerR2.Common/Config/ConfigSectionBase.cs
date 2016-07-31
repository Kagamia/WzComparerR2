using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Reflection;

namespace WzComparerR2.Config
{
    public abstract class ConfigSectionBase<T> : ConfigurationSection
        where T : ConfigSectionBase<T>, new()
    {
        public static T Default
        {
            get
            {
                string secName = ConfigManager.GetSectionName(typeof(T));
                try
                {
                    return ConfigManager.ConfigFile.GetSection(secName) as T;
                }
                catch (ConfigurationErrorsException e)
                {
                    ConfigManager.Reload();
                    return ConfigManager.ConfigFile.GetSection(secName) as T;
                }
            }
        }
    }
}

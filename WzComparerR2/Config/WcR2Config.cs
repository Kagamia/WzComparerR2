using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Drawing;
using WzComparerR2.Patcher;

namespace WzComparerR2.Config
{
    [SectionName("WcR2")]
    public sealed class WcR2Config : ConfigSectionBase<WcR2Config>
    {
        public WcR2Config()
        {
            this.MainStyle = DevComponents.DotNetBar.eStyle.Office2007VistaGlass;
            this.MainStyleColor = Color.DimGray;
            this.SortWzOnOpened = true;
            this.AutoDetectExtFiles = true;
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

        /// <summary>
        /// 获取或设置主窗体界面样式。
        /// </summary>
        [ConfigurationProperty("mainStyle")]
        public ConfigItem<DevComponents.DotNetBar.eStyle> MainStyle
        {
            get { return (ConfigItem<DevComponents.DotNetBar.eStyle>)this["mainStyle"]; }
            set { this["mainStyle"] = value; }
        }

        /// <summary>
        /// 获取或设置主窗体界面主题色。
        /// </summary>
        [ConfigurationProperty("mainStyleColor")]
        public ConfigItem<Color> MainStyleColor
        {
            get { return (ConfigItem<Color>)this["mainStyleColor"]; }
            set { this["mainStyleColor"] = value; }
        }

        /// <summary>
        /// 获取或设置Wz对比报告默认输出文件夹。
        /// </summary>
        [ConfigurationProperty("comparerOutputFolder")]
        public ConfigItem<string> ComparerOutputFolder
        {
            get { return (ConfigItem<string>)this["comparerOutputFolder"]; }
            set { this["comparerOutputFolder"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示Wz文件加载后是否自动排序。
        /// </summary>
        [ConfigurationProperty("sortWzOnOpened")]
        public ConfigItem<bool> SortWzOnOpened
        {
            get { return (ConfigItem<bool>)this["sortWzOnOpened"]; }
            set { this["sortWzOnOpened"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示Wz文件加载后是否自动排序。
        /// </summary>
        [ConfigurationProperty("sortWzByImgID")]
        public ConfigItem<bool> SortWzByImgID
        {
            get { return (ConfigItem<bool>)this["sortWzByImgID"]; }
            set { this["sortWzByImgID"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示Wz加载中对于ansi字符串的编码。
        /// </summary>
        [ConfigurationProperty("wzEncoding")]
        public ConfigItem<int> WzEncoding
        {
            get { return (ConfigItem<int>)this["wzEncoding"]; }
            set { this["wzEncoding"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示加载Base.wz时是否自动检测扩展wz文件（如Map2、Mob2）。
        /// </summary>
        [ConfigurationProperty("autoDetectExtFiles")]
        public ConfigItem<bool> AutoDetectExtFiles
        {
            get { return (ConfigItem<bool>)this["autoDetectExtFiles"]; }
            set { this["autoDetectExtFiles"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示读取wz是否跳过img检测。
        /// </summary>
        [ConfigurationProperty("imgCheckDisabled")]
        public ConfigItem<bool> ImgCheckDisabled
        {
            get { return (ConfigItem<bool>)this["imgCheckDisabled"]; }
            set { this["imgCheckDisabled"] = value; }
        }

        [ConfigurationProperty("patcherSettings")]
        [ConfigurationCollection(typeof(PatcherSetting), CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
        public PatcherSettingCollection PatcherSettings
        {
            get { return (PatcherSettingCollection)this["patcherSettings"]; }
        }
    }
}

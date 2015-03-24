using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Collections.ObjectModel;
using WzComparerR2.Patcher;

namespace WzComparerR2
{
    public static class Setting
    {
        static Setting()
        {
            recentDocuments = new List<string>(10);
            patcherSettings = new List<PatcherSetting>();
            settingFileName = Path.Combine(Application.StartupPath, "Settings.xml");
        }

        private static Dictionary<string, SettingItemCollection> sections;
        private static List<Type> itemTypes;

        private static string GetValue(string section, string key)
        {
            SettingItem item = GetItem(section, key);
            if (item == null)
                return null;
            return item.Value;
        }

        private static T GetValue<T>(string section, string key) where T : IConvertible
        {
            string value = GetValue(section, key);
            if (value == null)
                return default(T);
            T t = (T)((IConvertible)value).ToType(typeof(T), null);
            return t;
        }

        public static SettingItem GetItem(string section, string key)
        {
            SettingItemCollection items;
            if (!sections.TryGetValue(section, out items))
            {
                return null;
            }
            SettingItem item = items["key"];
            return item;
        }

        private static T GetValueList<T>(string section, string key) where T : SettingItem
        {
            return null;
        }

        private static void SetValue(string section, string key, string value)
        {
        }

        public static void RegisterItemType(Type type)
        {
        }

        public static void BeginEdit()
        {
            autoSave = false;
        }

        public static void EndEdit()
        {
            autoSave = true;
            Save();
        }

        private static bool autoSave = true;


        private static string settingFileName;
        private static List<string> recentDocuments;
        private static string autoSavePictureFolder;
        private static int gifBackGroundColor;
        private static int gifMinAlphaMixed;
        private static int gifEncoder;
        private static List<PatcherSetting> patcherSettings;
        private static string patchFile;
        private static string comparerOutputFolder;
        private static int selectedFontIndex;

        private static bool autoQuickView;
        private static bool skillShowID;
        private static bool skillShowDelay;
        private static int skillDefaultLevel;
        private static int skillLevelInterval;
        private static bool gearShowID;
        private static bool gearShowWeaponSpeed;
        private static bool gearShowLevelOrSealed;
        private static bool itemShowID;
        private static bool itemLinkRecipeInfo;
        private static bool itemLinkRecipeItem;
        private static bool recipeShowID;

        /// <summary>
        /// 获取最近打开的文档。
        /// </summary>
        public static ReadOnlyCollection<string> RecentDocuments
        {
            get { return new ReadOnlyCollection<string>(Setting.recentDocuments); }
        }

        /// <summary>
        /// 新增最近打开的文档。
        /// </summary>
        /// <param Name="doc"></param>
        public static void AddRecentDocument(string doc)
        {
            if (string.IsNullOrEmpty(doc))
                return;
            recentDocuments.Remove(doc);

            int maxCount = recentDocuments.Capacity;
            if (recentDocuments.Count >= maxCount)
                recentDocuments.RemoveRange(maxCount - 1, recentDocuments.Count - (maxCount - 1));
            recentDocuments.Insert(0, doc);
            Save();
        }

        public static string AutoSavePictureFolder
        {
            get { return autoSavePictureFolder; }
            set { autoSavePictureFolder = value; Save(); }
        }

        public static Color GifBackGroundColor
        {
            get { return Color.FromArgb(gifBackGroundColor); }
            set { gifBackGroundColor = value.ToArgb(); Save(); }
        }

        public static int GifMinAlphaMixed
        {
            get { return gifMinAlphaMixed; }
            set { gifMinAlphaMixed = value; Save(); }
        }

        public static int GifEncoder
        {
            get { return gifEncoder; }
            set { gifEncoder = value; Save(); }
        }

        public static ReadOnlyCollection<PatcherSetting> PatcherSettings
        {
            get { return new ReadOnlyCollection<PatcherSetting>(patcherSettings); }
        }

        public static string PatchFile
        {
            get { return patchFile; }
            set { patchFile = value; Save(); }
        }

        public static string ComparerOutputFolder
        {
            get { return comparerOutputFolder; }
            set { comparerOutputFolder = value; Save(); }
        }

        public static int SelectedFontIndex
        {
            get { return selectedFontIndex; }
            set { selectedFontIndex = value; Save(); }
        }

        #region QuickView相关属性
        public static bool AutoQuickView
        {
            get { return autoQuickView; }
            set { autoQuickView = value; Save(); }
        }

        public static bool SkillShowID
        {
            get { return skillShowID; }
            set { skillShowID = value; Save(); }
        }

        public static bool SkillShowDelay
        {
            get { return skillShowDelay; }
            set { skillShowDelay = value; Save(); }
        }

        public static DefaultLevel SkillDefaultLevel
        {
            get { return (DefaultLevel)skillDefaultLevel; }
            set { skillDefaultLevel = (int)value; Save(); }
        }

        public static int SkillLevelInterval
        {
            get { return skillLevelInterval; }
            set { skillLevelInterval = value; Save(); }
        }

        public static bool GearShowID
        {
            get { return gearShowID; }
            set { gearShowID = value; Save(); }
        }

        public static bool GearShowWeaponSpeed
        {
            get { return gearShowWeaponSpeed; }
            set { gearShowWeaponSpeed = value; Save(); }
        }

        public static bool GearShowLevelOrSealed
        {
            get { return gearShowLevelOrSealed; }
            set { gearShowLevelOrSealed = value; Save(); }
        }

        public static bool ItemShowID
        {
            get { return itemShowID; }
            set { itemShowID = value; Save(); }
        }

        public static bool ItemLinkRecipeInfo
        {
            get { return itemLinkRecipeInfo; }
            set { itemLinkRecipeInfo = value; Save(); }
        }

        public static bool ItemLinkRecipeItem
        {
            get { return itemLinkRecipeItem; }
            set { itemLinkRecipeItem = value; Save(); }
        }

        public static bool RecipeShowID
        {
            get { return recipeShowID; }
            set { recipeShowID = value; Save(); }
        }
        #endregion

        public static void Save()
        {
            if (!autoSave)
            {
                return;
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement parentNode = xmlDoc.CreateElement("WzComparerR2");
            XmlElement settingNode = xmlDoc.CreateElement("Settings");
            XmlElement propNode;

            //recentDoc
            propNode = xmlDoc.CreateElement("RecentDocuments");
            foreach (string file in recentDocuments)
            {
                XmlElement fileName = CreateNode(xmlDoc, "file", "fileName", file);
                propNode.AppendChild(fileName);
            }
            settingNode.AppendChild(propNode);

            //autoSaveFolder
            propNode = CreateNode(xmlDoc, "AutoSaveFolder", "value", autoSavePictureFolder);
            settingNode.AppendChild(propNode);

            //gifBackGroundColor
            propNode = CreateNode(xmlDoc, "GifBackGroundColor", "value", gifBackGroundColor.ToString());
            settingNode.AppendChild(propNode);

            //gifMinAlphaMixed
            propNode = CreateNode(xmlDoc, "GifMinAlphaMixed", "value", gifMinAlphaMixed.ToString());
            settingNode.AppendChild(propNode);

            //gifEncoder
            propNode = CreateNode(xmlDoc, "GifEncoder", "value", gifEncoder.ToString());
            settingNode.AppendChild(propNode);

            //patcherSettings
            propNode = xmlDoc.CreateElement("Patcher");
            foreach (PatcherSetting patcher in patcherSettings)
            {
                XmlElement patcherSetting = CreateNode(xmlDoc, "PatcherSetting", "server", patcher.ServerName,
                    "urlFormat", patcher.UrlFormat, "ver0", patcher.Version0.ToString(), "ver1", patcher.Version1.ToString());
                propNode.AppendChild(patcherSetting);
            }
            settingNode.AppendChild(propNode);
            
            //selectedFontIndex
            propNode = CreateNode(xmlDoc, "SelectedFontIndex", "value", selectedFontIndex.ToString());
            settingNode.AppendChild(propNode);


            //autoQuickView
            propNode = CreateNode(xmlDoc, "AutoQuickView", "value", autoQuickView.ToString());
            settingNode.AppendChild(propNode);

            //skillShowID
            propNode = CreateNode(xmlDoc, "SkillShowID", "value", skillShowID.ToString());
            settingNode.AppendChild(propNode);

            //skillShowDelay
            propNode = CreateNode(xmlDoc, "SkillShowDelay", "value", skillShowDelay.ToString());
            settingNode.AppendChild(propNode);

            //skillDefaultLevel
            propNode = CreateNode(xmlDoc, "SkillDefaultLevel", "value", skillDefaultLevel.ToString());
            settingNode.AppendChild(propNode);

            //skillLevelInterval
            propNode = CreateNode(xmlDoc, "SkillLevelInterval", "value", skillLevelInterval.ToString());
            settingNode.AppendChild(propNode);

            //gearShowID
            propNode = CreateNode(xmlDoc, "GearShowID", "value", gearShowID.ToString());
            settingNode.AppendChild(propNode);

            //gearShowWeaponSpeed
            propNode = CreateNode(xmlDoc, "GearShowWeaponSpeed", "value", gearShowWeaponSpeed.ToString());
            settingNode.AppendChild(propNode);

            //gearShowLevelOrSealed
            propNode = CreateNode(xmlDoc, "GearShowLevelOrSealed", "value", gearShowLevelOrSealed.ToString());
            settingNode.AppendChild(propNode);

            //itemShowID
            propNode = CreateNode(xmlDoc, "ItemShowID", "value", itemShowID.ToString());
            settingNode.AppendChild(propNode);

            //itemLinkRecipeInfo
            propNode = CreateNode(xmlDoc, "ItemLinkRecipeInfo", "value", itemLinkRecipeInfo.ToString());
            settingNode.AppendChild(propNode);

            //itemLinkRecipeItem
            propNode = CreateNode(xmlDoc, "ItemLinkRecipeItem", "value", itemLinkRecipeItem.ToString());
            settingNode.AppendChild(propNode);

            //recipeShowID
            propNode = CreateNode(xmlDoc, "RecipeShowID", "value", recipeShowID.ToString());
            settingNode.AppendChild(propNode);

            parentNode.AppendChild(settingNode);
            xmlDoc.AppendChild(parentNode);
            xmlDoc.Save(settingFileName);
        }

        public static void Load()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNodeList lst;
            XmlNode node;
            try
            {
                xmlDoc.Load(settingFileName);
                lst = xmlDoc.SelectNodes("WzComparerR2/Settings/RecentDocuments/file");
                foreach (XmlNode nod in lst)
                {
                    XmlAttribute attr = nod.Attributes["fileName"];
                    if (attr != null)
                        recentDocuments.Add(attr.Value);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/AutoSaveFolder");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        autoSavePictureFolder = attr.Value;
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/GifBackGroundColor");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Int32.TryParse(attr.Value, out gifBackGroundColor);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/GifMinAlphaMixed");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Int32.TryParse(attr.Value, out gifMinAlphaMixed);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/GifEncoder");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Int32.TryParse(attr.Value, out gifEncoder);
                }

                lst = xmlDoc.SelectNodes("WzComparerR2/Settings/Patcher/PatcherSetting");
                if (lst.Count > 0)
                {
                    foreach (XmlNode nod in lst)
                    {
                        PatcherSetting patcherSetting;
                        int v;

                        XmlAttribute attr = nod.Attributes["server"];
                        if (attr != null && !string.IsNullOrEmpty(attr.Value))
                            patcherSetting = new PatcherSetting(attr.Value);
                        else
                            continue;

                        attr = nod.Attributes["urlFormat"];
                        if (attr != null)
                            patcherSetting.UrlFormat = attr.Value;

                        attr = nod.Attributes["ver0"];
                        if (attr != null)
                        {
                            Int32.TryParse(attr.Value, out v);
                            patcherSetting.Version0 = v;
                        }

                        attr = nod.Attributes["ver1"];
                        if (attr != null)
                        {
                            Int32.TryParse(attr.Value, out v);
                            patcherSetting.Version1 = v;
                        }
                        patcherSettings.Add(patcherSetting);
                    }
                }
                else
                {
                    patcherSettings.AddRange(DefaultPatcherSetting);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/SelectedFontIndex");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Int32.TryParse(attr.Value, out selectedFontIndex);
                }


                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/AutoQuickView");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Boolean.TryParse(attr.Value, out autoQuickView);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/SkillShowID");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Boolean.TryParse(attr.Value, out skillShowID);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/SkillShowDelay");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Boolean.TryParse(attr.Value, out skillShowDelay);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/SkillDefaultLevel");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Int32.TryParse(attr.Value, out skillDefaultLevel);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/SkillLevelInterval");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Int32.TryParse(attr.Value, out skillLevelInterval);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/GearShowID");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Boolean.TryParse(attr.Value, out gearShowID);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/GearShowWeaponSpeed");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Boolean.TryParse(attr.Value, out gearShowWeaponSpeed);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/GearShowLevelOrSealed");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Boolean.TryParse(attr.Value, out gearShowLevelOrSealed);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/ItemShowID");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Boolean.TryParse(attr.Value, out itemShowID);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/ItemLinkRecipeInfo");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Boolean.TryParse(attr.Value, out itemLinkRecipeInfo);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/ItemLinkRecipeItem");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Boolean.TryParse(attr.Value, out itemLinkRecipeItem);
                }

                node = xmlDoc.SelectSingleNode("WzComparerR2/Settings/RecipeShowID");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["value"];
                    if (attr != null)
                        Boolean.TryParse(attr.Value, out recipeShowID);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static XmlElement CreateNode(XmlDocument xmlDoc, string nodeName, params string[] attributes)
        {
            if (xmlDoc == null || string.IsNullOrEmpty(nodeName))
                return null;
            XmlElement child = xmlDoc.CreateElement(nodeName);
            if (attributes != null)
            {
                for (int i = 0; i < attributes.Length; i += 2)
                {
                    string attrName = attributes[i], attrValue = attributes[i + 1];
                    if (attrName != null)
                    {
                        XmlAttribute attr = xmlDoc.CreateAttribute(attrName);
                        attr.Value = attrValue;
                        child.Attributes.Append(attr);
                    }
                }
            }
            return child;
        }

        private static IEnumerable<PatcherSetting> DefaultPatcherSetting
        {
            get
            {
                yield return new PatcherSetting("KMST", "http://maplestory.dn.nexoncdn.co.kr/PatchT/{1:d5}/{0:d5}to{1:d5}.patch");
                yield return new PatcherSetting("KMS", "http://maplestory.dn.nexoncdn.co.kr/Patch/{1:d5}/{0:d5}to{1:d5}.patch");
                yield return new PatcherSetting("JMS", "ftp://download2.nexon.co.jp/maple/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch");
            }
        }
    }
}

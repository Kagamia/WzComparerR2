using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Xml;

namespace WzComparerR2
{
    public class SettingItem
    {
        public SettingItem()
            : this(null, null)
        {
        }

        public SettingItem(string key)
            : this(key, null)
        {
        }

        public SettingItem(string key, string value)
        {
            this.key = key;
            this.attr = new NameValueCollection();
            if (value != null)
                this.attr["value"] = value;
        }

        private string key;
        private NameValueCollection attr;

        public string Key
        {
            get { return key; }
        }

        public string Value
        {
            get { return this.attr["value"]; }
            set { this.attr["value"] = value; }
        }

        public NameValueCollection Attributes
        {
            get { return attr; }
        }

        public string this[string key]
        {
            get { return this.attr[key]; }
            set { this.attr[key] = value; }
        }

        public virtual XmlNode WriteTo(XmlDocument xmlDoc)
        {
            if (xmlDoc == null)
                return null;
            XmlElement node = xmlDoc.CreateElement("add");
            if (this.key != null)
                node.Attributes.Append(xmlDoc.CreateAttribute("key")).Value = this.key;
            if (this.GetType() != typeof(SettingItem))
                node.Attributes.Append(xmlDoc.CreateAttribute("class")).Value = this.GetType().FullName;
            foreach (string key in this.attr.AllKeys)
            {
                node.Attributes.Append(xmlDoc.CreateAttribute(key)).Value = this.attr[key];
            }
            return node;
        }

        public virtual void LoadFrom(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return;
        }
    }
}

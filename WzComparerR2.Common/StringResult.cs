using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Common
{
    public class StringResult
    {
        public StringResult()
        {
            allValues = new Dictionary<string, string>();
        }

        public StringResult(bool initSkillH)
            : this()
        {
            if (initSkillH)
            {
                this.skillH = new List<string>();
                this.skillpH = new List<string>();
                this.skillhcH = new List<string>();
            }
        }

        private string name;
        private string desc;
        private string pdesc;
        private string autodesc;
        private List<string> skillH;
        private List<string> skillpH;
        private List<string> skillhcH;
        private string fullPath;
        private Dictionary<string, string> allValues;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Desc
        {
            get { return desc; }
            set { desc = value; }
        }

        public string Pdesc
        {
            get { return pdesc; }
            set { pdesc = value; }
        }

        public string AutoDesc
        {
            get { return autodesc; }
            set { autodesc = value; }
        }

        public List<string> SkillH
        {
            get { return skillH; }
        }

        public List<string> SkillpH
        {
            get { return skillpH; }
        }

        public List<string> SkillhcH
        {
            get { return skillhcH; }
        }

        public string FullPath
        {
            get { return fullPath; }
            set { fullPath = value; }
        }

        public Dictionary<string, string> AllValues
        {
            get { return allValues; }
        }

        public string this[string key]
        {
            get
            {
                string value = null;
                if (key != null)
                {
                    this.allValues.TryGetValue(key, out value);
                }
                return value;
            }
        }
    }
}

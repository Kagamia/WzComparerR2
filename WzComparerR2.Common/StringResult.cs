using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Common
{
    public class StringResult
    {
        public StringResult()
        {
        }

        public string Name { get; set; }
        public string Desc { get; set; }
        public string Pdesc { get; set; }
        public string AutoDesc { get; set; }
        public string FullPath { get; set; }

        private List<KeyValuePair<string, string>> allValues;

        public string this[string key]
        {
            get
            {
                if (this.allValues != null && key != null)
                {
                    foreach(var kv in this.allValues)
                    {
                        if (kv.Key == key)
                        {
                            return kv.Value;
                        }
                    }
                }
                return null;
            }
            set
            {
                if (key != null)
                {
                    if (this.allValues == null)
                    {
                        this.allValues = new List<KeyValuePair<string, string>>();
                    }

                    for(int i = 0; i < this.allValues.Count; i++)
                    {
                        var kv = this.allValues[i];
                        if (kv.Key == key)
                        {
                            this.allValues[i] = new KeyValuePair<string, string>(key, value);
                            return;
                        }
                    }
                    this.allValues.Add(new KeyValuePair<string, string>(key, value));
                }
            }
        }
    }

    public sealed class StringResultSkill : StringResult
    {

        public StringResultSkill()
        {
            this.skillH = new List<string>();
            this.skillpH = new List<string>();
            this.skillhcH = new List<string>();
            this.skillExtraH = new List<KeyValuePair<int, string>>();
        }

        public List<string> SkillH
        {
            get { return this.skillH; }
        }

        public List<string> SkillpH
        {
            get { return this.skillpH; }
        }

        public List<string> SkillhcH
        {
            get { return this.skillhcH; }
        }

        public List<KeyValuePair<int, string>> SkillExtraH
        {
            get { return this.skillExtraH; }
        }

        private readonly List<string> skillH;
        private readonly List<string> skillpH;
        private readonly List<string> skillhcH;
        private readonly List<KeyValuePair<int, string>> skillExtraH;
    }
}

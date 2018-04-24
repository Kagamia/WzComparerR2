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

        public virtual List<string> SkillH
        {
            get { return null; }
        }

        public virtual List<string> SkillpH
        {
            get { return null; }
        }

        public virtual List<string> SkillhcH
        {
            get { return null; }
        }

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
        }

        public override List<string> SkillH
        {
            get { return this.skillH; }
        }

        public override List<string> SkillpH
        {
            get { return this.skillpH; }
        }

        public override List<string> SkillhcH
        {
            get { return this.skillhcH; }
        }

        private readonly List<string> skillH;
        private readonly List<string> skillpH;
        private readonly List<string> skillhcH;
    }
}

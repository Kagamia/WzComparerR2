using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WzComparerR2.Config
{
    public sealed class SectionNameAttribute : Attribute
    {
        public SectionNameAttribute(string sectionName)
        {
            this.Name = sectionName;
        }

        public string Name { get; set; }
    }
}

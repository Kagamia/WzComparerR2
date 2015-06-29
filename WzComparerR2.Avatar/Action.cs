using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Avatar
{
    public class Action
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public bool Enabled { get; set; }
        public int Order { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}

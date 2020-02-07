using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public class TooltipHelp : ICloneable
    {
        public TooltipHelp(string title, string desc)
        {
            this.Title = title;
            this.Desc = desc;
        }

        public string Title { get; set; }
        public string Desc { get; set; }

        public virtual object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
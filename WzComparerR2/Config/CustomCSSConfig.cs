using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms.VisualStyles;

namespace WzComparerR2.Config
{
    [SectionName("WcR2.CustomCSS")]
    public class CustomCSSConfig : ConfigSectionBase<CustomCSSConfig>
    {
        public CustomCSSConfig()
        {
            BackgroundColor = Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber));
            NormalTextColor = Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber));
            ChangedBackgroundColor = Color.FromArgb(Int32.Parse("fffff4c4", NumberStyles.HexNumber));
            AddedBackgroundColor = Color.FromArgb(Int32.Parse("ffebf2f8", NumberStyles.HexNumber));
            RemovedBackgroundColor = Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber));
            ChangedTextColor = Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber));
            AddedTextColor = Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber));
            RemovedTextColor = Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber));
            HyperlinkColor = Color.FromArgb(Int32.Parse("ff0000ff", NumberStyles.HexNumber));
        }

        [ConfigurationProperty("backgroundColor")]
        public Color BackgroundColor
        {
            get { return (Color)this["backgroundColor"]; }
            set { this["backgroundColor"] = value; }
        }

        [ConfigurationProperty("normalTextColor")]
        public Color NormalTextColor
        {
            get { return (Color)this["normalTextColor"]; }
            set { this["normalTextColor"] = value; }
        }

        [ConfigurationProperty("changedBackgroundColor")]
        public Color ChangedBackgroundColor
        {
            get { return (Color)this["changedBackgroundColor"]; }
            set { this["changedBackgroundColor"] = value; }
        }

        [ConfigurationProperty("addedBackgroundColor")]
        public Color AddedBackgroundColor
        {
            get { return (Color)this["addedBackgroundColor"]; }
            set { this["addedBackgroundColor"] = value; }
        }

        [ConfigurationProperty("removedBackgroundColor")]
        public Color RemovedBackgroundColor
        {
            get { return (Color)this["removedBackgroundColor"]; }
            set { this["removedBackgroundColor"] = value; }
        }

        [ConfigurationProperty("changedTextColor")]
        public Color ChangedTextColor
        {
            get { return (Color)this["changedTextColor"]; }
            set { this["changedTextColor"] = value; }
        }

        [ConfigurationProperty("addedTextColor")]
        public Color AddedTextColor
        {
            get { return (Color)this["addedTextColor"]; }
            set { this["addedTextColor"] = value; }
        }

        [ConfigurationProperty("removedTextColor")]
        public Color RemovedTextColor
        {
            get { return (Color)this["removedTextColor"]; }
            set { this["removedTextColor"] = value; }
        }

        [ConfigurationProperty("hyperlinkColor")]
        public Color HyperlinkColor
        {
            get { return (Color)this["hyperlinkColor"]; }
            set { this["hyperlinkColor"] = value; }
        }
    }
}

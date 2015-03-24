using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace WzComparerR2.PluginBase
{
    internal class PluginInfo
    {
        public string FileName { get; set; }
        public Assembly Assembly { get; set; }
        public PluginEntry Instance { get; set; }
    }
}

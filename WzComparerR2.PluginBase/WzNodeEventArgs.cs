using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.PluginBase
{
    public class WzNodeEventArgs : EventArgs
    {
        public WzNodeEventArgs(Wz_Node node)
        {
            this.Node = node;
        }

        public Wz_Node Node { get; private set; }
    }
}

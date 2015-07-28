using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.PluginBase
{
    public class WzStructureEventArgs : EventArgs
    {
        public WzStructureEventArgs(Wz_Structure wz)
        {
            this.WzStructure = wz;
        }

        public Wz_Structure WzStructure { get; private set; }
    }
}

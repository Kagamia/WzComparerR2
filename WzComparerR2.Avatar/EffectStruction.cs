using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WzComparerR2.Avatar
{
    public class EffectStruction
    {
        public EffectStruction(int ic, int fr)
        {
            itemcode = ic;
            frame = fr;
            delay = 100;
            action = "";
        }
        public int itemcode;
        public string action;
        public int frame;
        public int delay;

        public override string ToString()
        {
            return string.Format("{0}_{1}_{2}", itemcode, action, frame);
        }
    }
}
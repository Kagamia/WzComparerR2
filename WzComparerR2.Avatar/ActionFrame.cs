using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Avatar
{
    public class ActionFrame
    {
        public ActionFrame()
        {
        }

        public ActionFrame(string action, int frame)
        {
            this.Action = action;
            this.Frame = frame;
        }

        public string Action { get; set; }
        public int Frame { get; set; }
        public int Delay { get; set; }
    }
}

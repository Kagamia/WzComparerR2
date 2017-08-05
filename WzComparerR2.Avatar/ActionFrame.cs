using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

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
        public int? Frame { get; set; }
        public int Delay { get; set; }

        public bool? Face { get; set; }
        public bool Flip { get; set; }
        public Point Move { get; set; }
        public int Rotate { get; set; }
        public int RotateProp { get; set; }

        //骑宠用特殊属性
        public string ForceCharacterAction { get; set; }
        public int? ForceCharacterActionFrameIndex { get; set; }
        public bool ForceCharacterFaceHide { get; set; }
        public bool ForceCharacterFlip { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WzComparerR2.Controls
{
    public class AnimationItemEventArgs
    {
        public AnimationItemEventArgs(AnimationItem item)
        {
            this.Item = item;
        }

        /// <summary>
        /// 事件相关联的AnimationItem。
        /// </summary>
        public AnimationItem Item { get; private set; }
        public bool Handled { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender.Patches2
{
    public class TooltipItem : SceneItem
    {
        public Rectangle Rect { get; set; }
        public Rectangle CharRect { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public string ItemEU { get; set; }
    }
}

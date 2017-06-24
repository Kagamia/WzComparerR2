using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Patches2
{
    public class LadderRopeItem : SceneItem
    {
        public int X { get; set; }
        public int Y1 { get; set; }
        public int Y2 { get; set; }
        public int L { get; set; }
        public int Uf { get; set; }
        public int Page { get; set; }

        public static LadderRopeItem LoadFromNode(Wz_Node node)
        {
            var item = new LadderRopeItem()
            {
                X = node.Nodes["x"].GetValueEx(0),
                Y1 = node.Nodes["y1"].GetValueEx(0),
                Y2 = node.Nodes["y2"].GetValueEx(0),
                L = node.Nodes["l"].GetValueEx(0),
                Uf = node.Nodes["uf"].GetValueEx(0),
                Page = node.Nodes["page"].GetValueEx(0),
            };
            return item;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Patches2
{
    public class TileItem : SceneItem
    {
        public string TS { get; set; }
        public string U { get; set; }
        public string No { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public ItemView View { get; set; }

        public static TileItem LoadFromNode(Wz_Node node)
        {
            var item = new TileItem()
            {
                U = node.Nodes["u"].GetValueEx<string>(null),
                No = node.Nodes["no"].GetValueEx<string>(null),

                X = node.Nodes["x"].GetValueEx(0),
                Y = node.Nodes["y"].GetValueEx(0),
            };
            return item;
        }

        public class ItemView
        {
            /// <summary>
            /// 时间关联，单位为毫秒。
            /// </summary>
            public int Time { get; set; }

            /// <summary>
            /// 动画资源。
            /// </summary>
            public object Animator { get; set; }
        }
    }
}

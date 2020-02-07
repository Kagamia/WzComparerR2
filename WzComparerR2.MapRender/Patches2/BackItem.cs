using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Patches2
{
    public class BackItem : SceneItem
    {
        public string BS { get; set; }
        public int Ani { get; set; }
        public string No { get; set; }
        public string SpineAni { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Cx { get; set; }
        public int Cy { get; set; }
        public int Rx { get; set; }
        public int Ry { get; set; }
        public int Alpha { get; set; }
        public TileMode TileMode { get; set; }
        public int ScreenMode { get; set; }
        public bool Flip { get; set; }
        public bool IsFront { get; set; }
        public List<Tuple<int, int>> Quest { get; set; }

        
        public ItemView View { get; set; }

        public static BackItem LoadFromNode(Wz_Node node)
        {
            var item = new BackItem()
            {
                BS = node.Nodes["bS"].GetValueEx<string>(null),
                Ani = node.Nodes["ani"].GetValueEx<int>(0),
                No = node.Nodes["no"].GetValueEx<string>(null),
                SpineAni = node.Nodes["spineAni"].GetValueEx<string>(null),
               
                X = node.Nodes["x"].GetValueEx(0),
                Y = node.Nodes["y"].GetValueEx(0),
                Cx = node.Nodes["cx"].GetValueEx(0),
                Cy = node.Nodes["cy"].GetValueEx(0),
                Rx = node.Nodes["rx"].GetValueEx(0),
                Ry = node.Nodes["ry"].GetValueEx(0),
                Alpha = node.Nodes["a"].GetValueEx(255),

                TileMode = GetBackTileMode(node.Nodes["type"].GetValueEx(0)),
                ScreenMode = node.Nodes["screenMode"].GetValueEx(0),
                Flip = node.Nodes["f"].GetValueEx(false),
                IsFront = node.Nodes["front"].GetValueEx(false),
            };
            item.Quest = new List<Tuple<int, int>>();
            if (node.Nodes["backTags"] != null)
            {
                int questID;
                if (int.TryParse(node.Nodes["backTags"].GetValueEx<string>(null), out questID))
                {
                    item.Quest.Add(new Tuple<int, int>(questID, 1));
                }
            }
            return item;
        }

        private static TileMode GetBackTileMode(int type)
        {
            switch (type)
            {
                case 0: return TileMode.None;
                case 1: return TileMode.Horizontal;
                case 2: return TileMode.Vertical;
                case 3: return TileMode.BothTile;
                case 4: return TileMode.Horizontal | TileMode.ScrollHorizontal;
                case 5: return TileMode.Vertical | TileMode.ScrollVertical;
                case 6: return TileMode.BothTile | TileMode.ScrollHorizontal;
                case 7: return TileMode.BothTile | TileMode.ScrollVertical;
                default: return TileMode.None;
            }
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

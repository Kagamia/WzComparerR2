using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Patches2
{
    public class ObjItem : SceneItem
    {
        public string OS { get; set; }
        public string L0 { get; set; }
        public string L1 { get; set; }
        public string L2 { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public bool Flip { get; set; }
        public string Tags { get; set; }
        public List<Tuple<int, int>> Quest { get; set; }

        public ItemView View { get; set; }

        public static ObjItem LoadFromNode(Wz_Node node)
        {
            var item = new ObjItem()
            {
                OS = node.Nodes["oS"].GetValueEx<string>(null),
                L0 = node.Nodes["l0"].GetValueEx<string>(null),
                L1 = node.Nodes["l1"].GetValueEx<string>(null),
                L2 = node.Nodes["l2"].GetValueEx<string>(null),

                X = node.Nodes["x"].GetValueEx(0),
                Y = node.Nodes["y"].GetValueEx(0),
                Z = node.Nodes["z"].GetValueEx(0),

                Flip = node.Nodes["f"].GetValueEx(false),
                Tags = node.Nodes["tags"].GetValueEx<string>(null),
            };
            item.Quest = new List<Tuple<int, int>>();
            if (item.Tags != null)
            {
                int questID;
                if (int.TryParse(item.Tags, out questID) || (item.Tags.StartsWith("q") && int.TryParse(item.Tags.Substring(1), out questID)))
                {
                    item.Quest.Add(new Tuple<int, int>(questID, 1));
                }
            }
            if (node.Nodes["quest"] != null)
            {
                foreach (Wz_Node questNode in node.Nodes["quest"].Nodes)
                {
                    item.Quest.Add(new Tuple<int, int>(int.Parse(questNode.Text), Convert.ToInt32(questNode.Value)));
                }
            }
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

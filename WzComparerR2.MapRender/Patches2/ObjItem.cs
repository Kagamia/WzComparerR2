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
        public bool Light { get; set; }
        public string SpineAni { get; set; }
        public List<Tuple<int, int>> Quest { get; set; }
        public List<Tuple<int, string, int>> Questex { get; set; }

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
                Light = node.Nodes["light"].GetValueEx<int>(0) != 0,
                SpineAni = node.Nodes["spineAni"].GetValueEx<string>(null),
            };

            string objTags = node.Nodes["tags"].GetValueEx<string>(null);
            if (!string.IsNullOrWhiteSpace(objTags))
            {
                item.Tags = objTags.Split(',').Select(tag => tag.Trim()).ToArray();
            }
            item.Quest = new List<Tuple<int, int>>();
            if (item.Tags != null)
            {
                int questID;
                foreach (string tag in item.Tags)
                {
                    if (int.TryParse(tag, out questID) || (tag.StartsWith("q") && int.TryParse(tag.Substring(1), out questID)))
                    {
                        item.Quest.Add(Tuple.Create(questID, 1));
                    }
                }
            }
            if (node.Nodes["quest"] != null)
            {
                foreach (Wz_Node questNode in node.Nodes["quest"].Nodes)
                {
                    item.Quest.Add(Tuple.Create(int.Parse(questNode.Text), Convert.ToInt32(questNode.Value)));
                }
            }

            item.Questex = new List<Tuple<int, string, int>>();
            if (node.Nodes["questex"] != null)
            {
                foreach (Wz_Node qnodes in node.Nodes["questex"].Nodes)
                {
                    if (int.TryParse(qnodes.Text, out int questID))
                    {
                        Wz_Node knodes = qnodes.Nodes["key"];
                        Wz_Node vnodes = qnodes.Nodes["value"];
                        if (knodes != null && vnodes != null)
                        {
                            item.Questex.Add(Tuple.Create(questID, knodes.GetValueEx<string>(null), vnodes.GetValueEx<int>(-1)));
                        }
                    }
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

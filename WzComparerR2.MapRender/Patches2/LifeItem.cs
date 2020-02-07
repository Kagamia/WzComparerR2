using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Patches2
{
    public class LifeItem : SceneItem
    {
        public int ID { get; set; }
        public LifeType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int MobTime { get; set; }
        public bool Flip { get; set; }
        public bool Hide { get; set; }
        public int Fh { get; set; }
        public int Cy { get; set; }
        public int Rx0 { get; set; }
        public int Rx1 { get; set; }
        public List<Tuple<int, int>> Quest { get; set; }
        public List<Tuple<long, long>> Date { get; set; }

        public ItemView View { get; set; }

        public LifeInfo LifeInfo { get; set; }

        public static LifeItem LoadFromNode(Wz_Node node)
        {
            var item = new LifeItem()
            {
                ID = node.Nodes["id"].GetValueEx(0),
                Type = ParseLifeType(node.Nodes["type"].GetValueEx<string>(null)),
                X = node.Nodes["x"].GetValueEx(0),
                Y = node.Nodes["y"].GetValueEx(0),
                MobTime = node.Nodes["mobTime"].GetValueEx(0),
                Flip = node.Nodes["f"].GetValueEx(false),
                Hide = node.Nodes["hide"].GetValueEx(false),
                Fh = node.Nodes["fh"].GetValueEx(0),
                Cy = node.Nodes["cy"].GetValueEx(0),
                Rx0 = node.Nodes["rx0"].GetValueEx(0),
                Rx1 = node.Nodes["rx1"].GetValueEx(0)
            };
            item.Quest = new List<Tuple<int, int>>();
            item.Date = new List<Tuple<long, long>>();
            if (item.Type == LifeType.Npc)
            {
                string path = $@"Npc\{item.ID:D7}.img";
                var npcNode = PluginBase.PluginManager.FindWz(path);

                int? npcLink = npcNode?.FindNodeByPath(@"info\link").GetValueEx<int>();
                if (npcLink != null)
                {
                    path = $@"Npc\{npcLink.Value:D7}.img";
                    npcNode = PluginBase.PluginManager.FindWz(path);
                }

                if (npcNode != null)
                {
                    foreach (Wz_Node conditionNode in npcNode.Nodes.Where(n => n.Text.StartsWith("condition")))
                    {
                        foreach (Wz_Node questNode in conditionNode.Nodes.Where(n => n.Text.All(char.IsDigit)))
                        {
                            item.Quest.Add(new Tuple<int, int>(int.Parse(questNode.Text), Convert.ToInt32(questNode.Value)));
                        }
                        if (conditionNode.Nodes["dateStart"] != null || conditionNode.Nodes["dateEnd"] != null)
                        {
                            item.Date.Add(new Tuple<long, long>(conditionNode.Nodes["dateStart"].GetValueEx<long>(0), conditionNode.Nodes["dateEnd"].GetValueEx<long>(0)));
                        }
                    }
                }
            }
            return item;
        }

        private static LifeType ParseLifeType(string text)
        {
            switch (text)
            {
                case "m": return LifeType.Mob;
                case "n": return LifeType.Npc;
                default: return LifeType.Unknown;
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

        public enum LifeType
        {
            Unknown = 0,
            Mob = 1,
            Npc = 2
        }

    }
}

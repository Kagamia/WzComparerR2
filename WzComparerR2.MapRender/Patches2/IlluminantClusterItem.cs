using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using WzComparerR2.Rendering;
using WzComparerR2.WzLib;
using WzComparerR2.Animation;

namespace WzComparerR2.MapRender.Patches2
{
    public class IlluminantClusterItem : SceneItem
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public int Size { get; set; }
        public double Speed { get; set; }
        public int Particle { get; set; }
        public int StartPoint { get; set; }
        public int EndPoint { get; set; }

        public ItemView StartView { get; set; }
        public ItemView EndView { get; set; }

        public static IlluminantClusterItem LoadFromNode(Wz_Node node)
        {
            var item = new IlluminantClusterItem()
            {
                Start = node.Nodes["start"].GetValueEx<Wz_Vector>(null)?.ToPoint() ?? Point.Zero,
                End = node.Nodes["end"].GetValueEx<Wz_Vector>(null)?.ToPoint() ?? Point.Zero,
                Size = node.Nodes["size"].GetValueEx<int>(0),
                Speed = node.Nodes["speed"].GetValueEx<double>(0),
                Particle = node.Nodes["particle"].GetValueEx<int>(0),
                StartPoint = node.Nodes["startPoint"].GetValueEx<int>(0),
                EndPoint = node.Nodes["endPoint"].GetValueEx<int>(0),
            };
            return item;
        }

        public class ItemView
        {
            public int Time { get; set; }

            public object Animator { get; set; }
        }
    }
}
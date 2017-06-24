using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using WzComparerR2.Rendering;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.Patches2
{
    class SkyWhaleItem : SceneItem
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public int Width { get; set; }
        public double Speed { get; set; }
        public double FixSpeedShoe { get; set; }
        public double VRate { get; set; }
        public int ApplyTerm { get; set; }

        public static SkyWhaleItem LoadFromNode(Wz_Node node)
        {
            var item = new SkyWhaleItem()
            {
                Start = node.Nodes["start"].GetValueEx<Wz_Vector>(null)?.ToPoint()?? Point.Zero,
                End = node.Nodes["end"].GetValueEx<Wz_Vector>(null)?.ToPoint() ?? Point.Zero,
                Width = node.Nodes["width"].GetValueEx<int>(0),
                Speed = node.Nodes["speed"].GetValueEx<double>(0),
                FixSpeedShoe = node.Nodes["fixSpeedShoe"].GetValueEx<double>(0),
                VRate = node.Nodes["vrate"].GetValueEx<double>(0),
                ApplyTerm = node.Nodes["applyTerm"].GetValueEx<int>(0),
            };
            return item;
        }
    }
}

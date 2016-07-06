using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Animation
{
    public class FrameAnimationData 
    {
        public FrameAnimationData()
        {
            this.Frames = new List<Frame>();
        }

        public FrameAnimationData(IEnumerable<Frame> frames)
        {
            this.Frames = new List<Frame>(frames);
        }

        public List<Frame> Frames { get; private set; }

        public Rectangle GetBound()
        {
            Rectangle? bound = null;
            foreach (var frame in this.Frames)
            {
                bound = bound == null ? frame.Rectangle : Rectangle.Union(frame.Rectangle, bound.Value);
            }
            return bound ?? Rectangle.Empty;
        }

        public static FrameAnimationData CreateFromNode(Wz_Node node, GraphicsDevice graphicsDevice, GlobalFindNodeFunction findNode)
        {
            if (node == null)
                return null;
            var anime = new FrameAnimationData();
            for (int i = 0; ; i++)
            {
                Wz_Node frameNode = node.FindNodeByPath(i.ToString());

                if (frameNode == null || frameNode.Value == null)
                    break;
                Frame frame = Frame.CreateFromNode(frameNode, graphicsDevice, findNode);

                if (frame == null)
                    break;
                anime.Frames.Add(frame);
            }
            if (anime.Frames.Count > 0)
                return anime;
            else
                return null;
        }
    }
}

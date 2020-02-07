using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Animation
{
    public class MultiFrameAnimationData 
    {
        public MultiFrameAnimationData()
        {
            this.Frames = new Dictionary<string, List<Frame>>();
        }

        public Dictionary<string, List<Frame>> Frames { get; private set; }

        public Rectangle GetBound(string aniName)
        {
            Rectangle? bound = null;
            if (aniName == null)
            {
                return Rectangle.Empty;
            }
            foreach (var frame in this.Frames[aniName])
            {
                bound = bound == null ? frame.Rectangle : Rectangle.Union(frame.Rectangle, bound.Value);
            }
            return bound ?? Rectangle.Empty;
        }

        public static MultiFrameAnimationData CreateFromNode(Wz_Node node, GraphicsDevice graphicsDevice, GlobalFindNodeFunction findNode)
        {
            if (node == null)
                return null;
            var anime = new MultiFrameAnimationData();
            for (int i = 0; ; i++)
            {
                Wz_Node frameNode = node.FindNodeByPath(i.ToString());

                if (frameNode == null)
                    break;

                while (frameNode.Value is Wz_Uol)
                {
                    Wz_Uol uol = frameNode.Value as Wz_Uol;
                    Wz_Node uolNode = uol.HandleUol(frameNode);
                    if (uolNode != null)
                    {
                        frameNode = uolNode;
                    }
                }

                int delay = frameNode.Nodes["delay"].GetValueEx<int>(100);
                int count = 0;
                foreach (Wz_Node aniNode in frameNode.Nodes)
                {
                    Frame frame = Frame.CreateFromNode(aniNode, graphicsDevice, findNode);

                    if (frame == null)
                        continue;
                    frame.Delay = delay;
                    if (!anime.Frames.ContainsKey(aniNode.Text))
                    {
                        if (i != 0)
                        {
                            return null;
                        }
                        anime.Frames[aniNode.Text] = new List<Frame>();
                    }
                    anime.Frames[aniNode.Text].Add(frame);
                    count++;
                }
                if (anime.Frames.Count != count)
                {
                    return null;
                }
            }
            if (anime.Frames.Count > 0)
                return anime;
            else
                return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.Rendering;

namespace WzComparerR2.Animation
{
    public class Frame
    {
        public Frame()
        {
            this.A0 = 255;
            this.A1 = 255;
        }

        public Frame(Texture2D texture) : this(texture, null)
        {
        }

        public Frame(Texture2D atlasPage, Rectangle? atlasRect) : this()
        {
            this.Texture = atlasPage;
            this.AtlasRect = atlasRect;
        }

        public Texture2D Texture { get; set; }
        public Rectangle? AtlasRect { get; set; }
        public Wz_Png Png { get; set; }
        public Point Origin { get; set; }
        public int Z { get; set; }
        public int Delay { get; set; }
        public int A0 { get; set; }
        public int A1 { get; set; }
        public bool Blend { get; set; }

        public Rectangle Rectangle
        {
            get
            {
                if (AtlasRect != null)
                {
                    return new Rectangle(-Origin.X, -Origin.Y, AtlasRect.Value.Width, AtlasRect.Value.Height);
                }
                else if (Texture != null)
                {
                    return new Rectangle(-Origin.X, -Origin.Y, Texture.Width, Texture.Height);
                }
                else
                {
                    return Rectangle.Empty;
                }   
            }
        }

        public static Frame CreateFromNode(Wz_Node frameNode, GraphicsDevice graphicsDevice, GlobalFindNodeFunction findNode)
        {
            if (frameNode == null || frameNode.Value == null)
            {
                return null;
            }

            while (frameNode.Value is Wz_Uol)
            {
                Wz_Uol uol = frameNode.Value as Wz_Uol;
                Wz_Node uolNode = uol.HandleUol(frameNode);
                if (uolNode != null)
                {
                    frameNode = uolNode;
                }
                else
                {
                    break;
                }
            }
            if (frameNode.Value is Wz_Png)
            {
                var linkNode = frameNode.GetLinkedSourceNode(findNode);
                Wz_Png png = linkNode?.GetValue<Wz_Png>() ?? (Wz_Png)frameNode.Value;

                var frame = new Frame(png.ToTexture(graphicsDevice))
                {
                    Png = png,
                };

                foreach (Wz_Node propNode in frameNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "origin":
                            frame.Origin = (propNode.Value as Wz_Vector).ToPoint();
                            break;
                        case "delay":
                            frame.Delay = propNode.GetValue<int>();
                            break;
                        case "z":
                            frame.Z = propNode.GetValue<int>();
                            break;
                        case "a0":
                            frame.A0 = propNode.GetValue<int>();
                            break;
                        case "a1":
                            frame.A1 = propNode.GetValue<int>();
                            break;
                    }
                }

                if (frame.Delay == 0)
                {
                    frame.Delay = 100;//给予默认delay
                }
                return frame;
            }
            return null;
        }
    }
}

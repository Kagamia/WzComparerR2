using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spine;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.Rendering;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Animation
{
    public class WzSpineTextureLoader : TextureLoader
    {
        public WzSpineTextureLoader(Wz_Node topNode, GraphicsDevice graphicsDevice)
            : this(topNode, graphicsDevice, null)
        {
        }

        public WzSpineTextureLoader(Wz_Node topNode, GraphicsDevice graphicsDevice, GlobalFindNodeFunction findNodeFunc)
        {
            this.TopNode = topNode;
            this.GraphicsDevice = graphicsDevice;
            this.FindNodeFunction = findNodeFunc;
        }

        public Wz_Node TopNode { get; private set; }

        public GraphicsDevice GraphicsDevice { get; private set; }

        public GlobalFindNodeFunction FindNodeFunction { get; set; }

        public void Load(AtlasPage page, string path)
        {
            var frameNode = this.TopNode.FindNodeByPath(path);

            if (frameNode == null || frameNode.Value == null)
            {
                return;
            }

            while (frameNode.Value is Wz_Uol)
            {
                Wz_Uol uol = frameNode.Value as Wz_Uol;
                Wz_Node uolNode = uol.HandleUol(frameNode);
                if (uolNode != null)
                {
                    frameNode = uolNode;
                }
            }

            if (frameNode.Value is Wz_Png)
            {
                var linkNode = frameNode.GetLinkedSourceNode(FindNodeFunction);
                Wz_Png png = linkNode?.GetValue<Wz_Png>() ?? (Wz_Png)frameNode.Value;
                page.rendererObject = png.ToTexture(this.GraphicsDevice);
                page.width = png.Width;
                page.height = png.Height;
            }
        }

        public void Unload(object texture)
        {
            (texture as Texture2D)?.Dispose();
        }
    }
}

using System;
using WzComparerR2.Common;
using WzComparerR2.Rendering;
using WzComparerR2.WzLib;

using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Animation
{
    public class WzSpineTextureLoader : Spine.TextureLoader, Spine.V2.TextureLoader
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

        public void Load(Spine.AtlasPage page, string path)
        {
            if (this.TryLoadTexture(path, out var texture))
            {
                page.rendererObject = texture;
                page.width = texture.Width;
                page.height = texture.Height;
            }
        }

        public void Load(Spine.V2.AtlasPage page, string path)
        {
            if (this.TryLoadTexture(path, out var texture))
            {
                page.rendererObject = texture;
                page.width = texture.Width;
                page.height = texture.Height;
            }
        }

        public void Unload(object texture)
        {
            (texture as Texture2D)?.Dispose();
        }

        private bool TryLoadTexture(string path, out Texture2D texture)
        {
            texture = null;
            var frameNode = this.TopNode.FindNodeByPath(path);
            frameNode = frameNode.ResolveUol();

            if (frameNode.Value is Wz_Png)
            {
                var linkNode = frameNode.GetLinkedSourceNode(FindNodeFunction);
                Wz_Png png = (linkNode ?? frameNode).GetValue<Wz_Png>();
                texture = png.ToTexture(this.GraphicsDevice);
                return true;
            }

            return false;
        }
    }
}

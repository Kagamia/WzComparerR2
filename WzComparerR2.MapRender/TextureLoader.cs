using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.IO;
using System.IO.Compression;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender
{
    public class TextureLoader
    {
        public TextureLoader(GraphicsDevice graphicsDevice)
        {
            this.GraphicsDevice = graphicsDevice;
            this.loadedTexture = new Dictionary<string, TextureItem>(StringComparer.CurrentCultureIgnoreCase);
        }

        public GraphicsDevice GraphicsDevice { get; private set; }
        public bool IsCounting { get; private set; }

        private Dictionary<string, TextureItem> loadedTexture;

#if MapRenderV1
        public RenderFrame CreateFrame(Wz_Node frameNode)
        {
            string key = frameNode.FullPathToFile.Replace('\\', '/');
            Wz_Png png = null;

            string source = frameNode.FindNodeByPath("source").GetValueEx<string>(null);
            if (!string.IsNullOrEmpty(source))
            {
                Wz_Node sourceNode = PluginManager.FindWz(source);
                if (sourceNode != null)
                {
                    png = sourceNode.Value as Wz_Png;
                    key = sourceNode.FullPathToFile.Replace('\\', '/');
                }
            }
            else
            {
                png = frameNode.Value as Wz_Png;
            }

            if (png == null)
                return null;

            RenderFrame frame = new RenderFrame();
            Wz_Vector vec = frameNode.FindNodeByPath("origin").GetValueEx<Wz_Vector>(null);
            frame.Texture = GetTexture(png, key);
            frame.Origin = (vec == null ? new Vector2() : new Vector2(vec.X, vec.Y));
            frame.Delay = frameNode.FindNodeByPath("delay").GetValueEx<int>(100);
            frame.Z = frameNode.FindNodeByPath("z").GetValueEx<int>(0);
            frame.A0 = frameNode.FindNodeByPath("a0").GetValueEx<int>(255);
            frame.A1 = frameNode.FindNodeByPath("a1").GetValueEx<int>(frame.A0);
            return frame;
        }
#endif

        private Texture2D GetTexture(Wz_Png png, string key)
        {
            TextureItem texItem;
            if (!this.loadedTexture.TryGetValue(key, out texItem))
            {
                texItem = new TextureItem() { Texture = PngToTexture(this.GraphicsDevice, png) };
                this.loadedTexture[key] = texItem;
            }
            else
            {
            }

            if (IsCounting)
            {
                texItem.counter++;
            }
            return texItem.Texture;
        }

        public void BeginCounting()
        {
            if (!this.IsCounting)
            {
                this.IsCounting = true;
                foreach (var kv in this.loadedTexture)
                {
                    kv.Value.counter = 0;
                }
            }
        }

        public void EndCounting()
        {
            if (this.IsCounting)
            {
                this.IsCounting = false;
            }
        }

        public void ClearUnusedTexture()
        {
            List<string> removedKeys = new List<string>();
            foreach (var kv in this.loadedTexture)
            {
                if (kv.Value.Texture != null && kv.Value.counter <= 0 && !kv.Value.Texture.IsDisposed)
                {
                    kv.Value.Texture.Dispose();
                    removedKeys.Add(kv.Key);
                }
            }
            foreach (string key in removedKeys)
            {
                this.loadedTexture.Remove(key);
            }
        }

        public static Texture2D PngToTexture(GraphicsDevice device, Wz_Png png)
        {
            return WzComparerR2.Rendering.WzLibExtension.ToTexture(png, device);
        }

        private class TextureItem
        {
            public TextureItem()
            {
            }

            public Texture2D Texture { get; set; }
            public int counter { get; set; }
        }
    }
}

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
            byte[] plainData = png.GetRawData();
            if (plainData == null)
            {
                return null;
            }

            Texture2D t2d = null;

            switch (png.Form)
            {
                case 1:
                    t2d = new Texture2D(device, png.Width, png.Height, 1, TextureUsage.None, SurfaceFormat.Bgra4444);
                    t2d.SetData<byte>(plainData);
                    break;

                case 2:
                    t2d = new Texture2D(device, png.Width, png.Height, 1, TextureUsage.None, SurfaceFormat.Color);
                    t2d.SetData<byte>(plainData);
                    break;

                case 513:
                    t2d = new Texture2D(device, png.Width, png.Height, 1, TextureUsage.None, SurfaceFormat.Bgr565);
                    t2d.SetData<byte>(plainData);
                    break;

                case 517:
                    t2d = new Texture2D(device, png.Width, png.Height, 1, TextureUsage.None, SurfaceFormat.Bgr565);
                    byte[] texData = new byte[png.Width * png.Height * 2];
                    for (int j0 = 0, j1 = png.Height / 16; j0 < j1; j0++)
                    {
                        int idxTex = j0 * 16 * png.Width * 2;
                        for (int i0 = 0, i1 = png.Width / 16; i0 < i1; i0++)
                        {
                            int idx = (i0 + j0 * i1) * 2;

                            for (int k = 0; k < 16; k++)
                            {
                                texData[idxTex + i0 * 32 + k * 2] = plainData[idx];
                                texData[idxTex + i0 * 32 + k * 2 + 1] = plainData[idx + 1];
                            }
                        }
                        for (int k = 1; k < 16; k++)
                        {
                            Buffer.BlockCopy(texData, idxTex, texData, idxTex + k * png.Width * 2, png.Width * 2);
                        }
                    }
                    //int idx = 0;
                    //for (int i = 0; i < plainData.Length; i++)
                    //{
                    //    for (byte j = 0; j < 8; j++)
                    //    {
                    //        byte lumi = Convert.ToByte(((plainData[i] & (0x01 << (7 - j))) >> (7 - j)) * 0xFF);

                    //        for (int k = 0; k < 16; k++)
                    //        {
                    //            texData[idx++] = lumi;
                    //        }
                    //    }
                    //}
                    t2d.SetData<byte>(texData);
                    break;

                default: //默认从bitmap复制 二次转换
                    var bitmap = png.ExtractPng();
                    t2d = new Texture2D(device, png.Width, png.Height, 1, TextureUsage.None, SurfaceFormat.Color);

                    var bmpData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    plainData = new byte[bmpData.Stride * bmpData.Height];
                    System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, plainData, 0, plainData.Length);
                    bitmap.UnlockBits(bmpData);

                    bitmap.Dispose();
                    bitmap = null;

                    t2d.SetData(plainData);
                    break;

            }

            return t2d;
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

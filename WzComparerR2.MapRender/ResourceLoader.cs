using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using WzComparerR2.Animation;
using WzComparerR2.Rendering;
using WzComparerR2.Common;
using Spine;

namespace WzComparerR2.MapRender
{
    public class ResourceLoader : IDisposable
    {
        public ResourceLoader(IServiceProvider serviceProvider)
        {
            this.Services = serviceProvider;
            this.loadedItems = new Dictionary<string, ResourceHolder>();
            this.loadedAnimationData = new Dictionary<string, object>();
        }

        public IServiceProvider Services { get; protected set; }
        public PatchVisibility PatchVisibility { get; set; }

        protected Dictionary<string, ResourceHolder> loadedItems;
        protected Dictionary<string, object> loadedAnimationData;
        private bool isCounting;

        private GraphicsDevice GraphicsDevice
        {
            get
            {
                return this.Services.GetService<IGraphicsDeviceService>().GraphicsDevice;
            }
        }

        public virtual T Load<T>(string assetName)
        {
            return Load<T>(null, assetName);
        }

        public virtual T Load<T>(Wz_Node node)
        {
            return Load<T>(node, null);
        }

        private T Load<T>(Wz_Node node = null, string assetName = null)
        {
            if (node == null && assetName == null)
                throw new ArgumentNullException();

            assetName = assetName ?? node.FullPathToFile;

            ResourceHolder holder;
            if (!loadedItems.TryGetValue(assetName, out holder))
            {
                var res = InnerLoad(node ?? PluginManager.FindWz(assetName), typeof(T));
                if (res == null)
                {
                    return default(T);
                }

                holder = new ResourceHolder();
                holder.Resource = res;
                loadedItems[assetName] = holder;
            }

            //结算计数器
            if (isCounting)
            {
                holder.Count++;
            }

            //特殊处理
            return (T)holder.Resource;
        }

        public object LoadAnimationData(Wz_Node node)
        {
            object aniData;
            string assetName = node.FullPathToFile;
            if (!loadedAnimationData.TryGetValue(assetName, out aniData))
            {
                aniData = InnerLoadAnimationData(node);
                if (aniData == null)
                {
                    return null;
                }
                loadedAnimationData[assetName] = aniData;
            }
            return aniData;
        }

        public object LoadAnimationData(string assetName)
        {
            var node = PluginManager.FindWz(assetName);
            return node != null ? LoadAnimationData(node) : null;
        }

        public ParticleDesc LoadParticleDesc(Wz_Node node)
        {
            var desc = new ParticleDesc();
            desc.Name = node.Text;
            foreach (var pNode in node.Nodes)
            {
                switch (pNode.Text)
                {
                    case "totalParticle": desc.TotalParticle = pNode.GetValue<int>(); break;
                    case "angle": desc.Angle = pNode.GetValue<float>(); break;
                    case "angleVar": desc.AngleVar = pNode.GetValue<float>(); break;
                    case "duration": desc.Duration = pNode.GetValue<float>(); break;
                    case "blendFuncSrc": desc.BlendFuncSrc = (ParticleBlendFunc)pNode.GetValue<int>(); break;
                    case "blendFuncDst": desc.BlendFuncDst = (ParticleBlendFunc)pNode.GetValue<int>(); break;
                    case "startColor": desc.StartColor = System.Drawing.Color.FromArgb(pNode.GetValue<int>()).ToXnaColor(); break;
                    case "startColorVar": desc.StartColorVar = System.Drawing.Color.FromArgb(pNode.GetValue<int>()).ToXnaColor(); break;
                    case "endColor": desc.EndColor = System.Drawing.Color.FromArgb(pNode.GetValue<int>()).ToXnaColor(); break;
                    case "endColorVar": desc.EndColorVar = System.Drawing.Color.FromArgb(pNode.GetValue<int>()).ToXnaColor(); break;
                    case "MiddlePoint0": desc.MiddlePoint0 = pNode.GetValue<int>(); break;
                    case "MiddlePointAlpha0": desc.MiddlePointAlpha0 = pNode.GetValue<int>(); break;
                    case "MiddlePoint1": desc.MiddlePoint1 = pNode.GetValue<int>(); break;
                    case "MiddlePointAlpha1": desc.MiddlePointAlpha1 = pNode.GetValue<int>(); break;
                    case "startSize": desc.StartSize = pNode.GetValue<float>(); break;
                    case "startSizeVar": desc.StartSizeVar = pNode.GetValue<float>(); break;
                    case "endSize": desc.EndSize = pNode.GetValue<float>(); break;
                    case "endSizeVar": desc.EndSizeVar = pNode.GetValue<float>(); break;
                    case "posX": desc.PosX = pNode.GetValue<float>(); break;
                    case "posY": desc.PosY = pNode.GetValue<float>(); break;
                    case "posVarX": desc.PosVarX = pNode.GetValue<float>(); break;
                    case "posVarY": desc.PosVarY = pNode.GetValue<float>(); break;
                    case "startSpin": desc.StartSpin = pNode.GetValue<float>(); break;
                    case "startSpinVar": desc.StartSpinVar = pNode.GetValue<float>(); break;
                    case "endSpin": desc.EndSpin = pNode.GetValue<float>(); break;
                    case "endSpinVar": desc.EndSpinVar = pNode.GetValue<float>(); break;
                    case "GRAVITY":
                        {
                            var gravityDesc = new ParticleGravityDesc();
                            foreach (var pNode2 in pNode.Nodes)
                            {
                                switch (pNode2.Text)
                                {
                                    case "x": gravityDesc.X = pNode2.GetValue<float>(); break;
                                    case "y": gravityDesc.Y = pNode2.GetValue<float>(); break;
                                    case "speed": gravityDesc.Speed = pNode2.GetValue<float>(); break;
                                    case "speedVar": gravityDesc.SpeedVar = pNode2.GetValue<float>(); break;
                                    case "radialAccel": gravityDesc.RadialAccel = pNode2.GetValue<float>(); break;
                                    case "radialAccelVar": gravityDesc.RadialAccelVar = pNode2.GetValue<float>(); break;
                                    case "tangentialAccel": gravityDesc.TangentialAccel = pNode2.GetValue<float>(); break;
                                    case "tangentialAccelVar": gravityDesc.TangentialAccelVar = pNode2.GetValue<float>(); break;
                                    case "rotationIsDir": gravityDesc.RotationIsDir = pNode2.GetValue<int>() != 0; break;
                                }
                            }
                            desc.Gravity = gravityDesc;
                        }
                        break;
                    case "RADIUS":
                        {
                            var radiusDesc = new ParticleRadiusDesc();
                            foreach (var pNode2 in pNode.Nodes)
                            {
                                switch (pNode2.Text)
                                {
                                    case "startRadius": radiusDesc.StartRadius = pNode2.GetValue<float>(); break;
                                    case "startRadiusVar": radiusDesc.StartRadiusVar = pNode2.GetValue<float>(); break;
                                    case "endRadius": radiusDesc.EndRadius = pNode2.GetValue<float>(); break;
                                    case "endRadiusVar": radiusDesc.EndRadiusVar = pNode2.GetValue<float>(); break;
                                    case "rotatePerSecond": radiusDesc.RotatePerSecond = pNode2.GetValue<float>(); break;
                                    case "rotatePerSecondVar": radiusDesc.RotatePerSecondVar = pNode2.GetValue<float>(); break;
                                }
                            }
                            desc.Radius = radiusDesc;
                        }
                        break;
                    case "life": desc.Life = pNode.GetValue<float>(); break;
                    case "lifeVar": desc.LifeVar = pNode.GetValue<float>(); break;
                    case "opacityModifyRGB": desc.OpacityModifyRGB = pNode.GetValue<int>() != 0; break;
                    case "positionType": desc.PositionType = pNode.GetValue<int>(); break;
                    case "texture":
                        desc.Texture = this.LoadFrame(pNode);
                        break;
                }
            }
            return desc;
        }

        public void BeginCounting()
        {
            foreach (var kv in this.loadedItems)
            {
                kv.Value.Count = 0;
            }
            isCounting = true;
        }

        public void EndCounting()
        {
            isCounting = false;
        }

        public void ClearAnimationCache()
        {
            this.loadedAnimationData.Clear();
        }

        public void Recycle()
        {
            var preRemoved = this.loadedItems
                .Where(kv => kv.Value.Count <= 0)
                .ToList();

            foreach (var kv in preRemoved)
            {
                this.loadedItems.Remove(kv.Key);
                UnloadResource(kv.Value.Resource);
            }
        }

        public void Unload()
        {
            foreach (var kv in this.loadedItems)
            {
                UnloadResource(kv.Value.Resource);
            }

            this.loadedItems.Clear();
            this.loadedAnimationData.Clear();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void UnloadResource(object resource)
        {
            if (resource is Texture2D)
            {
                ((Texture2D)resource).Dispose();
            }
            else if (resource is TextureAtlas) //回头再考虑回收策略
            {
                ((TextureAtlas)resource).Texture.Dispose();
            }
            else if (resource is Music)
            {
                ((Music)resource).Dispose();
            }
            else
            {
                (resource as IDisposable)?.Dispose();
            }
        }

        private object InnerLoad(Wz_Node node, Type assetType)
        {
            if (assetType == typeof(TextureAtlas)) //回头再说
            {
                var png = node.GetValue<Wz_Png>();
                if (png != null)
                {
                    return new TextureAtlas(png.ToTexture(this.GraphicsDevice));
                }
            }
            else if (assetType == typeof(Texture2D))
            {
                var png = node.GetValue<Wz_Png>();
                if (png != null)
                {
                    return png.ToTexture(this.GraphicsDevice);
                }
            }
            else if (assetType == typeof(Music))
            {
                var sound = node.GetValue<Wz_Sound>();
                if (sound != null)
                {
                    return new Music(sound);
                }
            }
            return null;
        }

        private object InnerLoadAnimationData(Wz_Node node)
        {
            if (node != null)
            {
                while (node.Value is Wz_Uol)
                {
                    node = ((Wz_Uol)node.Value).HandleUol(node);
                }

                if (node.Value is Wz_Png) //单帧动画
                {
                    var aniData = new FrameAnimationData();
                    var frame = LoadFrame(node);
                    aniData.Frames.Add(frame);
                    return aniData;
                }
                else if (node.Value == null && node.Nodes.Count > 0) //分析目录
                {
                    string spine = node.Nodes["spine"].GetValueEx<string>(null);
                    if (spine != null) //读取spine动画
                    {
                        var loader = new SpineTextureLoader(this, node);
                        var atlasNode = node.Nodes[spine + ".atlas"];
                        var aniData = SpineAnimationData.CreateFromNode(atlasNode, null, loader);
                        return aniData;
                    }
                    else //读取序列帧动画
                    {
                        var frames = new List<Frame>();
                        Wz_Node frameNode;
                        for (int i = 0; (frameNode = node.Nodes[i.ToString()]) != null; i++)
                        {
                            var frame = LoadFrame(frameNode);
                            frames.Add(frame);
                        }
                        var repeat = node.Nodes["repeat"].GetValueEx<bool>();
                        return new RepeatableFrameAnimationData(frames) { Repeat = repeat };
                    }
                }
            }
            return null;
        }

        private Frame LoadFrame(Wz_Node node)
        {
            //处理uol
            while (node?.Value is Wz_Uol uol)
            {
                node = uol.HandleUol(node);
            }
            if (node == null)
            {
                return new Frame();
            }
            //寻找link
            var linkNode = node.GetLinkedSourceNode(PluginManager.FindWz);
            //加载资源
            var atlas = Load<TextureAtlas>(linkNode);
            //读取其他信息
            var frame = new Frame()
            {
                Texture = atlas.Texture,
                AtlasRect = atlas.SrcRect,
                Z = node.Nodes["z"].GetValueEx(0),
                Delay = node.Nodes["delay"].GetValueEx(100),
                Blend = node.Nodes["blend"].GetValueEx(0) != 0,
                Origin = (node.Nodes["origin"]?.Value as Wz_Vector)?.ToPoint() ?? Point.Zero
            };
            frame.A0 = node.Nodes["a0"].GetValueEx(255);
            frame.A1 = node.Nodes["a1"].GetValueEx(frame.A0);
            return frame;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Unload();
            }
        }

        ~ResourceLoader()
        {
            this.Dispose(false);
        }

        protected class ResourceHolder
        {
            public object Resource { get; set; }
            public int Count { get; set; }
        }

        private class SpineTextureLoader : Spine.TextureLoader
        {
            public SpineTextureLoader(ResourceLoader resLoader, Wz_Node topNode)
            {
                this.BaseLoader = resLoader;
                this.TopNode = topNode;
            }

            public ResourceLoader BaseLoader { get; set; }
            public Wz_Node TopNode { get; set; }

            public void Load(AtlasPage page, string path)
            {
                var frameNode = this.TopNode.FindNodeByPath(path);

                if (frameNode == null || frameNode.Value == null)
                {
                    return;
                }

                //处理uol
                while (frameNode.Value is Wz_Uol)
                {
                    frameNode = ((Wz_Uol)frameNode.Value).HandleUol(frameNode);
                }
                //寻找link
                var linkNode = frameNode.GetLinkedSourceNode(PluginManager.FindWz);
                //加载资源
                var texture = BaseLoader.Load<Texture2D>(linkNode);

                page.rendererObject = texture;
                page.width = texture.Width;
                page.height = texture.Height;
            }

            public void Unload(object texture)
            {
                //什么都不做
            }
        }
    }
}

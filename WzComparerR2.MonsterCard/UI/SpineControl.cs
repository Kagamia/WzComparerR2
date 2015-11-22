using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Spine;
using WzComparerR2.WzLib;
using WzComparerR2.Common;

namespace WzComparerR2.MonsterCard.UI
{
    public partial class SpineControl : WzComparerR2.MonsterCard.UI.Xna.GraphicsDeviceControl
    {
        public SpineControl()
        {
            InitializeComponent();
            this.timer1 = new Timer();
            this.timer1.Interval = 30;
            this.timer1.Tick += Timer1_Tick;
            this.effectSkeletonData = new List<KeyValuePair<string, SkeletonData>>();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        Timer timer1;

        /// <summary>
        /// 获取或设置GIF的绘图参数。
        /// </summary>
        internal AnimationDrawArgs AniDrawArgs { get; set; }
        public bool EnableEffect { get; set; }
        public bool ShowBoundingBox { get; set; }
        public bool ShowDrawingArea { get; set; }

        public string CurrentAniName
        {
            get
            {
                if (imgNode == null || aniItem == null)
                {
                    return null;
                }
                return imgNode.Text + "_" + aniItem.aniName;
            }
        }

        Texture2D t2dBg;
        SpriteBatch sb;
        SkeletonMeshRenderer renderer;
        Texture2D pixel;

        Wz_Node imgNode;
        SkeletonData skeletonData;
        List<KeyValuePair<string, SkeletonData>> effectSkeletonData;

        AnimateItem aniItem;

        public bool LoadSkeleton(Wz_Node imgNode, string aniName, bool? useJson = null)
        {
            if (this.imgNode != imgNode)
            {
                var data = LoadMobSkeleton(imgNode, useJson);
                if (data == null) //加载失败
                {
                    return false;
                }

                var effListNode = imgNode.FindNodeByPath("spine_effect");
                var effList = new List<KeyValuePair<string, SkeletonData>>();
                if (effListNode != null)
                {
                    foreach (var effNode in effListNode.Nodes)
                    {
                        var effData = LoadEffectSkeleton(effNode);
                        if (effData != null)
                        {
                            effList.Add(new KeyValuePair<string, SkeletonData>(effNode.Text, effData));
                        }
                    }
                }

                this.imgNode = imgNode;
                this.skeletonData = data;
                this.effectSkeletonData.Clear();
                this.effectSkeletonData.AddRange(effList);
            }

            this.aniItem = new AnimateItem(this.skeletonData);
            this.aniItem.AnimationEvent += this.AnimationState_Event;
            this.aniItem.ChangeAnime(aniName, true);
            return true;
        }

        private SkeletonData LoadMobSkeleton(Wz_Node imgNode, bool? useJson = null)
        {
            var m = Regex.Match(imgNode.Text, "^(.+).img$");
            if (!m.Success)
            {
                goto _failed;
            }

            var atlasNode = imgNode.FindNodeByPath(m.Result("$1") + ".atlas");
            if (atlasNode == null)
            {
                goto _failed;
            }

            var loadType = SkeletonLoadType.Auto;
            if (useJson != null)
            {
                loadType = useJson.Value ? SkeletonLoadType.Json : SkeletonLoadType.Binary;
            }

            var skeletonData = SpineLoader.LoadSkeleton(atlasNode, loadType,
                new WzTextureLoader(imgNode, GraphicsDevice));
            if (skeletonData == null)
            {
                goto _failed;
            }
            return skeletonData;


            _failed:
            return null;
        }

        private SkeletonData LoadEffectSkeleton(Wz_Node effectNode)
        {
            var regex = new Regex(@"^(.+)\.atlas$");
            Match m;
            var atlasNode = effectNode.Nodes.FirstOrDefault(n =>
            {
                m = regex.Match(n.Text);
                return m.Success;
            });

            if (atlasNode == null)
            {
                goto _failed;
            }

            var skeletonData = SpineLoader.LoadSkeleton(atlasNode, SkeletonLoadType.Auto,
                new WzTextureLoader(effectNode, GraphicsDevice));
            if (skeletonData == null)
            {
                goto _failed;
            }
            return skeletonData;

            _failed:
            return null;
        }

        private void AnimationState_Event(AnimateItem aniItem, AnimationState state, int trackIndex, Event e)
        {
            if (!EnableEffect)
            {
                return;
            }

            if (e.Data.Name == "effect")
            {
                var aniData = this.effectSkeletonData.SelectMany(kv =>
                    kv.Value.Animations.Select(ani => new { data = kv.Value, ani = ani })
                    ).FirstOrDefault(ani => ani.ani.Name == e.String);

                if (aniData != null)
                {
                    var effItem = new AnimateItem(aniData.data);
                    effItem.ChangeAnime(aniData.ani, false);
                    effItem.AnimationEnd += (o, s, i) =>
                    {
                        aniItem.subItems.Remove(effItem);
                    };
                    aniItem.subItems.Add(effItem);
                }
            }
            else if (e.Data.Name == "mobEffect")
            {
                var data = this.effectSkeletonData.FirstOrDefault(kv => kv.Key == e.String).Value;

                if (data != null)
                {
                    var effItem = new AnimateItem(data);
                    effItem.ChangeAnime(data.Animations.Items[0], false);
                    effItem.AnimationEnd += (o, s, i) =>
                    {
                        aniItem.subItems.Remove(effItem);
                    };
                    aniItem.subItems.Add(effItem);
                }
            }
        }

        protected override void Initialize()
        {
            this.sb = new SpriteBatch(GraphicsDevice);
            this.t2dBg = CreateBgTexture();
            this.pixel = new Texture2D(this.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            pixel.SetData<uint>(new uint[] { 0xffffffff });
            this.renderer = new SkeletonMeshRenderer(GraphicsDevice);
            this.renderer.PremultipliedAlpha = false;
            this.timer1.Enabled = true;
        }

        protected override void Draw()
        {
            GraphicsDevice.Clear(Color.White);
            FillBackgrnd();

            int x, y;
            if (this.AniDrawArgs != null)
            {
                x = AniDrawArgs.OriginX;
                y = AniDrawArgs.OriginY;
            }
            else
            {
                x = this.Width / 2;
                y = this.Height;
            }

            if (aniItem != null)
            {
                aniItem.SetPosition(x, y);
                aniItem.Update();
                
               
                for (int i = aniItem.subItems.Count - 1; i >= 0; i--)
                {
                    var eff = aniItem.subItems[i];
                    eff.SetPosition(x, y);
                    eff.Update();
                }

                //skeleton.RootBone.ScaleX = 1f;
                //skeleton.RootBone.ScaleY = 1f;
                //skeleton.UpdateWorldTransform();

                //渲染标记
                sb.Begin(SpriteBlendMode.AlphaBlend);
                if (ShowDrawingArea)
                {
                    Rectangle rect = GetAnimateBound(aniItem);
                    sb.Draw(pixel, rect, new Color(Color.Gray, 0.3f));
                }
                if (ShowBoundingBox)
                {
                    var bound = new SkeletonBounds();
                    bound.Update(aniItem.skeleton, true);
                    sb.Draw(pixel, new Rectangle((int)bound.MinX, (int)bound.MinY, (int)bound.Width, (int)bound.Height), new Color(Color.Red, 0.3f));
                }
                sb.End();

                //渲染骨骼
                renderer.Begin();
                renderer.Draw(aniItem.skeleton);
                foreach (var eff in aniItem.subItems)
                {
                    renderer.Draw(eff.skeleton);
                }
                renderer.End();


            }
        }

        public System.Drawing.Bitmap SaveAsGif(int frameDelay)
        {
            if (this.aniItem == null || this.skeletonData == null)
            {
                return null;
            }

            var renderer = new SkeletonMeshRenderer(this.GraphicsDevice);
            var shader = CreateAlphaHandlerEffect();
            renderer.PremultipliedAlpha = false;

            Gif gif = new Gif();
            List<byte[]> bmpCache = new List<byte[]>();

            var aniItem = new AnimateItem(this.skeletonData);
            aniItem.AnimationEvent += this.AnimationState_Event;
            var ani = this.skeletonData.FindAnimation(this.aniItem.aniName);
            aniItem.ChangeAnime(ani, false);
            aniItem.SetPosition(0, 0);

            int length = (int)Math.Round(ani.Duration * 1000);
            int time = 0;
            do
            {
                aniItem.aniState.Apply(aniItem.skeleton);
                aniItem.skeleton.UpdateWorldTransform();
                for (int i = aniItem.subItems.Count - 1; i >= 0; i--)
                {
                    var eff = aniItem.subItems[i];
                    eff.SetPosition(0, 0);
                    eff.aniState.Apply(eff.skeleton);
                    eff.skeleton.UpdateWorldTransform();
                }
                Rectangle rect = GetAnimateBound(aniItem);

                if (rect.Width > 0 && rect.Height > 0)
                {
                    RenderTarget2D rt = new RenderTarget2D(this.GraphicsDevice, rect.Width, rect.Height, 1, SurfaceFormat.Color, RenderTargetUsage.PreserveContents);
                    DepthStencilBuffer stencil = new DepthStencilBuffer(this.GraphicsDevice, rect.Width, rect.Height, DepthFormat.Depth24Stencil8);

                    //渲染
                    GraphicsDevice.SetRenderTarget(0, rt);
                    GraphicsDevice.DepthStencilBuffer = stencil;
                    GraphicsDevice.Clear(Color.TransparentBlack);
                    renderer.Effect.World = Matrix.CreateTranslation(-rect.Left, -rect.Top, 0);
                    renderer.Begin();
                    renderer.Draw(aniItem.skeleton);
                    foreach (var eff in aniItem.subItems)
                    {
                        renderer.Draw(eff.skeleton);
                    }
                    renderer.End();

                    //处理透明度
                    RenderTarget2D rt2 = new RenderTarget2D(this.GraphicsDevice, rect.Width, rect.Height, 1, SurfaceFormat.Color, RenderTargetUsage.PreserveContents);
                    GraphicsDevice.SetRenderTarget(0, rt2);
                    GraphicsDevice.Clear(Color.TransparentBlack);

                    shader.Begin(SaveStateMode.None);
                    shader.Techniques[0].Passes[0].Begin();
                    sb.Begin(SpriteBlendMode.None);
                    sb.Draw(rt.GetTexture(), new Rectangle(0, 0, rect.Width, rect.Height), Color.White);
                    sb.End();
                    shader.Techniques[0].Passes[0].End();
                    shader.End();

                    //结束
                    GraphicsDevice.SetRenderTarget(0, null);
                    GraphicsDevice.DepthStencilBuffer = null;

                    //保存
                    byte[] bmpData = new byte[rt.Width * rt.Height * 4];
                    rt2.GetTexture().GetData(bmpData);
                    bmpCache.Add(bmpData);
                    unsafe
                    {
                        fixed (byte* pData = bmpData)
                        {
                            var bmp = new System.Drawing.Bitmap(rt.Width, rt.Height, rt.Width * 4,
                                System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                                new IntPtr(pData));
                            int delay = Math.Min(length - time, frameDelay);
                            gif.Frames.Add(new GifFrame(bmp, new System.Drawing.Point(-rect.Left, -rect.Top), delay));
                        }
                    }

                    //清理
                    stencil.Dispose();
                    rt.Dispose();
                    rt2.Dispose();
                }

                //更新时间轴
                time += frameDelay;
                aniItem.aniState.Update(frameDelay / 1000f);
                for (int i = aniItem.subItems.Count - 1; i >= 0; i--)
                {
                    aniItem.subItems[i].aniState.Update(frameDelay / 1000f);
                }
            }
            while (time < length);

            shader.Dispose();

            var gifBmp = gif.EncodeGif2(Setting.GifBackGroundColor, Setting.GifMinAlphaMixed);
            bmpCache = null;
            return gifBmp;
            //this.effItems.Clear();
        }

        private Rectangle GetAnimateBound(AnimateItem aniItem)
        {
            var bounds = ModelBounds.Empty;
            UpdateBounds(ref bounds, aniItem.skeleton);
            foreach (var eff in aniItem.subItems)
            {
                UpdateBounds(ref bounds, eff.skeleton);
            }
            return bounds.GetBound();
        }

        private void UpdateBounds(ref ModelBounds bounds, Skeleton skeleton)
        {
            float[] vertices = new float[8];
            var drawOrder = skeleton.DrawOrder;
            for (int i = 0, n = drawOrder.Count; i < n; i++)
            {
                Slot slot = drawOrder.Items[i];
                Attachment attachment = slot.Attachment;
                if (attachment is RegionAttachment)
                {
                    RegionAttachment region = (RegionAttachment)attachment;
                    region.ComputeWorldVertices(slot.Bone, vertices);
                    bounds.Update(vertices, 8);
                }
                else if (attachment is MeshAttachment)
                {
                    MeshAttachment mesh = (MeshAttachment)attachment;
                    int vertexCount = mesh.Vertices.Length;
                    if (vertices.Length < vertexCount) vertices = new float[vertexCount];
                    mesh.ComputeWorldVertices(slot, vertices);
                    bounds.Update(vertices, vertexCount);
                }
                else if (attachment is SkinnedMeshAttachment)
                {
                    SkinnedMeshAttachment mesh = (SkinnedMeshAttachment)attachment;
                    int vertexCount = mesh.UVs.Length;
                    if (vertices.Length < vertexCount) vertices = new float[vertexCount];
                    mesh.ComputeWorldVertices(slot, vertices);
                    bounds.Update(vertices, vertexCount);
                }
            }
        }

        private void FillBackgrnd()
        {
            sb.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            sb.Draw(t2dBg, Vector2.Zero, new Rectangle(0, 0, this.Width, this.Height), Color.White);
            sb.End();
        }

        private Texture2D CreateBgTexture()
        {
            Texture2D t2d = new Texture2D(GraphicsDevice, 16, 16, 1, TextureUsage.Tiled, SurfaceFormat.Color);
            Color[] color = new Color[t2d.Width * t2d.Height];
            for (int y = 0; y < t2d.Height / 2; y++)
            {
                for (int x = t2d.Width / 2; x < t2d.Width; x++)
                {
                    color[y * t2d.Width + x] = Color.LightGray;
                }
            }

            for (int y = t2d.Height / 2; y < t2d.Height; y++)
            {
                for (int x = 0; x < t2d.Width / 2; x++)
                {
                    color[y * t2d.Width + x] = Color.LightGray;
                }
            }

            t2d.SetData<Color>(color);
            return t2d;
        }

        private Texture2D CreatePixel()
        {
            var pixel = new Texture2D(this.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            pixel.SetData<uint>(new uint[] { 0xffffffff });
            return pixel;
        }

        private Effect CreateAlphaHandlerEffect()
        {
            string effSrc = @"
sampler sampler1 : register(s0);

float4 PS(float2 texCoord: TEXCOORD0) : COLOR0
{
    float4 color = tex2D(sampler1, texCoord);
    if (color.a = 0) color.rgb = 0;
    else color.rgb /= color.a;
    return color;
}

technique Technique0
{
	pass Pass0
	{   
		PixelShader = compile ps_2_0 PS();
	}
}
";

            var compileEff = Effect.CompileEffectFromSource(effSrc, null, null, CompilerOptions.None, TargetPlatform.Windows);
            if (compileEff.Success)
            {
                return new Effect(this.GraphicsDevice, compileEff.GetEffectCode(), CompilerOptions.None, null);
            }
            return null;
        }

        private class WzTextureLoader : TextureLoader
        {
            public WzTextureLoader(Wz_Node imgNode, GraphicsDevice device)
            {
                this.Node = imgNode;
                this.GraphicsDevice = device;
            }

            public Wz_Node Node { get; private set; }
            public GraphicsDevice GraphicsDevice { get; set; }

            public void Load(AtlasPage page, string path)
            {
                var node = this.Node.FindNodeByPath(path);
                var bmpOrig = BitmapOrigin.CreateFromNode(node, PluginBase.PluginManager.FindWz);
                if (bmpOrig.Bitmap != null)
                {
                    var bmp = bmpOrig.Bitmap;
                    var t2d = new Texture2D(GraphicsDevice, bmp.Width, bmp.Height, 1, TextureUsage.None, SurfaceFormat.Color);
                    {
                        var data = bmp.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                        byte[] buffer = new byte[data.Stride * data.Height];
                        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                        bmp.UnlockBits(data);
                        t2d.SetData(buffer);
                    }
                    page.rendererObject = t2d;
                    page.width = bmp.Width;
                    page.height = bmp.Height;

                    bmp.Dispose();
                }
            }

            public void Unload(object texture)
            {
                (texture as Texture2D)?.Dispose();
            }
        }

        private class AnimateItem
        {
            public AnimateItem(SkeletonData data)
            {
                this.data = data;
                this.subItems = new List<AnimateItem>();
            }

            public Skeleton skeleton;
            public AnimationState aniState;
            public float lastTime;
            public Stopwatch sw;
            public string aniName;

            public event EventDelegate AnimationEvent;
            public event StartEndDelegate AnimationEnd;

            public List<AnimateItem> subItems;

            private SkeletonData data;

            public void ChangeAnime(string aniName, bool loop)
            {
                this.skeleton = new Skeleton(this.data);
                this.aniState = new AnimationState(new AnimationStateData(data));
                this.aniName = aniName;
                this.aniState.SetAnimation(0, aniName, loop);
                this.InitAnime();
            }

            public void ChangeAnime(Animation ani, bool loop)
            {
                this.skeleton = new Skeleton(this.data);
                this.aniState = new AnimationState(new AnimationStateData(data));
                this.aniName = ani.Name;
                this.aniState.SetAnimation(0, ani, loop);
                this.InitAnime();
            }

            private void InitAnime()
            {
                this.aniState.Event += OnAnimationEvent;
                this.aniState.End += OnAnimationEnd;
                sw = Stopwatch.StartNew();
                lastTime = 0;
            }

            public void Update()
            {
                float animationTime = sw == null ? 0f : (float)sw.Elapsed.TotalSeconds;
                this.aniState.Update(animationTime - lastTime);
                lastTime = animationTime;

                this.aniState.Apply(skeleton);
                skeleton.UpdateWorldTransform();
            }

            public void SetPosition(int x, int y)
            {
                skeleton.X = x;
                skeleton.Y = y;
            }

            private void OnAnimationEvent(AnimationState state, int trackIndex, Event e)
            {
                if (this.AnimationEvent != null)
                {
                    this.AnimationEvent(this, state, trackIndex, e);
                }
            }

            private void OnAnimationEnd(AnimationState state, int trackIndex)
            {
                if (this.AnimationEnd != null)
                {
                    this.AnimationEnd(this, state, trackIndex);
                }
            }

            public delegate void EventDelegate(AnimateItem aniItem, AnimationState state, int trackIndex, Event e);
            public delegate void StartEndDelegate(AnimateItem aniItem, AnimationState state, int trackIndex);
        }

        private struct ModelBounds
        {
            public float minX, minY, maxX, maxY;

            public bool IsEmpty
            {
                get { return minX >= maxX || minY >= maxY; }
            }

            public void Update(float[] vertices, int count)
            {
                int i = 0;
                if (count % 4 != 0)
                {
                    if (vertices[0] > maxX) maxX = vertices[0];
                    if (vertices[0] < minX) minX = vertices[0];
                    if (vertices[1] > maxY) maxY = vertices[1];
                    if (vertices[1] < minY) minY = vertices[1];
                    i += 2;
                }

                while (i < count)
                {
                    if (vertices[i] > vertices[i + 2])
                    {
                        if (vertices[i] > maxX) maxX = vertices[i];
                        if (vertices[i + 2] < minX) minX = vertices[i + 2];
                    }
                    else
                    {
                        if (vertices[i + 2] > maxX) maxX = vertices[i + 2];
                        if (vertices[i] < minX) minX = vertices[i];
                    }

                    if (vertices[i + 1] > vertices[i + 3])
                    {
                        if (vertices[i + 1] > maxY) maxY = vertices[i + 1];
                        if (vertices[i + 3] < minY) minY = vertices[i + 3];
                    }
                    else
                    {
                        if (vertices[i + 3] > maxY) maxY = vertices[i + 3];
                        if (vertices[i + 1] < minY) minY = vertices[i + 1];
                    }

                    i += 4;
                }
            }

            public Rectangle GetBound()
            {
                if (IsEmpty)
                {
                    return new Rectangle();
                }

                return new Rectangle((int)Math.Round(minX),
                    (int)Math.Round(minY),
                    (int)Math.Round(maxX - minX),
                    (int)Math.Round(maxY - minY));
            }

            public static ModelBounds Empty
            {
                get
                {
                    var b = new ModelBounds();
                    b.minX = b.minY = float.MaxValue;
                    b.maxX = b.maxY = float.MinValue;
                    return b;
                }
            }
        }
    }
}

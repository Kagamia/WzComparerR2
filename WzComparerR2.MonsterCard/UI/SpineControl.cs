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
            this.effItems = new List<AnimateItem>();
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

        Texture2D t2dBg;
        BasicEffect eff;
        SpriteBatch sb;
        SkeletonMeshRenderer renderer;

        Wz_Node imgNode;
        SkeletonData skeletonData;
        List<KeyValuePair<string, SkeletonData>> effectSkeletonData;

        AnimateItem aniItem;
        List<AnimateItem> effItems;


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
            this.effItems.Clear();
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


        private void AnimationState_Event(AnimationState state, int trackIndex, Event e)
        {
            if (e.Data.Name == "effect")
            {
                var aniData = this.effectSkeletonData.SelectMany(kv =>
                    kv.Value.Animations.Items.Select(ani => new { data = kv.Value, ani = ani })
                    ).FirstOrDefault(ani => ani.ani.Name == e.String);

                if (aniData != null)
                {
                    var effItem = new AnimateItem(aniData.data);
                    effItem.ChangeAnime(aniData.ani, false);
                    effItem.AnimationEnd += (s, i) =>
                    {
                        this.effItems.Remove(effItem);
                    };
                    this.effItems.Add(effItem);
                }
            }
            else if (e.Data.Name == "mobEffect")
            {
                var data = this.effectSkeletonData.FirstOrDefault(kv => kv.Key == e.String).Value;

                if (data != null)
                {
                    var effItem = new AnimateItem(data);
                    effItem.ChangeAnime(data.Animations.Items[0], false);
                    effItem.AnimationEnd += (s, i) =>
                    {
                        this.effItems.Remove(effItem);
                    };
                    this.effItems.Add(effItem);
                }
            }
        }

        protected override void Initialize()
        {
            this.sb = new SpriteBatch(GraphicsDevice);
            this.eff = new BasicEffect(GraphicsDevice, null);
            this.t2dBg = CreateBgTexture();
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

                for (int i = this.effItems.Count - 1; i >= 0; i--)
                {
                    var eff = this.effItems[i];
                    eff.SetPosition(x, y);
                    eff.Update();
                }

                //skeleton.RootBone.ScaleX = 1f;
                //skeleton.RootBone.ScaleY = 1f;
                //skeleton.UpdateWorldTransform();

                renderer.Begin();
                renderer.Draw(aniItem.skeleton);
                foreach (var eff in this.effItems)
                {
                    renderer.Draw(eff.skeleton);
                }
                renderer.End();
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
            }

            public Skeleton skeleton { get; private set; }
            public AnimationState aniState { get; private set; }
            public float lastTime { get; private set; }
            public Stopwatch sw { get; private set; }

            public event AnimationState.EventDelegate AnimationEvent;
            public event AnimationState.StartEndDelegate AnimationEnd;

            private SkeletonData data;

            public void ChangeAnime(string aniName, bool loop)
            {
                this.skeleton = new Skeleton(this.data);
                this.aniState = new AnimationState(new AnimationStateData(data));
                this.aniState.SetAnimation(0, aniName, loop);
                this.InitAnime();
            }

            public void ChangeAnime(Animation ani, bool loop)
            {
                this.skeleton = new Skeleton(this.data);
                this.aniState = new AnimationState(new AnimationStateData(data));
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
                    this.AnimationEvent(state, trackIndex, e);
                }
            }

            private void OnAnimationEnd(AnimationState state, int trackIndex)
            {
                if (this.AnimationEnd != null)
                {
                    this.AnimationEnd(state, trackIndex);
                }
            }
        }
    }
}

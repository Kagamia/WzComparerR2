using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using WzComparerR2.Common;
using WzComparerR2.Rendering;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.MapRender.UI;
using WzComparerR2.Animation;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System.Runtime.InteropServices;

using Form = System.Windows.Forms.Form;
#region USING_EK
using KeyBinding = EmptyKeys.UserInterface.Input.KeyBinding;
using RelayCommand = EmptyKeys.UserInterface.Input.RelayCommand;
using KeyGesture = EmptyKeys.UserInterface.Input.KeyGesture;
using KeyCode = EmptyKeys.UserInterface.Input.KeyCode;
using ModifierKeys = EmptyKeys.UserInterface.Input.ModifierKeys;
#endregion

namespace WzComparerR2.MapRender
{
    public partial class FrmMapRender2 : Game
    {
        public FrmMapRender2(Wz_Image img)
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.DeviceCreated += Graphics_DeviceCreated;
            this.mapImg = img;
            this.MaxElapsedTime = TimeSpan.MaxValue;
            this.IsFixedTimeStep = false;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60);
            this.InactiveSleepTime = TimeSpan.FromSeconds(1.0 / 30);
            this.IsMouseVisible = true;

            this.Content = new WcR2ContentManager(this.Services);
            this.patchVisibility = new PatchVisibility();
            this.patchVisibility.FootHoldVisible = false;
            this.patchVisibility.LadderRopeVisible = false;
            this.patchVisibility.SkyWhaleVisible = false;
            GameExt.FixKeyboard(this);

            var form = Form.FromHandle(this.Window.Handle) as Form;
            form.Load += Form_Load;
            form.FormClosed += Form_FormClosed;
        }

        private void Form_Load(object sender, EventArgs e)
        {
            var form = (Form)sender;
            form.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            form.SetDesktopLocation(0, 0);
        }

        private void Form_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
        {
            GameExt.EnsureGameExit(this);
        }

        private void Graphics_DeviceCreated(object sender, EventArgs e)
        {
            this.engine = new WcR2Engine(this.GraphicsDevice,
                graphics.PreferredBackBufferWidth,
                graphics.PreferredBackBufferHeight);

            WcR2Engine.FixEKBugs();
        }

        public StringLinker StringLinker { get; set; }

        GraphicsDeviceManager graphics;
        Wz_Image mapImg;
        RenderEnv renderEnv;
        MapData mapData;
        ResourceLoader resLoader;
        MeshBatcher batcher;
        PatchVisibility patchVisibility;

        bool prepareCapture;
        Task captureTask;
        Resolution resolution;

        List<ItemRect> allItems = new List<ItemRect>();
        MapRenderUIRoot ui;
        Tooltip2 tooltip;
        WcR2Engine engine;
        Music bgm;

        protected override void Initialize()
        {
            this.renderEnv = new RenderEnv(this, this.graphics);
            this.batcher = new MeshBatcher(this.GraphicsDevice);
            this.ui = new MapRenderUIRoot();
            this.ui.LoadContents(this.Content);
            this.BindingUIInput();
            this.tooltip = new Tooltip2(this.GraphicsDevice);
            this.tooltip.StringLinker = this.StringLinker;
            SwitchResolution(Resolution.Window_800_600);
            base.Initialize();
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            base.OnDeactivated(sender, args);
        }

        private void BindingUIInput()
        {
            //键盘事件
            //切换分辨率
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => SwitchResolution()), KeyCode.Enter, ModifierKeys.Alt));

            //开关小地图
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.ui.Minimap.Toggle()), KeyCode.M, ModifierKeys.None) { IsRepeatEnabled = true });

            //截图
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => { if (CanCapture()) prepareCapture = true; }), KeyCode.Scroll, ModifierKeys.None));

            //层隐藏
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.BackVisible = !this.patchVisibility.BackVisible), KeyCode.D1, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.ReactorVisible = !this.patchVisibility.ReactorVisible), KeyCode.D2, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.ObjVisible = !this.patchVisibility.ObjVisible), KeyCode.D3, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.TileVisible = !this.patchVisibility.TileVisible), KeyCode.D4, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.NpcVisible = !this.patchVisibility.NpcVisible), KeyCode.D5, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.MobVisible = !this.patchVisibility.MobVisible), KeyCode.D6, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ =>
            {
                var visible = this.patchVisibility.FootHoldVisible;
                this.patchVisibility.FootHoldVisible = !visible;
                this.patchVisibility.LadderRopeVisible = !visible;
                this.patchVisibility.SkyWhaleVisible = !visible;
            }), KeyCode.D7, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ =>
            {
                var portals = this.mapData.Scene.Descendants().OfType<ContainerNode>().SelectMany(container => container.Slots).OfType<PortalItem>();
                if (!this.patchVisibility.PortalVisible)
                {
                    this.patchVisibility.PortalVisible = true;
                    this.patchVisibility.PortalInEditMode = false;
                    foreach (var item in portals)
                    {
                        item.View.IsEditorMode = false;
                    }
                }
                else if (!this.patchVisibility.PortalInEditMode)
                {
                    this.patchVisibility.PortalInEditMode = true;
                    foreach (var item in portals)
                    {
                        item.View.IsEditorMode = true;
                    }
                }
                else
                {
                    this.patchVisibility.PortalVisible = false;
                }
            }), KeyCode.D8, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.FrontVisible = !this.patchVisibility.FrontVisible), KeyCode.D9, ModifierKeys.Control));

            //移动操作
            #region 移动操作
            {
                //键盘移动
                int boostMoveFlag = 0;
                var direction1 = Vector2.Zero;

                Action<Vector2> calcKeyboardMoveDir = dir =>
                {
                    if (dir.X != 0)
                    {
                        var preMove = dir.X * 10 * (boostMoveFlag != 0 ? 3 : 1);

                        if (Math.Sign(preMove) * Math.Sign(direction1.X) == -1
                            && Math.Abs(direction1.X) <= Math.Abs(preMove))
                        {
                            direction1.X = 0;
                        }
                        else
                        {
                            direction1.X += preMove;
                        }
                    }
                    if (dir.Y != 0)
                    {
                        var preMove = dir.Y * 10 * (boostMoveFlag != 0 ? 3 : 1);

                        if (Math.Sign(preMove) * Math.Sign(direction1.Y) == -1
                            && Math.Abs(direction1.Y) <= Math.Abs(preMove))
                        {
                            direction1.Y = 0;
                        }
                        else
                        {
                            direction1.Y += preMove;
                        }
                    }
                };

                //键盘动量减速
                Action keyboardMoveSlowDown = () =>
                {
                    if (direction1.X > 2 || direction1.X < -2) direction1.X *= 0.98f;
                    else direction1.X = 0;
                    if (direction1.Y > 2 || direction1.Y < 2) direction1.Y *= 0.98f;
                    else direction1.Y = 0;
                };

                this.ui.PreviewKeyDown += (o, e) =>
                {
                    switch (e.Key)
                    {
                        case KeyCode.Up:
                            calcKeyboardMoveDir(new Vector2(0, -1));
                            break;
                        case KeyCode.Down:
                            calcKeyboardMoveDir(new Vector2(0, 1));
                            break;
                        case KeyCode.Left:
                            calcKeyboardMoveDir(new Vector2(-1, 0));
                            break;
                        case KeyCode.Right:
                            calcKeyboardMoveDir(new Vector2(1, 0));
                            break;

                        case KeyCode.LeftControl:
                            boostMoveFlag |= 0x01;
                            break;
                        case KeyCode.RightControl:
                            boostMoveFlag |= 0x02;
                            break;
                    }
                };
                this.ui.KeyUp += (o, e) =>
                {
                    switch (e.Key)
                    {
                        case KeyCode.LeftControl:
                            boostMoveFlag &= ~0x01;
                            break;
                        case KeyCode.RightControl:
                            boostMoveFlag &= ~0x02;
                            break;
                    }
                };

                //鼠标移动
                bool isMouseDown = false;
                var direction2 = Vector2.Zero;

                Action<EmptyKeys.UserInterface.Input.MouseEventArgs> calcMouseMoveDir = e =>
                {
                    var mousePos = e.GetPosition();
                    Vector2 vec = new Vector2(2 * mousePos.X / this.ui.Width - 1, 2 * mousePos.Y / this.ui.Height - 1);
                    var distance = vec.Length();
                    if (distance >= 0.4f)
                    {
                        vec *= (distance - 0.4f) / distance;
                    }
                    else
                    {
                        vec = Vector2.Zero;
                    }
                    direction2 = vec * 20;
                };

                this.ui.MouseDown += (o, e) =>
                {
                    if (e.ChangedButton == EmptyKeys.UserInterface.Input.MouseButton.Right)
                    {
                        isMouseDown = true;
                        calcMouseMoveDir(e);
                    }
                };
                this.ui.MouseMove += (o, e) =>
                {
                    if (isMouseDown)
                    {
                        calcMouseMoveDir(e);
                    }
                };
                this.ui.MouseUp += (o, e) =>
                {
                    if (e.ChangedButton == EmptyKeys.UserInterface.Input.MouseButton.Right)
                    {
                        isMouseDown = false;
                        direction2 = Vector2.Zero;
                    }
                };

                //更新事件
                this.ui.InputUpdated += (o, e) =>
                {
                    this.renderEnv.Camera.Center += direction1 + direction2 * ((boostMoveFlag != 0) ? 3 : 1);
                    keyboardMoveSlowDown();
                };
            }
            #endregion

            //点击事件
            UIHelper.RegisterClickEvent<SceneItem>(this.ui.ContentControl,
                (sender, point) => {
                    int x = (int)point.X;
                    int y = (int)point.Y;
                    var mouseTarget = this.allItems.Reverse<ItemRect>().FirstOrDefault(item =>
                    {
                        return item.rect.Contains(x, y) && (item.item is PortalItem || item.item is ReactorItem);
                    });
                    return mouseTarget.item;
                },
                this.OnSceneItemClick);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            this.resLoader = new ResourceLoader(this.Services);
            this.mapData = new MapData();

            if (this.mapImg != null)
            {
                LoadMap(this.mapImg);
            }
        }

        private void LoadMap(Wz_Image mapImg)
        {
            //加载地图数据
            this.mapData.Load(mapImg.Node, resLoader);
            this.mapData.PreloadResource(resLoader);

            //同步UI
            this.renderEnv.Camera.WorldRect = mapData.VRect;

            //加载bgm
            if (!string.IsNullOrEmpty(this.mapData.Bgm))
            {
                if (this.bgm != null)
                {
                    this.bgm.Stop();
                    this.bgm.Dispose();
                    this.bgm = null;
                }

                var path = new List<string>() { "Sound" };
                path.AddRange(this.mapData.Bgm.Split('/'));
                path[1] += ".img";
                var bgmNode = PluginManager.FindWz(string.Join("\\", path));
                if (bgmNode != null)
                {
                    this.bgm = resLoader.Load<Music>(bgmNode);
                    if (this.bgm != null)
                    {
                        this.bgm.IsLoop = true;
                        this.bgm.Play();
                    }
                }
            }

            StringResult sr;
            if (this.mapData.ID != null && this.StringLinker != null
                && StringLinker.StringMap.TryGetValue(this.mapData.ID.Value, out sr))
            {
                this.ui.Minimap.StreetName = sr["streetName"];
                this.ui.Minimap.MapName = sr["mapName"];
            }
            else
            {
                this.ui.Minimap.StreetName = null;
                this.ui.Minimap.MapName = null;
            }

            if (this.mapData.MiniMap.MapMark != null)
            {
                this.ui.Minimap.MapMark = engine.Renderer.CreateTexture(this.mapData.MiniMap.MapMark);
            }
            else
            {
                this.ui.Minimap.MapMark = null;
            }

            if (this.mapData.MiniMap.Canvas != null)
            {
                this.ui.Minimap.MinimapCanvas = engine.Renderer.CreateTexture(this.mapData.MiniMap.Canvas);
            }
            else
            {
                this.ui.Minimap.MinimapCanvas = null;
            }

            this.ui.Minimap.Icons.Clear();
            foreach(var portal in this.mapData.Scene.Fly.Portal.Slots.OfType<PortalItem>())
            {
                switch (portal.Type)
                {
                    case 2:
                    case 7:
                        object tooltip = portal.Tooltip;
                        if (tooltip == null && portal.ToMap != null && portal.ToMap != 999999999
                            && StringLinker.StringMap.TryGetValue(portal.ToMap.Value, out sr))
                        {
                            tooltip = sr["mapName"];
                        }
                        this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                        {
                            IconType = UIMinimap2.IconType.Portal,
                            Tooltip = tooltip,
                            WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                        });
                        break;

                    case 10:
                        this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                        {
                            IconType = UIMinimap2.IconType.Transport,
                            Tooltip = portal.Tooltip,
                            WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                        });
                        break;
                }
            }

            if (mapData.MiniMap.Width > 0 && mapData.MiniMap.Height > 0)
            {
                this.ui.Minimap.MapRegion = new Rectangle(-mapData.MiniMap.CenterX, -mapData.MiniMap.CenterY, mapData.MiniMap.Width, mapData.MiniMap.Height).ToRect();
            }
            else
            {
                this.ui.Minimap.MapRegion = this.mapData.VRect.ToRect();
            }
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            this.renderEnv.Input.Update(gameTime);
            this.ui.UpdateInput(gameTime.ElapsedGameTime.TotalMilliseconds);

            //需要手动更新数据部分
            this.renderEnv.Camera.AdjustToWorldRect();
            {
                var rect = this.renderEnv.Camera.ClipRect;
                this.ui.Minimap.CameraViewPort = new EmptyKeys.UserInterface.Rect(rect.X, rect.Y, rect.Width, rect.Height);
            }
            //更新ui
            this.ui.UpdateLayout(gameTime.ElapsedGameTime.TotalMilliseconds);
            //更新场景
            UpdateAllItems(mapData.Scene, gameTime.ElapsedGameTime);
            //更新tooltip
            UpdateTooltip();
        }

        protected override void Draw(GameTime gameTime)
        {
            if (prepareCapture)
            {
                Capture(gameTime);
            }

            this.GraphicsDevice.Clear(Color.Black);
            DrawScene(gameTime);
            DrawTooltipItems(gameTime);
            this.ui.Draw(gameTime.ElapsedGameTime.TotalMilliseconds);
            this.tooltip.Draw(gameTime, renderEnv);
            base.Draw(gameTime);
        }

        #region 截图相关

        private bool CanCapture()
        {
            return (captureTask == null || captureTask.IsCompleted) && !prepareCapture;
        }

        private void Capture(GameTime gameTime)
        {
            var oldTarget = GraphicsDevice.GetRenderTargets();

            //检查显卡支持纹理大小
            var maxTextureWidth = 4096;
            var maxTextureHeight = 4096;

            Rectangle oldRect = this.renderEnv.Camera.WorldRect;
            int width = Math.Min(oldRect.Width, maxTextureWidth);
            int height = Math.Min(oldRect.Height, maxTextureHeight);
            this.renderEnv.Camera.UseWorldRect = true;

            var target2d = new RenderTarget2D(this.GraphicsDevice, width, height, false, SurfaceFormat.Bgra32, DepthFormat.None);

            //计算一组截图区
            int horizonBlock = (int)Math.Ceiling(1.0 * oldRect.Width / width);
            int verticalBlock = (int)Math.Ceiling(1.0 * oldRect.Height / height);
            byte[,][] picBlocks = new byte[horizonBlock, verticalBlock][];
            for (int j = 0; j < verticalBlock; j++)
            {
                for (int i = 0; i < horizonBlock; i++)
                {
                    //计算镜头区域
                    this.renderEnv.Camera.WorldRect = new Rectangle(
                        oldRect.X + i * width,
                        oldRect.Y + j * height,
                        width,
                        height);

                    //绘制
                    GraphicsDevice.SetRenderTarget(target2d);
                    GraphicsDevice.Clear(Color.Black);
                    DrawScene(gameTime);
                    GraphicsDevice.SetRenderTarget(null);
                    //保存
                    Texture2D t2d = target2d;
                    byte[] data = new byte[target2d.Width * target2d.Height * 4];
                    t2d.GetData(data);
                    picBlocks[i, j] = data;
                }
            }
            target2d.Dispose();

            this.renderEnv.Camera.WorldRect = oldRect;
            this.renderEnv.Camera.UseWorldRect = false;

            GraphicsDevice.SetRenderTargets(oldTarget);
            prepareCapture = false;

            captureTask = Task.Factory.StartNew(() =>
                SaveTexture(picBlocks, oldRect.Width, oldRect.Height, target2d.Width, target2d.Height)
            );
        }

        private void SaveTexture(byte[,][] picBlocks, int mapWidth, int mapHeight, int blockWidth, int blockHeight)
        {
            //透明处理
            foreach (byte[] data in picBlocks)
            {
                for (int i = 0, j = data.Length; i < j; i += 4)
                {
                    data[i + 3] = 255;
                }
            }

            //组装
            byte[] mapData = new byte[mapWidth * mapHeight * 4];
            for (int j = 0; j < picBlocks.GetLength(1); j++)
            {
                for (int i = 0; i < picBlocks.GetLength(0); i++)
                {
                    byte[] data = picBlocks[i, j];

                    Rectangle blockRect = new Rectangle();
                    blockRect.X = i * blockWidth;
                    blockRect.Y = j * blockHeight;
                    blockRect.Width = Math.Min(mapWidth - blockRect.X, blockWidth);
                    blockRect.Height = Math.Min(mapHeight - blockRect.Y, blockHeight);

                    int length = blockRect.Width * 4;
                    if (blockRect.X == 0 && blockRect.Width == mapWidth) //整块复制
                    {
                        int startIndex = mapWidth * 4 * blockRect.Y;
                        Buffer.BlockCopy(data, 0, mapData, startIndex, blockRect.Width * blockRect.Height * 4);
                    }
                    else //逐行扫描
                    {
                        int offset = 0;
                        for (int y = blockRect.Top, y0 = blockRect.Bottom; y < y0; y++)
                        {
                            int startIndex = (y * mapWidth + blockRect.X) * 4;
                            Buffer.BlockCopy(data, offset, mapData, startIndex, length);
                            offset += blockWidth * 4;
                        }
                    }
                }
            }

            try
            {
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(
                    mapWidth,
                    mapHeight,
                    mapWidth * 4,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                    Marshal.UnsafeAddrOfPinnedArrayElement(mapData, 0));

                bitmap.Save(DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + (this.mapData?.ID ?? 0).ToString("D9") + ".png",
                    System.Drawing.Imaging.ImageFormat.Png);

                bitmap.Dispose();
            }
            catch
            {
            }
        }
        #endregion

        protected override void UnloadContent()
        {
            base.UnloadContent();
            this.resLoader.Unload();
            this.ui.UnloadContents();
            this.mapImg = null;
            this.mapData = null;
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);
            this.batcher.Dispose();
            this.batcher = null;
            this.renderEnv.Dispose();
            this.renderEnv = null;
            this.engine = null;

            GameExt.RemoveKeyboardEvent(this);
        }

        private void SwitchResolution()
        {
            var r = (Resolution)(((int)this.resolution + 1) % 4);
            SwitchResolution(r);
        }

        private void SwitchResolution(Resolution r)
        {
            Form gameWindow = (Form)Form.FromHandle(this.Window.Handle);
            switch (r)
            {
                case Resolution.Window_800_600:
                case Resolution.Window_1024_768:
                case Resolution.Window_1366_768:
                    gameWindow.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
                    break;
                case Resolution.WindowFullScreen:
                    gameWindow.SetDesktopLocation(0, 0);
                    gameWindow.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    break;
                default:
                    r = Resolution.Window_800_600;
                    goto case Resolution.Window_800_600;
            }

            this.resolution = r;
            this.renderEnv.Camera.DisplayMode = (int)r;
            this.ui.Width = this.renderEnv.Camera.Width;
            this.ui.Height = this.renderEnv.Camera.Height;
            engine.Renderer.ResetNativeSize();
        }

        enum Resolution
        {
            Window_800_600 = 0,
            Window_1024_768 = 1,
            Window_1366_768 = 2,
            WindowFullScreen = 3,
        }

        struct ItemRect
        {
            public SceneItem item;
            public Rectangle rect;
        }
    }
}

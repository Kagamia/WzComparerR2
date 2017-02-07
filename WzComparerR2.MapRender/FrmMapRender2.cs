using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WzComparerR2.Rendering;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.Animation;
using System.Runtime.InteropServices;

namespace WzComparerR2.MapRender
{
    public class FrmMapRender2 : Game
    {
        public FrmMapRender2(Wz_Image img)
        {
            graphics = new GraphicsDeviceManager(this);
            this.mapImg = img;
            this.MaxElapsedTime = TimeSpan.MaxValue;
            this.IsFixedTimeStep = false;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60);
            this.InactiveSleepTime = TimeSpan.FromSeconds(1.0 / 30);
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            this.IsMouseVisible = true;

            GameExt.FixKeyboard(this);
        }

        GraphicsDeviceManager graphics;
        Wz_Image mapImg;
        XnaFont font;
        RenderEnv renderEnv;
        MapData mapData;
        ResourceLoader resLoader;
        MeshBatcher batcher;

        bool prepareCapture;
        Task captureTask;

        List<Tuple<string, Rectangle>> allItems = new List<Tuple<string, Rectangle>>();

        protected override void Initialize()
        {
            base.Initialize();
            this.renderEnv = new RenderEnv(this, this.graphics);
            this.batcher = new MeshBatcher(this.GraphicsDevice);

            this.renderEnv.Camera.WorldRect = mapData.VRect;
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            base.OnDeactivated(sender, args);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            this.resLoader = new ResourceLoader(this.Services);
            this.font = new XnaFont(this.GraphicsDevice, "宋体", 12);
            this.mapData = new MapData();
            
            if (this.mapImg != null)
            {
                this.mapData.Load(mapImg.Node, resLoader);
                this.mapData.PreloadResource(resLoader);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            this.renderEnv.Input.Update(gameTime);
            if (renderEnv.Input.IsMouseButtonPressing(MouseButton.RightButton))
            {
                var mousePos = renderEnv.Input.MousePosition;
                this.renderEnv.Camera.Center += new Vector2(mousePos.X - 640, mousePos.Y - 360) / 10;
            }

            //临时方向键移动视角
            {
                var input = this.renderEnv.Input;
                int boost = 1;
                Vector2 offs = new Vector2();

                if (input.IsKeyPressing(Keys.Left))
                    offs.X -= 5;
                if (input.IsKeyPressing(Keys.Right))
                    offs.X += 5;
                if (input.IsKeyPressing(Keys.Up))
                    offs.Y -= 5;
                if (input.IsKeyPressing(Keys.Down))
                    offs.Y += 5;

                if (input.IsCtrlPressing)
                    boost = 2;
                this.renderEnv.Camera.Center += offs * boost;
            }

            //临时截图键
            if (renderEnv.Input.IsKeyDown(Keys.Scroll) 
                && (captureTask == null || captureTask.IsCompleted)
                && !prepareCapture)
            {
                prepareCapture = true;
            }

            UpdateAllItems(mapData.Scene, gameTime.ElapsedGameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (prepareCapture)
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
                        DrawScene();
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

            this.GraphicsDevice.Clear(Color.Black);
            DrawScene();

            var sb = this.renderEnv.Sprite;
            {
                
                var mp = this.renderEnv.Input.MousePosition;
                var text = new StringBuilder();
                foreach (var kv in this.allItems)
                {
                    if (kv.Item2.Contains(mp))
                        text.AppendLine(kv.Item1);
                }
                sb.Begin();
                sb.DrawStringEx(this.font, text.ToString(), Vector2.Zero, Color.Red);
                sb.End();
            }
            base.Draw(gameTime);
        }

        private void DrawScene()
        {
            allItems.Clear();
            this.batcher.Begin(Matrix.CreateTranslation(new Vector3(-this.renderEnv.Camera.Origin, 0)));
            foreach (var kv in GetDrawableItems(this.mapData.Scene))
            {
                this.batcher.Draw(kv.Value);

                //缓存绘图区域
                {
                    Rectangle[] rects = this.batcher.Measure(kv.Value);
                    if (kv.Value.RenderObject is Frame)
                    {
                        var frame = (Frame)kv.Value.RenderObject;
                    }
                    if (rects != null && rects.Length > 0)
                    {
                        for (int i = 0; i < rects.Length; i++)
                        {
                            rects[i].X -= (int)this.renderEnv.Camera.Origin.X;
                            rects[i].Y -= (int)this.renderEnv.Camera.Origin.Y;
                            allItems.Add(new Tuple<string, Rectangle>(kv.Key.Name, rects[i]));
                        }
                    }
                }
            }
            this.batcher.End();
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
                    IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);

                    Rectangle blockRect = new Rectangle();
                    blockRect.X = i * blockWidth;
                    blockRect.Y = j * blockHeight;
                    blockRect.Width = Math.Min(mapWidth - blockRect.X, blockWidth);
                    blockRect.Height = Math.Min(mapHeight - blockRect.Y, blockHeight);

                    int length = blockRect.Width * 4;
                    if (blockRect.X == 0 && blockRect.Width == mapWidth) //整块复制
                    {
                        int startIndex = mapWidth * 4 * blockRect.Y;
                        Marshal.Copy(pData, mapData, startIndex, blockRect.Width * blockRect.Height * 4);
                    }
                    else //逐行扫描
                    {
                        for (int y = blockRect.Top, y0 = blockRect.Bottom; y < y0; y++)
                        {
                            int startIndex = (y * mapWidth + blockRect.X) * 4;
                            Marshal.Copy(pData, mapData, startIndex, length);
                            pData = new IntPtr(pData.ToInt32() + blockWidth * 4);
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

        private IEnumerable<KeyValuePair<SceneItem, MeshItem>> GetDrawableItems(SceneNode node)
        {
            var container = node as ContainerNode;
            if (container != null)  //暂时不考虑缩进z层递归合并  container下没有子节点
            {
                foreach (var kv in container.Slots.Select(item => new KeyValuePair<SceneItem, MeshItem>(item, GetMesh(item)))
                    .Where(kv => kv.Value != null)
                    .OrderBy(kv => kv.Value))
                {
                    yield return kv;
                }
            }
            else 
            {
                foreach (var mesh in node.Nodes.SelectMany(child => GetDrawableItems(child)))
                {
                    yield return mesh;
                }
            }
        }

        private void UpdateAllItems(SceneNode node, TimeSpan elapsed)
        {
            var container = node as ContainerNode;
            if (container != null)  //暂时不考虑缩进z层递归合并  container下没有子节点
            {
                foreach (var item in container.Slots)
                {
                    if (item is BackItem)
                    {
                        var back = (BackItem)item;
                        (back.View.Animator as WzComparerR2.Controls.AnimationItem)?.Update(elapsed);
                        back.View.Time += (int)elapsed.TotalMilliseconds;
                    }
                    else if (item is ObjItem)
                    {
                        var _item = (ObjItem)item;
                        (_item.View.Animator as WzComparerR2.Controls.AnimationItem)?.Update(elapsed);
                        _item.View.Time += (int)elapsed.TotalMilliseconds;
                    }
                    else if (item is TileItem)
                    {
                        var tile = (TileItem)item;
                        (tile.View.Animator as WzComparerR2.Controls.AnimationItem)?.Update(elapsed);
                        tile.View.Time += (int)elapsed.TotalMilliseconds;
                    }
                    else if (item is LifeItem)
                    {
                        var life = (LifeItem)item;
                        var smAni = (life.View.Animator as StateMachineAnimator);
                        if (smAni != null)
                        {
                            if (smAni.GetCurrent() == null) //当前无动作
                            {
                                smAni.SetAnimation(smAni.Data.States[0]); //动作0
                            }
                            smAni.Update(elapsed);
                        }

                        life.View.Time += (int)elapsed.TotalMilliseconds;
                    }
                    else if (item is PortalItem)
                    {
                        var portal = (PortalItem)item;

                        //更新状态
                        var cursorPos = renderEnv.Camera.CameraToWorld(renderEnv.Input.MousePosition);
                        var sensorRect = new Rectangle(portal.X - 250, portal.Y - 150, 500, 300);
                        portal.View.IsFocusing = sensorRect.Contains(cursorPos);

                        //更新动画
                        var ani = portal.View.IsEditorMode ? portal.View.EditorAnimator : portal.View.Animator;
                        if (ani is StateMachineAnimator)
                        {
                            if (portal.View.Controller != null)
                            {
                                portal.View.Controller.Update(elapsed);
                            }
                            else
                            {
                                ((StateMachineAnimator)ani).Update(elapsed);
                            }
                        }
                        else if (ani is FrameAnimator)
                        {
                            var frameAni = (FrameAnimator)ani;
                            frameAni.Update(elapsed);
                        }
                    }
                    else if (item is ReactorItem)
                    {
                        var reactor = (ReactorItem)item;
                        var ani = reactor.View.Animator;
                        if (ani is StateMachineAnimator)
                        {
                            if (reactor.View.Controller != null)
                            {
                                reactor.View.Controller.Update(elapsed);
                            }
                            else
                            {
                                ((StateMachineAnimator)ani).Update(elapsed);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var child in node.Nodes)
                {
                    UpdateAllItems(child, elapsed);
                }
            }
        }

        private MeshItem GetMesh(SceneItem item)
        {
            if (item is BackItem)
            {
                return GetMeshBack((BackItem)item);
            }
            else if (item is ObjItem)
            {
                return GetMeshObj((ObjItem)item);
            }
            else if (item is TileItem)
            {
                return GetMeshTile((TileItem)item);
            }
            else if (item is LifeItem)
            {
                return GetMeshLife((LifeItem)item);
            }
            else if (item is PortalItem)
            {
                return GetMeshPortal((PortalItem)item);
            }
            else if (item is ReactorItem)
            {
                return GetMeshReactor((ReactorItem)item);
            }

            return null;
        }

        private MeshItem GetMeshBack(BackItem back)
        {
            //计算计算culling
            if (back.ScreenMode != 0 && back.ScreenMode != renderEnv.Camera.DisplayMode + 1)
            {
                return null;
            }

            //计算坐标
            var renderObject = (back.View.Animator as FrameAnimator)?.CurrentFrame.Rectangle.Size ?? Point.Zero;
            int cx = (back.Cx == 0 ? renderObject.X : back.Cx);
            int cy = (back.Cy == 0 ? renderObject.Y : back.Cy);

            Vector2 tileOff = new Vector2(cx, cy);
            Vector2 position = new Vector2(back.X, back.Y);

            //计算水平卷动
            if (back.TileMode.HasFlag(TileMode.ScrollHorizontial))
            {
                position.X += ((float)back.Rx * 5 * back.View.Time / 1000) % cx;// +this.Camera.Center.X * (100 - Math.Abs(this.rx)) / 100;
            }
            else //镜头移动比率偏移
            {
                position.X += renderEnv.Camera.Center.X * (100 + back.Rx) / 100;
            }

            //计算垂直卷动
            if (back.TileMode.HasFlag(TileMode.ScrollVertical))
            {
                position.Y += ((float)back.Ry * 5 * back.View.Time / 1000) % cy;// +this.Camera.Center.Y * (100 - Math.Abs(this.ry)) / 100;
            }
            else //镜头移动比率偏移
            {
                position.Y += (renderEnv.Camera.Center.Y) * (100 + back.Ry) / 100;
            }

            //y轴镜头调整
            if (back.TileMode == TileMode.None && renderEnv.Camera.WorldRect.Height > 600)
                position.Y += (renderEnv.Camera.Height - 600) / 2;

            //取整
            position.X = (float)Math.Floor(position.X);
            position.Y = (float)Math.Floor(position.Y);

            //计算tile
            Rectangle? tileRect = null;
            if (back.TileMode != TileMode.None)
            {
                var cameraRect = renderEnv.Camera.ClipRect;

                int l, t, r, b;
                if (back.TileMode.HasFlag(TileMode.Horizontal) && cx > 0)
                {
                    l = (int)Math.Floor((cameraRect.Left - position.X) / cx) - 1;
                    r = (int)Math.Ceiling((cameraRect.Right - position.X) / cx) + 1;
                }
                else
                {
                    l = 0;
                    r = 1;
                }

                if (back.TileMode.HasFlag(TileMode.Vertical) && cy > 0)
                {
                    t = (int)Math.Floor((cameraRect.Top - position.Y) / cy) - 1;
                    b = (int)Math.Ceiling((cameraRect.Bottom - position.Y) / cy) + 1;
                }
                else
                {
                    t = 0;
                    b = 1;
                }

                tileRect = new Rectangle(l, t, r - l, b - t);
            }

            //生成mesh
            var renderObj = GetRenderObject(back.View.Animator, back.Flip, back.Alpha);
            return renderObj == null ? null : new MeshItem()
            {
                RenderObject = renderObj,
                Position = position,
                Z0 = 0,
                Z1 = back.Index,
                FlipX = back.Flip,
                TileRegion = tileRect,
                TileOffset = tileOff,
            };
        }

        private MeshItem GetMeshObj(ObjItem obj)
        {
            var renderObj = GetRenderObject(obj.View.Animator, obj.Flip);
            return renderObj == null ? null : new MeshItem()
            {
                RenderObject = renderObj,
                Position = new Vector2(obj.X, obj.Y),
                FlipX = obj.Flip,
                Z0 = obj.Z,
                Z1 = obj.Index,
            };
        }

        private MeshItem GetMeshTile(TileItem tile)
        {
            var renderObj = GetRenderObject(tile.View.Animator);
            return renderObj == null ? null : new MeshItem()
            {
                RenderObject = renderObj,
                Position = new Vector2(tile.X, tile.Y),
                Z0 = ((renderObj as Frame)?.Z ?? 0),
                Z1 = tile.Index,
            };
        }

        private MeshItem GetMeshLife(LifeItem life)
        {
            var renderObj = GetRenderObject(life.View.Animator);
            return renderObj == null ? null : new MeshItem()
            {
                RenderObject = renderObj,
                Position = new Vector2(life.X, life.Cy),
                FlipX = life.Flip,
                Z0 = ((renderObj as Frame)?.Z ?? 0),
                Z1 = life.Index,
            };
        }

        private MeshItem GetMeshPortal(PortalItem portal)
        {
            var renderObj = GetRenderObject(portal.View.IsEditorMode ? portal.View.EditorAnimator : portal.View.Animator);
            return renderObj == null ? null : new MeshItem()
            {
                RenderObject = renderObj,
                Position = new Vector2(portal.X, portal.Y),
                Z0 = ((renderObj as Frame)?.Z ?? 0),
                Z1 = portal.Index,
            };
        }

        private MeshItem GetMeshReactor(ReactorItem reactor)
        {
            var renderObj = GetRenderObject(reactor.View.Animator);
            return renderObj == null ? null : new MeshItem()
            {
                RenderObject = renderObj,
                Position = new Vector2(reactor.X, reactor.Y),
                FlipX = reactor.Flip,
                Z0 = ((renderObj as Frame)?.Z ?? 0),
                Z1 = reactor.Index,
            };
        }

        private object GetRenderObject(object animator, bool flip = false, int alpha = 255)
        {
            if (animator is FrameAnimator)
            {
                var frame = ((FrameAnimator)animator).CurrentFrame;
                if (frame != null)
                {
                    if (alpha < 255) //理论上应该返回一个新的实例
                    {
                        frame.A0 = frame.A0 * alpha / 255;
                    }
                    return frame;
                }
            }
            else if (animator is SpineAnimator)
            {
                var skeleton = ((SpineAnimator)animator).Skeleton;
                if (skeleton != null)
                {
                    if (alpha < 255)
                    {
                        skeleton.A = alpha / 255.0f;
                    }
                    return skeleton;
                }
            }
            else if (animator is StateMachineAnimator)
            {
                var smAni = (StateMachineAnimator)animator;
                return smAni.Data.GetMesh();
            }

            //各种意外
            return null;
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
            this.resLoader.Unload();
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
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using Microsoft.Xna.Framework;
using WzComparerR2.Rendering;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.Animation;


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
        }

        GraphicsDeviceManager graphics;
        Wz_Image mapImg;
        XnaFont font;
        RenderEnv renderEnv;
        MapData mapData;
        ResourceLoader resLoader;
        MeshBatcher batcher;

        protected override void Initialize()
        {
            base.Initialize();
            this.renderEnv = new RenderEnv(this, this.graphics);

            this.batcher = new MeshBatcher(this.GraphicsDevice);
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
            this.font = new XnaFont(this.GraphicsDevice, "微软雅黑", 24);
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

            UpdateAllItems(mapData.Scene, gameTime.ElapsedGameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            this.GraphicsDevice.Clear(Color.Black);
            this.batcher.Begin(Matrix.CreateTranslation(new Vector3(-this.renderEnv.Camera.Origin, 0)));
            foreach (var mesh in GetDrawableItems(this.mapData.Scene))
            {
                this.batcher.Draw(mesh);
            }
            this.batcher.End();
            base.Draw(gameTime);
        }

        private IEnumerable<MeshItem> GetDrawableItems(SceneNode node)
        {
            var container = node as ContainerNode;
            if (container != null)  //暂时不考虑缩进z层递归合并  container下没有子节点
            {
                foreach (var mesh in container.Slots.Select(item => GetMesh(item))
                    .Where(mesh => mesh != null)
                    .OrderBy(mesh => mesh))
                {
                    yield return mesh;
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
                        var tile = ((TileItem)item);
                        (tile.View.Animator as WzComparerR2.Controls.AnimationItem)?.Update(elapsed);
                        tile.View.Time += (int)elapsed.TotalMilliseconds;
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
            position.Y += (renderEnv.Camera.Height - 600)/2;

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

        private object GetRenderObject(object animator, bool flip = false, int alpha = 255)
        {
            if (animator is FrameAnimator)
            {
                var frame = ((FrameAnimator)animator).CurrentFrame;
                if (frame != null)
                {
                    if (alpha < 255)
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

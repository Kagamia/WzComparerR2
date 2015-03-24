using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Resource = CharaSimResource.Resource;
using WzComparerR2.WzLib;

namespace WzComparerR2.MapRender.UI
{
    public class UIMiniMap
    {
        public UIMiniMap(GraphicsDevice graphicsDevice)
        {
            this.frame = new Dictionary<string, Texture2D>();
            this.frame["n"] = Utils.BitmapToTexture(graphicsDevice, Resource.UIWindow2_img_MiniMap_MaxMap_n);
            this.frame["ne"] = Utils.BitmapToTexture(graphicsDevice, Resource.UIWindow2_img_MiniMap_MaxMap_ne);
            this.frame["e"] = Utils.BitmapToTexture(graphicsDevice, Resource.UIWindow2_img_MiniMap_MaxMap_e);
            this.frame["se"] = Utils.BitmapToTexture(graphicsDevice, Resource.UIWindow2_img_MiniMap_MaxMap_se);
            this.frame["s"] = Utils.BitmapToTexture(graphicsDevice, Resource.UIWindow2_img_MiniMap_MaxMap_s);
            this.frame["sw"] = Utils.BitmapToTexture(graphicsDevice, Resource.UIWindow2_img_MiniMap_MaxMap_sw);
            this.frame["w"] = Utils.BitmapToTexture(graphicsDevice, Resource.UIWindow2_img_MiniMap_MaxMap_w);
            this.frame["nw"] = Utils.BitmapToTexture(graphicsDevice, Resource.UIWindow2_img_MiniMap_MaxMap_nw);
            this.frame["c"] = Utils.BitmapToTexture(graphicsDevice, Resource.UIWindow2_img_MiniMap_MaxMap_c);
            this.frame["nw2"] = Utils.BitmapToTexture(graphicsDevice, Resource.UIWindow2_img_MiniMap_MaxMap_nw2);

            this.resource = new NineFormResource();

            this.MapMarkVisible = true;
            this.Size = new Vector2(300, 300);
            this.mapMarkOrigin = new Vector2(7, 17);
            this.miniMapOrigin = new Vector2(this.frame["w"].Width, this.frame["n"].Height);
            this.minSize = new Vector2(this.frame["nw"].Width + this.frame["ne"].Width, this.frame["nw"].Height + this.frame["sw"].Height);
            this.streetNameOrigin = new Vector2(48, 20);
            this.mapNameOrigin = new Vector2(48, 34);

            this.Portals = new List<Vector2>();
            this.Transports = new List<Vector2>();
        }

        private Dictionary<string, Texture2D> frame;
        private NineFormResource resource;
        private readonly Vector2 mapMarkOrigin;
        private readonly Vector2 miniMapOrigin;
        private readonly Vector2 minSize;
        private readonly Vector2 streetNameOrigin;
        private readonly Vector2 mapNameOrigin;

        private bool mapMarkVisible;

        public bool MapMarkVisible
        {
            get { return this.mapMarkVisible; }
            set
            {
                this.mapMarkVisible = value;
                this.UpdateResource();
            }
        }

        public Vector2 Size { get; set; }
        public Vector2 Position { get; set; }
        public bool Visible { get; set; }
        public MiniMap MiniMap { get; set; }
        public String MapName { get; set; }
        public String StreetName { get; set; }

        public List<Vector2> Portals { get; private set; }
        public List<Vector2> Transports { get; private set; }
        public bool ResourceLoaded { get; private set; }

        public XnaFont MapNameFont { get; set; }

        private Texture2D texPortal;
        private Texture2D texTransport;

        public Rectangle MinimapRectangle
        {
            get
            {
                int x = (int)this.miniMapOrigin.X,
                    y = (int)this.miniMapOrigin.Y;
                int w = (int)this.Size.X - this.resource.W.Width - this.resource.E.Width,
                    h = (int)this.Size.Y - this.resource.N.Height - this.resource.S.Height;
                return new Rectangle(x, y, w, h);
            }
        }
        private void UpdateResource()
        {
            this.resource.N = this.frame["n"];
            this.resource.NE = this.frame["ne"];
            this.resource.E = this.frame["e"];
            this.resource.SE = this.frame["se"];
            this.resource.S = this.frame["s"];
            this.resource.SW = this.frame["sw"];
            this.resource.W = this.frame["w"];
            //this.resource.C = this.frame["c"];
            this.resource.C = null;
            if (this.mapMarkVisible)
            {
                this.resource.NW = this.frame["nw"];
            }
            else
            {
                this.resource.NW = this.frame["nw2"];
            }
        }

        public void Draw(RenderEnv env, GameTime gameTime)
        {
            //计算UI偏移
            Matrix trans = Matrix.CreateTranslation(this.Position.X, this.Position.Y, 0);

            //绘制外框
            env.GraphicsDevice.ScissorRectangle = new Rectangle((int)this.Position.X, (int)this.Position.Y, (int)this.Size.X, (int)this.Size.Y);
            env.GraphicsDevice.RenderState.ScissorTestEnable = true;

            env.Sprite.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, trans);
            env.Sprite.FillRectangle(this.MinimapRectangle, new Color(Color.Black, 0.7f));
            UIGraphics.DrawNineForm(env, this.resource, Vector2.Zero, this.Size);
            env.Sprite.End();

            env.GraphicsDevice.RenderState.ScissorTestEnable = false;

            //绘制标题
            if (this.MapNameFont != null)
            {
                env.Sprite.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Texture, SaveStateMode.None, trans);
                if (this.StreetName != null)
                {
                    env.Sprite.DrawStringEx(this.MapNameFont, this.StreetName, this.streetNameOrigin, Color.White);
                }
                if (this.MapName != null)
                {
                    env.Sprite.DrawStringEx(this.MapNameFont, this.MapName, this.mapNameOrigin, Color.White);
                }
                env.Sprite.End();
            }

            //绘制小地图
            if (this.MiniMap != null)
            {
                //绘制小地图标记
                env.Sprite.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, trans);
                if (MapMarkVisible && this.MiniMap.MapMark != null)
                {
                    env.Sprite.Draw(this.MiniMap.MapMark, mapMarkOrigin, Color.White);
                }
                env.Sprite.End();

                if (this.MiniMap.Canvas != null)
                {
                    //计算世界地图到小地图的偏移
                    Texture2D canvas = this.MiniMap.Canvas;
                    Rectangle fromRect;
                    if (this.MiniMap.Width > 0 && this.MiniMap.Height > 0)
                    {
                        fromRect = new Rectangle(-this.MiniMap.CenterX, -this.MiniMap.CenterY, this.MiniMap.Width, this.MiniMap.Height);
                    }
                    else
                    {
                        fromRect = env.Camera.WorldRect;
                    }
                    Rectangle toRect = new Rectangle(0, 0, canvas.Width, canvas.Height);
                    Matrix worldToMinimap = Matrix.CreateTranslation(-fromRect.X, -fromRect.Y, 0)
                            * Matrix.CreateScale(1f / fromRect.Width * toRect.Width, 1f / fromRect.Height * toRect.Height, 0)
                            * Matrix.CreateTranslation(toRect.X, toRect.Y, 0);

                    //计算小地图区域的二次偏移
                    Rectangle rect = this.MinimapRectangle;
                    Vector2 offset = new Vector2((rect.Width - canvas.Width) / 2, (rect.Height - canvas.Height) / 2);
                    worldToMinimap *= Matrix.CreateTranslation(offset.X, offset.Y, 0);

                    //设置剪裁区域
                    env.GraphicsDevice.ScissorRectangle = new Rectangle(
                      (int)this.Position.X + rect.X, (int)this.Position.Y + rect.Y, rect.Width, rect.Height);
                    env.GraphicsDevice.RenderState.ScissorTestEnable = true;

                    //绘制小地图本体
                    env.Sprite.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, trans);
                    env.Sprite.Draw(this.MiniMap.Canvas, miniMapOrigin + offset, Color.White);

                    if (this.ResourceLoaded)
                    {
                        Vector2 iconOrigin = new Vector2(0, 5);
                        //绘制一般传送门
                        if (this.Portals.Count > 0 && this.texPortal != null)
                        {
                            Vector2 origin = new Vector2(this.texPortal.Width / 2, this.texPortal.Height / 2);
                            foreach (var portal in this.Portals)
                            {
                                Vector2 position = Vector2.Transform(portal, worldToMinimap);
                                position = miniMapOrigin + position - origin - iconOrigin;
                                position = MathHelper2.Round(position);
                                env.Sprite.Draw(this.texPortal, position, Color.White);
                            }
                        }

                        //绘制地图内传送门
                        if (this.Transports.Count > 0 && this.texTransport != null)
                        {
                            Vector2 origin = new Vector2(this.texTransport.Width / 2, this.texTransport.Height / 2);
                            foreach (var portal in this.Transports)
                            {
                                Vector2 position = Vector2.Transform(portal, worldToMinimap);
                                position = miniMapOrigin + position - origin - iconOrigin;
                                position = MathHelper2.Round(position);
                                env.Sprite.Draw(this.texTransport, position, Color.White);
                            }
                        }
                    }

                    //绘制摄像机区域框
                    Rectangle cameraRect = MathHelper2.Transform(env.Camera.ClipRect, worldToMinimap);
                    cameraRect.X += (int)miniMapOrigin.X;
                    cameraRect.Y += (int)miniMapOrigin.Y;
                    env.Sprite.DrawRectangle(cameraRect, Color.Yellow);
                    env.Sprite.End();
                    env.GraphicsDevice.RenderState.ScissorTestEnable = false;
                }
            }

        }

        public void UpdateSize()
        {
            Vector2 minimapSize;
            if (this.MiniMap == null || this.MiniMap.Canvas == null)
            {
                minimapSize = Vector2.Zero;
            }
            else
            {
                Texture2D tex = this.MiniMap.Canvas;
                minimapSize = new Vector2(tex.Width, tex.Height);
            }

            //计算小地图size
            int top = this.resource.N.Height,
                bottom = this.resource.S.Height,
                left = this.resource.W.Width,
                right = this.resource.E.Width;

            minimapSize = new Vector2(left + right + minimapSize.X,
               top + bottom + minimapSize.Y);

            //计算地图名称size
            float mapNameRight = this.mapNameOrigin.X;
            if (this.MapNameFont != null)
            {
                if (this.MapName != null)
                {
                    mapNameRight = Math.Max(mapNameRight,
                        this.mapNameOrigin.X + this.MapNameFont.MeasureString(this.MapName).X);
                }
                if (this.StreetName != null)
                {
                    mapNameRight = Math.Max(mapNameRight,
                        this.mapNameOrigin.X + this.MapNameFont.MeasureString(this.StreetName).X);
                }
            }
            mapNameRight += this.resource.E.Width;

            this.Size = new Vector2(MathHelper2.Max(this.minSize.X, minimapSize.X, mapNameRight),
                MathHelper2.Max(this.minSize.Y, minimapSize.Y));
        }

        public void LoadResource(GraphicsDevice graphicsDevice, Wz_Node mapWz)
        {
            Wz_Node minimapNode = mapWz.FindNodeByPath(true, "MapHelper.img", "minimap");
            if (minimapNode != null)
            {
                Wz_Node portalNode = minimapNode.FindNodeByPath("portal");
                Wz_Node transportNode = minimapNode.FindNodeByPath("transport");

                Wz_Png png;
                if ((png = portalNode.GetValueEx<Wz_Png>(null)) != null)
                {
                    this.texPortal = TextureLoader.PngToTexture(graphicsDevice, png);
                }

                if ((png = transportNode.GetValueEx<Wz_Png>(null)) != null)
                {
                    this.texTransport = TextureLoader.PngToTexture(graphicsDevice, png);
                }
            }
            this.ResourceLoaded = true;
        }
    }
}

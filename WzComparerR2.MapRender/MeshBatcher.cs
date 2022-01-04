using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.Animation;
using WzComparerR2.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Spine;

namespace WzComparerR2.MapRender
{
    sealed class MeshBatcher : IDisposable
    {
        public MeshBatcher(GraphicsDevice graphicsDevice)
        {
            this.GraphicsDevice = graphicsDevice;
            this.alphaBlendState = StateEx.NonPremultipled_Hidef();
            this.meshPool = new Stack<MeshItem>();
        }

        public GraphicsDevice GraphicsDevice { get; private set; }
        public bool D2DEnabled { get; set; }
        public bool CullingEnabled { get; set; }

        //内部batcher
        SpriteBatchEx sprite;
        SkeletonMeshRenderer spineRender;
        D2DRenderer d2dRender;
        BlendState alphaBlendState;
        ItemType lastItem;
        Stack<MeshItem> meshPool;

        //start参数
        private Matrix? matrix;
        private bool isInBeginEndPair;

        //culling参数
        private bool matrixNoRot;
        private Rectangle viewport;


        public void Begin(Matrix? matrix = null)
        {
            this.matrix = matrix;
            this.lastItem = ItemType.Unknown;
            this.isInBeginEndPair = true;
            this.PrepareCullingParameters();
        }

        private void PrepareCullingParameters()
        {
            if (this.matrix == null)
            {
                matrixNoRot = true;
            }
            else
            {
                var mt = this.matrix.Value;
                if (mt.M12 == 0 && mt.M21 == 0)
                {
                    matrixNoRot = true;
                }
                else
                {
                    matrixNoRot = false;
                }
            }

            //重新计算viewport
            this.viewport = this.GraphicsDevice.Viewport.Bounds;
            if (matrixNoRot)
            {
                var invmt = Matrix.Invert(this.matrix.Value);
                var lt = Vector2.Transform(this.viewport.Location.ToVector2(), invmt);
                var rb = Vector2.Transform(new Vector2(this.viewport.Right, this.viewport.Bottom), invmt);
                int l = (int)Math.Floor(lt.X);
                int t = (int)Math.Floor(lt.Y);
                int r = (int)Math.Ceiling(rb.X);
                int b = (int)Math.Ceiling(rb.Y);
                this.viewport = new Rectangle(l, t, r - l, b - t);
            }
        }

        public void Draw(MeshItem mesh)
        {
            if (mesh.RenderObject is Frame)
            {
                this.DrawItem(mesh, (Frame)mesh.RenderObject);
            }
            else if (mesh.RenderObject is Skeleton)
            {
                this.DrawItem(mesh, (Skeleton)mesh.RenderObject);
            }
            else if (mesh.RenderObject is TextMesh)
            {
                var textmesh = (TextMesh)mesh.RenderObject;
                this.DrawItem(mesh, textmesh);
            }
            else if (mesh.RenderObject is LineListMesh)
            {
                this.DrawItem((LineListMesh)mesh.RenderObject);
            }
            else if (mesh.RenderObject is ParticleSystem)
            {
                this.DrawItem(mesh, (ParticleSystem)mesh.RenderObject);
            }
        }

        private void DrawItem(MeshItem mesh, Frame frame)
        {
            if (frame == null || frame.Texture == null)
            {
                return;
            }

            var origin = mesh.FlipX ? new Vector2(frame.Rectangle.Width - frame.Origin.X, frame.Origin.Y) : frame.Origin.ToVector2();
            var eff = mesh.FlipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            var rect = frame.Rectangle;
            if (mesh.FlipX)
            {
                rect.X = -rect.Right;
            }

            //兼容平铺
            if (mesh.TileRegion != null)
            {
                var region = mesh.TileRegion.Value;
                for (int y = region.Top; y < region.Bottom; y++)
                {
                    for (int x = region.Left; x < region.Right; x++)
                    {
                        Vector2 pos = mesh.Position + mesh.TileOffset * new Vector2(x, y);
                        if (this.CullingEnabled)
                        {
                            var tileRect = rect;
                            tileRect.Offset(pos);
                            if (!this.IntersectsVP(tileRect))
                                continue;
                        }

                        Prepare(frame.Blend ? ItemType.Sprite_BlendAdditive : ItemType.Sprite);
                        sprite.Draw(frame.Texture, pos,
                            frame.AtlasRect,
                            new Color(Color.White, frame.A0),
                            0,
                            origin,
                            1,
                            eff,
                            0
                            );
                    }
                }
            }
            else
            {
                rect.Offset(mesh.Position);
                if (!this.CullingEnabled || this.IntersectsVP(rect))
                {
                    Prepare(frame.Blend ? ItemType.Sprite_BlendAdditive : ItemType.Sprite);
                    sprite.Draw(frame.Texture, mesh.Position,
                        frame.AtlasRect,
                        new Color(Color.White, frame.A0),
                        0,
                        origin,
                        1,
                        eff,
                        0
                        );
                }
            }
        }

        private void DrawItem(MeshItem mesh, Skeleton skeleton)
        {
            skeleton.FlipX = mesh.FlipX;

            //兼容平铺
            if (mesh.TileRegion != null)
            {
                var region = mesh.TileRegion.Value;
                for (int y = region.Top; y < region.Bottom; y++)
                {
                    for (int x = region.Left; x < region.Right; x++)
                    {
                        Vector2 pos = mesh.Position + mesh.TileOffset * new Vector2(x, y);
                        skeleton.X = pos.X;
                        skeleton.Y = pos.Y;
                        skeleton.UpdateWorldTransform();
                        Prepare(ItemType.Skeleton);
                        this.spineRender.Draw(skeleton);
                    }
                }
            }
            else
            {
                skeleton.X = mesh.Position.X;
                skeleton.Y = mesh.Position.Y;
                skeleton.UpdateWorldTransform();
                Prepare(ItemType.Skeleton);
                this.spineRender.Draw(skeleton);
            }
        }

        private void DrawItem(MeshItem mesh, TextMesh text)
        {
            if (text.Text != null && text.Font != null)
            {
                var size = text.Font.MeasureString(text.Text);
                var pos = mesh.Position;

                switch (text.Align)
                {
                    case Alignment.Near: break;
                    case Alignment.Center: pos.X -= (int)(size.X / 2); break;
                    case Alignment.Far: pos.X -= size.X; break;
                }

                var padding = text.Padding;
                var rect = new Rectangle((int)(pos.X - padding.Left),
                    (int)(pos.Y - padding.Top),
                    (int)(size.X + padding.Left + padding.Right),
                    (int)(size.Y + padding.Top + padding.Bottom)
                    );

                if (this.CullingEnabled)
                {
                    if (!this.IntersectsVP(rect))
                    {
                        return;
                    }
                }

                object baseFont = text.Font.BaseFont ?? text.Font;

                if (baseFont is XnaFont)
                {
                    Prepare(ItemType.Sprite);
                }
                else if (baseFont is D2DFont)
                {
                    Prepare(ItemType.D2DObject);
                }
                else
                {
                    return;
                }

                if (text.BackColor.A > 0) //绘制背景
                {
                    switch (this.lastItem)
                    {
                        case ItemType.Sprite: sprite.FillRoundedRectangle(rect, text.BackColor); break;
                        case ItemType.D2DObject: d2dRender.FillRoundedRectangle(rect, 3, text.BackColor); break;
                    }
                }

                if (text.ForeColor.A > 0) //绘制文字
                {
                    switch (this.lastItem)
                    {
                        case ItemType.Sprite: sprite.DrawStringEx((XnaFont)baseFont, text.Text, pos, text.ForeColor); break;
                        case ItemType.D2DObject: d2dRender.DrawString((D2DFont)baseFont, text.Text, pos, text.ForeColor); break;
                    }
                }
            }
        }

        private void DrawItem(LineListMesh lineList)
        {
            if (lineList != null && lineList.Lines != null)
            {
                var vertices = lineList.Lines;
                int vertexCount = vertices.Length / 2 * 2;
                if (this.D2DEnabled)
                {
                    Prepare(ItemType.D2DObject);
                    for (int i = 0; i < vertexCount; i += 2)
                    {
                        this.d2dRender.DrawLine(vertices[i].ToVector2(), vertices[i + 1].ToVector2(),
                            lineList.Thickness, lineList.Color);
                    }
                }
                else
                {
                    Prepare(ItemType.Sprite);
                    for (int i = 0; i < vertexCount; i += 2)
                    {
                        sprite.DrawLine(vertices[i], vertices[i + 1], lineList.Thickness, lineList.Color);
                    }
                }
            }
        }

        private void DrawItem(MeshItem mesh, ParticleSystem particleSystem)
        {
            if (particleSystem.Texture?.Texture != null)
            {
                ItemType itemType = ItemType.Unknown;
                if (particleSystem.BlendFuncSrc == ParticleBlendFunc.SRC_ALPHA
                    || (int)particleSystem.BlendFuncSrc == 12) //TODO: what's this? KMS v.293, esfera_temple_big
                {
                    switch (particleSystem.BlendFuncDst)
                    {
                        case ParticleBlendFunc.ONE: //5,2
                        case ParticleBlendFunc.DEST_ALPHA: //5,7
                            itemType = ItemType.Sprite_BlendAdditive;
                            break;
                        case ParticleBlendFunc.SRC_COLOR: //12,3
                        case ParticleBlendFunc.INV_SRC_ALPHA: //5,6
                            itemType = ItemType.Sprite_BlendNonPremultiplied;
                            break;
                    }
                }
                if (itemType == ItemType.Unknown)
                {
                    throw new Exception($"Unknown particle blendfunc: {particleSystem.BlendFuncSrc}, {particleSystem.BlendFuncDst}");
                }
                Prepare(itemType);
                particleSystem.Draw(this.sprite, mesh.Position);
            }
        }

        public Rectangle[] Measure(MeshItem mesh)
        {
            Rectangle[] region = null;
            int count;
            Measure(mesh, ref region, out count);
            return region;
        }

        public void Measure(MeshItem mesh, ref Rectangle[] region, out int count)
        {
            Rectangle rect = Rectangle.Empty;

            if (mesh.RenderObject is TextMesh)
            {
                var textItem = (TextMesh)mesh.RenderObject;
                var size = textItem.Font.MeasureString(textItem.Text);
                var pos = mesh.Position;

                switch (textItem.Align)
                {
                    case Alignment.Near: break;
                    case Alignment.Center: pos.X -= size.X / 2; break;
                    case Alignment.Far: pos.X -= size.X; break;
                }

                var padding = textItem.Padding;
                var rectBg = new Rectangle((int)(pos.X - padding.Left),
                    (int)(pos.Y - padding.Top),
                    (int)(size.X + padding.Left + padding.Right),
                    (int)(size.Y + padding.Top + padding.Bottom)
                    );
                var rectText = new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);

                count = 1;
                EnsureArraySize(ref region, count);
                Rectangle.Union(ref rectBg, ref rectText, out region[0]);
                return;
            }

            if (mesh.RenderObject is Frame)
            {
                var frame = (Frame)mesh.RenderObject;
                rect = frame.Rectangle;
                if (mesh.FlipX)
                {
                    rect.X = -rect.Right;
                }
            }
            else
            {
                count = 0;
                return;
            }

            rect.X += (int)mesh.Position.X;
            rect.Y += (int)mesh.Position.Y;

            if (mesh.TileRegion != null)
            {
                var tileRegion = mesh.TileRegion.Value;
                count = tileRegion.Width * tileRegion.Height;
                EnsureArraySize(ref region, count);
                Point offset = mesh.TileOffset.ToPoint();
                int i = 0;

                for (int y = tileRegion.Top; y < tileRegion.Bottom; y++)
                {
                    for (int x = tileRegion.Left; x < tileRegion.Right; x++)
                    {
                        region[i++] = new Rectangle(rect.X + x * offset.X,
                            rect.Y + y * offset.Y,
                            rect.Width,
                            rect.Height);
                    }
                }
            }
            else
            {
                count = 1;
                EnsureArraySize(ref region, count);
                region[0] = rect;
            }
        }

        private void EnsureArraySize<T>(ref T[] array, int length)
        {
            if (array == null)
            {
                array = new T[length];
            }
            else if (array.Length < length)
            {
                Array.Resize(ref array, length);
            }
        }

        public void End()
        {
            InnerFlush();
            this.lastItem = ItemType.Unknown;
            this.isInBeginEndPair = false;
        }

        private void Prepare(ItemType itemType)
        {
            if (lastItem == itemType)
            {
                return;
            }

            switch (itemType)
            {
                case ItemType.Sprite:
                case ItemType.Skeleton:
                case ItemType.D2DObject:
                case ItemType.Sprite_BlendAdditive:
                case ItemType.Sprite_BlendNonPremultiplied:
                    InnerFlush();
                    lastItem = itemType;
                    InnerBegin();
                    break;
            }
        }

        private void InnerBegin()
        {
            switch (lastItem)
            {
                case ItemType.Sprite:
                    if (this.sprite == null)
                    {
                        this.sprite = new SpriteBatchEx(this.GraphicsDevice);
                    }
                    this.sprite.Begin(SpriteSortMode.Deferred, this.alphaBlendState, transformMatrix: this.matrix);
                    break;

                case ItemType.Skeleton:
                    if (this.spineRender == null)
                    {
                        this.spineRender = new SkeletonMeshRenderer(this.GraphicsDevice);
                    }
                    this.spineRender.Effect.World = matrix ?? Matrix.Identity;
                    this.spineRender.Begin();
                    break;

                case ItemType.D2DObject:
                    if (this.d2dRender == null)
                    {
                        this.d2dRender = new D2DRenderer(this.GraphicsDevice);
                    }
                    if (this.matrix == null)
                    {
                        this.d2dRender.Begin();
                    }
                    else
                    {
                        this.d2dRender.Begin(this.matrix.Value);
                    }
                    break;

                case ItemType.Sprite_BlendAdditive:
                    if (this.sprite == null)
                    {
                        this.sprite = new SpriteBatchEx(this.GraphicsDevice);
                    }
                    this.sprite.Begin(SpriteSortMode.Deferred, BlendState.Additive, transformMatrix: this.matrix);
                    break;

                case ItemType.Sprite_BlendNonPremultiplied:
                    if (this.sprite == null)
                    {
                        this.sprite = new SpriteBatchEx(this.GraphicsDevice);
                    }
                    this.sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, transformMatrix: this.matrix);
                    break;
            }
        }

        private void InnerFlush()
        {
            switch (lastItem)
            {
                case ItemType.Sprite:
                case ItemType.Sprite_BlendAdditive:
                case ItemType.Sprite_BlendNonPremultiplied:
                    this.sprite.End();
                    break;

                case ItemType.Skeleton:
                    this.spineRender.End();
                    break;

                case ItemType.D2DObject:
                    this.d2dRender.End();
                    break;
            }
        }

        private bool IntersectsVP(Rectangle rect)
        {
            if (this.matrixNoRot)
            {
                bool isIntersects;
                this.viewport.Intersects(ref rect, out isIntersects);
                return isIntersects;
            }
            else
            {
                var mt = this.matrix.Value;
                Vector2 lt = new Vector2(rect.Left, rect.Top),
                    rt = new Vector2(rect.Right, rect.Top),
                    lb = new Vector2(rect.Left, rect.Bottom),
                    rb = new Vector2(rect.Right, rect.Bottom);
                Vector2.Transform(ref lt, ref mt, out lt);
                Vector2.Transform(ref rt, ref mt, out rt);
                Vector2.Transform(ref lb, ref mt, out lb);
                Vector2.Transform(ref rb, ref mt, out rb);

                int left = (int)Math.Floor(MathHelper2.Min(lt.X, rt.X, lb.X, rb.X));
                int top = (int)Math.Floor(MathHelper2.Min(lt.Y, rt.Y, lb.Y, rb.Y));
                int right = (int)Math.Ceiling(MathHelper2.Max(lt.X, rt.X, lb.X, rb.X));
                int bottom = (int)Math.Ceiling(MathHelper2.Max(lt.Y, rt.Y, lb.Y, rb.Y));
                var bound = new Rectangle(left, top, right - left, bottom - top);
                bool isIntersects;
                this.viewport.Intersects(ref bound, out isIntersects);
                return isIntersects;
            }
        }

        public MeshItem MeshPop()
        {
            if (this.meshPool.Count > 0)
            {
                var mesh = this.meshPool.Pop();
                mesh.RenderObject = null;
                mesh.Position = Vector2.Zero;
                mesh.Z0 = 0;
                mesh.Z1 = 0;
                mesh.FlipX = false;
                mesh.TileRegion = null;
                mesh.TileOffset = Vector2.Zero;
                return mesh;
            }
            else
            {
                return new MeshItem();
            }
        }

        public void MeshPush(MeshItem mesh)
        {
            mesh.RenderObject = null;
            this.meshPool.Push(mesh);
        }

        public void Dispose()
        {
            this.sprite?.Dispose();
            this.spineRender?.Effect.Dispose();
            this.alphaBlendState.Dispose();
            this.meshPool.Clear();
        }

        private enum ItemType
        {
            Unknown = 0,
            Sprite = 1,
            Skeleton = 2,
            D2DObject = 3,
            Sprite_AlphaBlend = Sprite,
            Sprite_BlendAdditive = 4,
            Sprite_BlendNonPremultiplied = 5,
        }

    }
}

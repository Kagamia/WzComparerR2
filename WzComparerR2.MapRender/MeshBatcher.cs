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
            this.sprite = new SpriteBatch(graphicsDevice);
            this.spineRender = new SkeletonMeshRenderer(graphicsDevice);
            this.alphaBlendState = StateEx.NonPremultipled_Hidef();
        }

        public GraphicsDevice GraphicsDevice { get; private set; }

        //内部batcher
        SpriteBatch sprite;
        SkeletonMeshRenderer spineRender;
        BlendState alphaBlendState;
        ItemType lastItem;

        //start参数
        private Matrix? matrix;
        private bool isInBeginEndPair;

        public void Begin(Matrix? matrix = null)
        {
            this.matrix = matrix;
            this.lastItem = ItemType.Unknown;
            this.isInBeginEndPair = true;
        }

        public void Draw(MeshItem mesh)
        {
            if (mesh.RenderObject is Frame)
            {
                var frame = (Frame)mesh.RenderObject;

                var origin = mesh.FlipX ? new Vector2(frame.Rectangle.Width - frame.Origin.X, frame.Origin.Y) : frame.Origin.ToVector2();
                var eff = mesh.FlipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                if (lastItem != ItemType.Sprite)
                {
                    InnerFlush();
                    lastItem = ItemType.Sprite;
                    InnerBegin();
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
                            sprite.Draw(frame.Texture, pos,
                                sourceRectangle: frame.AtlasRect,
                                color: new Color(Color.White, frame.A0),
                                origin: origin,
                                effects: eff
                                );
                        }
                    }
                }
                else
                {
                    sprite.Draw(frame.Texture, mesh.Position,
                        sourceRectangle: frame.AtlasRect,
                        color: new Color(Color.White, frame.A0),
                        origin: origin,
                        effects: eff
                        );
                }
            }
            else if (mesh.RenderObject is Skeleton)
            {
                var skeleton = (Skeleton)mesh.RenderObject;
                skeleton.FlipX = mesh.FlipX;

                if (lastItem != ItemType.Skeleton)
                {
                    InnerFlush();
                    lastItem = ItemType.Skeleton;
                    InnerBegin();
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
                            skeleton.X = pos.X;
                            skeleton.Y = pos.Y;
                            skeleton.UpdateWorldTransform();
                            this.spineRender.Draw(skeleton);
                        }
                    }
                }
                else
                {
                    skeleton.X = mesh.Position.X;
                    skeleton.Y = mesh.Position.Y;
                    skeleton.UpdateWorldTransform();
                    this.spineRender.Draw(skeleton);
                }
            }
        }

        public void End()
        {
            InnerFlush();
            this.lastItem = ItemType.Unknown;
            this.isInBeginEndPair = false;
        }

        private void InnerBegin()
        {
            switch (lastItem)
            {
                case ItemType.Sprite:
                    this.sprite.Begin(SpriteSortMode.Deferred, this.alphaBlendState, transformMatrix: this.matrix);
                    break;

                case ItemType.Skeleton:
                    this.spineRender.Effect.World = matrix ?? Matrix.Identity;
                    this.spineRender.Begin();
                    break;
            }
        }

        private void InnerFlush()
        {
            switch (lastItem)
            {
                case ItemType.Sprite:
                    this.sprite.End();
                    break;

                case ItemType.Skeleton:
                    this.spineRender.End();
                    break;
            }
        }

        public void Dispose()
        {
            this.sprite.Dispose();
            this.spineRender.Effect.Dispose();
            this.alphaBlendState.Dispose();
        }

        private enum ItemType
        {
            Unknown = 0,
            Sprite,
            Skeleton
        }

    }
}

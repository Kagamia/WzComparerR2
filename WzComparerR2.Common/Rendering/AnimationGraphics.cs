using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Controls;

namespace WzComparerR2.Rendering
{
    public class AnimationGraphics
    {
        public AnimationGraphics(GraphicsDevice graphicsDevice)
            : this (graphicsDevice, new SpriteBatch(graphicsDevice))
        {
        }

        public AnimationGraphics(GraphicsDevice graphicsDevice, SpriteBatch sprite)
        {
            this.GraphicsDevice = graphicsDevice;
            this.sprite = sprite;
            this.spineRenderer = new Spine.SkeletonRenderer(graphicsDevice);
            this.blendState = StateEx.NonPremultipled_Hidef();
        }

        public GraphicsDevice GraphicsDevice { get; private set; }

        private SpriteBatch sprite;
        private Spine.SkeletonRenderer spineRenderer;
        private BlendState blendState;

        public void Draw(FrameAnimator animator, Matrix world)
        {
            Frame frame = animator.CurrentFrame;
            if (frame != null && frame.Texture != null)
            {
                if (animator.Position != Point.Zero)
                {
                    world *= Matrix.CreateTranslation(animator.Position.X, animator.Position.Y, 0);
                }

                sprite.Begin(SpriteSortMode.Deferred, this.blendState, transformMatrix: world);
                sprite.Draw(frame.Texture,
                    Vector2.Zero,
                    frame.AtlasRect,
                    new Color(Color.White, frame.A0),
                    0,
                    frame.Origin.ToVector2(),
                    1,
                    SpriteEffects.None,
                    0);
                sprite.End();
            }
        }

        public void Draw(ISpineAnimator animator, Matrix world)
        {
            if (animator is AnimationItem aniItem && aniItem.Position != Point.Zero)
            {
                world *= Matrix.CreateTranslation(aniItem.Position.X, aniItem.Position.Y, 0);
            }

            spineRenderer.PremultipliedAlpha = animator.Data.PremultipliedAlpha;
            if (spineRenderer.Effect is BasicEffect basicEff)
            {
                basicEff.World = world;
                basicEff.Projection = Matrix.CreateOrthographicOffCenter(0, this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height, 0, 1, 0);
            }

            spineRenderer.Begin();
            animator.Render(spineRenderer);
            spineRenderer.End();
        }
    }
}

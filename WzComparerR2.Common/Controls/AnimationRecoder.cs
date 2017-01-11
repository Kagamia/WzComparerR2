using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Rendering;
using WzComparerR2.Animation;

namespace WzComparerR2.Controls
{
    public class AnimationRecoder
    {
        public AnimationRecoder(GraphicsDevice graphicsDevice)
        {
            this._device = graphicsDevice;
            this._graphics = new AnimationGraphics(_device);
            this.Items = new List<AnimationItem>();
            this.BackgroundColor = Color.TransparentBlack;
        }

        public List<AnimationItem> Items { get; private set; }

        public System.Drawing.Color GdipBackgroundColor
        {
            get
            {
                var c = this.BackgroundColor;
                return System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
            }
            set
            {
                this.BackgroundColor = value.ToXnaColor();
            }
        }

        public Color BackgroundColor { get; set; }

        public Texture2D BackgroundImage { get; set; }

        private GraphicsDevice _device;
        private RenderTargetBinding[] _oldBuffer;
        private RenderTarget2D _rt2d;
        private Rectangle _viewport;
        private AnimationGraphics _graphics;
        private SpriteBatch _sb;
        private PngEffect _eff;

        public void Begin(Rectangle viewport)
        {
            _rt2d = new RenderTarget2D(_device, viewport.Width, viewport.Height, false, SurfaceFormat.Bgra32, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            var binding = _device.GetRenderTargets();
            _device.SetRenderTarget(_rt2d);

            this._viewport = viewport;
            this._oldBuffer = binding;

            this._sb = new SpriteBatch(_device);
            this._eff = new PngEffect(_device);
        }

        public void Update(TimeSpan elapsed)
        {
            foreach (var aniItem in this.Items)
            {
                aniItem.Update(elapsed);
            }
        }

        public void Draw()
        {
            _device.SetRenderTarget(_rt2d);

            if (this.BackgroundImage != null)
            {
                this._device.Clear(Color.Black);
                var rect = new Rectangle(0, 0, _viewport.Width, _viewport.Height);
                _sb.Begin(blendState: BlendState.Opaque, samplerState:SamplerState.PointWrap);
                _sb.Draw(this.BackgroundImage, Vector2.Zero, rect, Color.White);
                _sb.End();
            }
            else
            {
                this._device.Clear(this.BackgroundColor);
            }
            
            Matrix world = Matrix.CreateTranslation(-this._viewport.Left, -this._viewport.Top, 0);

            foreach (var animation in this.Items)
            {
                if (animation != null)
                {
                    if (animation is FrameAnimator)
                    {
                        _graphics.Draw((FrameAnimator)animation, world);
                    }
                    else if (animation is SpineAnimator)
                    {
                        _graphics.Draw((SpineAnimator)animation, world);
                    }
                }
            }

            _device.SetRenderTargets(_oldBuffer);
        }

        public Texture2D GetPngTexture()
        {
            var texture = new RenderTarget2D(_device, _rt2d.Width, _rt2d.Height, false, SurfaceFormat.Bgra32, DepthFormat.None);
            _device.SetRenderTarget(texture);
            _eff.AlphaMixEnabled = false;

            _device.Clear(Color.TransparentBlack);
            _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, null, null, _eff, null);
            _sb.Draw(_rt2d, Vector2.Zero, Color.White);
            _sb.End();

            _device.SetRenderTargets(_oldBuffer);
            return texture;
        }

        public Texture2D GetGifTexture(Color mixColor, int minMixedAlpha)
        {
            var texture = new RenderTarget2D(_device, _rt2d.Width, _rt2d.Height, false, SurfaceFormat.Bgra32, DepthFormat.None);
            _device.SetRenderTarget(texture);

            _eff.AlphaMixEnabled = true;
            _eff.MixedColor = mixColor;
            _eff.MinMixedAlpha = minMixedAlpha;
           
            _device.Clear(Color.TransparentBlack);
            _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, null, null, _eff, null);
            _sb.Draw(_rt2d, Vector2.Zero, Color.White);
            _sb.End();

            _device.SetRenderTargets(_oldBuffer);
            return texture;
        }

        public void ResetAll()
        {
            foreach(var aniItem in this.Items)
            {
                aniItem.Reset();
            }
        }

        public int GetMaxLength()
        {
            return this.Items.Select(aniItem => Math.Max(0, aniItem.Length)).Max();
        }

        public int[] GetGifTimeLine(int minFrameDelay)
        {
            //只响应第0图层 回头合并...

            foreach (var animation in this.Items)
            {
                if (animation != null)
                {
                    if (animation is FrameAnimator)
                    {
                        return null;
                    }
                    else if (animation is SpineAnimator)
                    {
                        var m = ((SpineAnimator)animation).GetKeyFrames();
                        return null;
                    }
                }
            }
            return null;
        }

        public void End()
        {
            var binding = _device.GetRenderTargets();
            _device.SetRenderTargets(_oldBuffer);
            this._device = null;
            this._oldBuffer = null;

            for (int i = 0; i < binding.Length; i++)
            {
                binding[i].RenderTarget.Dispose();
            }

            _eff.Dispose();
            _sb.Dispose();
        }
    }
}

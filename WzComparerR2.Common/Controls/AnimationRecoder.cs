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
            this.BackgroundColor = Color.Transparent;
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
        private Point _targetSize;
        private AnimationGraphics _graphics;
        private SpriteBatch _sb;
        private PngEffect _eff;

        public void Begin(Rectangle viewport, Point? targetSize = null)
        {
            this._targetSize = targetSize ?? viewport.Size;
            _rt2d = new RenderTarget2D(_device, _targetSize.X, _targetSize.Y, false, SurfaceFormat.Bgra32, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
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
            System.Threading.Monitor.Enter(this._device);
            try
            {
                _device.SetRenderTarget(_rt2d);
                this.DrawAnimation();
            }
            finally
            {
                _device.SetRenderTargets(_oldBuffer);
                System.Threading.Monitor.Exit(this._device);
            }
        }

        private void DrawAnimation()
        {
            if (this.BackgroundImage != null)
            {
                this._device.Clear(Color.Black);
                var rect = new Rectangle(0, 0, _targetSize.X, _targetSize.Y);
                _sb.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.PointWrap);
                _sb.Draw(this.BackgroundImage, Vector2.Zero, rect, Color.White);
                _sb.End();
            }
            else
            {
                this._device.Clear(this.BackgroundColor);
            }

            Matrix world = Matrix.CreateTranslation(-this._viewport.Left, -this._viewport.Top, 0);
            if (_targetSize != _viewport.Size)
            {
                world *= Matrix.CreateScale(
                    1f * _targetSize.X / _viewport.Width,
                    1f * _targetSize.Y / _viewport.Height,
                    1f);
            }

            foreach (var animation in this.Items.Where(_ani => _ani != null))
            {
                if (animation is FrameAnimator framAni)
                {
                    _graphics.Draw(framAni, world);
                }
                else if (animation is ISpineAnimator spineAni)
                {
                    _graphics.Draw(spineAni, world);
                }
            }
        }

        public Texture2D GetPngTexture()
        {
            System.Threading.Monitor.Enter(this._device);
            try
            {
                var texture = new RenderTarget2D(_device, _rt2d.Width, _rt2d.Height, false, SurfaceFormat.Bgra32, DepthFormat.None);
                _device.SetRenderTarget(texture);
                _eff.AlphaMixEnabled = false;

                _device.Clear(Color.Transparent);
                _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, _eff, null);
                _sb.Draw(_rt2d, Vector2.Zero, Color.White);
                _sb.End();
                return texture;
            }
            finally
            {
                _device.SetRenderTargets(_oldBuffer);
                System.Threading.Monitor.Exit(this._device);
            }
        }

        public Texture2D GetGifTexture(Color mixColor, int minMixedAlpha)
        {
            System.Threading.Monitor.Enter(this._device);
            try
            {
                var texture = new RenderTarget2D(_device, _rt2d.Width, _rt2d.Height, false, SurfaceFormat.Bgra32, DepthFormat.None);
                _device.SetRenderTarget(texture);

                _eff.AlphaMixEnabled = true;
                _eff.MixedColor = mixColor;
                _eff.MinMixedAlpha = minMixedAlpha;

                _device.Clear(Color.Transparent);
                _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, _eff, null);
                _sb.Draw(_rt2d, Vector2.Zero, Color.White);
                _sb.End();

                return texture;
            }
            finally
            {
                _device.SetRenderTargets(_oldBuffer);
                System.Threading.Monitor.Exit(this._device);
            }
        }

        public void ResetAll()
        {
            foreach (var aniItem in this.Items)
            {
                aniItem.Reset();
            }
        }

        public int GetMaxLength()
        {
            return this.Items.Select(aniItem => Math.Max(0, aniItem.Length)).Max();
        }

        public int[] GetGifTimeLine(int preferredFrameDelay, int? maxFrameDelay = null)
        {
            if (preferredFrameDelay <= 0)
            {
                preferredFrameDelay = 1;
            }
            if (maxFrameDelay != null && maxFrameDelay < preferredFrameDelay)
            {
                maxFrameDelay = preferredFrameDelay;
            }

            // only calculate the first layer
            foreach (var animation in this.Items.Where(_ani => _ani != null))
            {
                if (animation is FrameAnimator frameAni)
                {
                    // we won't skip any frame even frame delay is greater than preferred delay
                    var timeline = new List<int>();
                    int totalLength = 0;
                    foreach (var frame in frameAni.GetKeyFrames())
                    {
                        totalLength += frame.Length;
                        if (frame.Animated)
                        {
                            for (int ms = frame.Length; ms > 0;)
                            {
                                if (ms >= preferredFrameDelay)
                                {
                                    timeline.Add(preferredFrameDelay);
                                    ms -= preferredFrameDelay;
                                }
                                else
                                {
                                    if (timeline.Count > 0)
                                    {
                                        timeline[timeline.Count - 1] += ms;
                                    }
                                    else
                                    {
                                        // duration of the first frame less than minFrameDelay, but we can't simply ignore it.
                                        timeline.Add(ms);
                                    }
                                    ms = 0;
                                }
                            }
                        }
                        else
                        {
                            if (maxFrameDelay != null)
                            {
                                for (int ms = frame.Length; ms > 0;)
                                {
                                    if (ms >= maxFrameDelay.Value)
                                    {
                                        timeline.Add(maxFrameDelay.Value);
                                        ms -= maxFrameDelay.Value;
                                    }
                                    else
                                    {
                                        timeline.Add(ms);
                                        ms = 0;
                                    }
                                }
                            }
                            else
                            {
                                timeline.Add(frame.Length);
                            }
                        }
                    }

                    return timeline.ToArray();
                }
                else if (animation is ISpineAnimator)
                {
                    return null;
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

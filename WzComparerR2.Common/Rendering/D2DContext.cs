using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.DXGI;

namespace WzComparerR2.Rendering
{
    public sealed class D2DContext : IDisposable
    {
        public Surface DxgiSurface { get; internal set; }
        public RenderTarget D2DRenderTarget { get; internal set; }
        
        internal bool IsBeginEndPair { get; private set; }
        private SolidColorBrush cachedBrush;


        public Brush GetBrush(Microsoft.Xna.Framework.Color color)
        {
            return this.GetBrush(color.XnaToDxColor());
        }

        public Brush GetBrush(SharpDX.Color4 color)
        {
            if (this.cachedBrush == null || this.cachedBrush.IsDisposed)
            {
                this.cachedBrush = new SolidColorBrush(this.D2DRenderTarget, color);
            }
            else
            {
                this.cachedBrush.Color = color;
            }
            return this.cachedBrush;
        }

        public void Dispose()
        {
            this.cachedBrush?.Dispose();
            this.DxgiSurface?.Dispose();
            this.D2DRenderTarget?.Dispose();
        }
    }
}

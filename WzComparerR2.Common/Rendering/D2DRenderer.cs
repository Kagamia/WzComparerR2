using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.Rendering
{
    public class D2DRenderer
    {
        public D2DRenderer(GraphicsDevice graphicsDevice)
        {
            this.GraphicsDevice = graphicsDevice;
        }

        public GraphicsDevice GraphicsDevice { get; private set; }

        internal bool IsBeginEndPair { get; private set; }

        protected D2DContext context;

        public void Begin()
        {
            this.Begin(SharpDX.Matrix3x2.Identity);
        }

        public void Begin(Microsoft.Xna.Framework.Matrix transform)
        {
            var mt3x2 = new SharpDX.Matrix3x2(transform.M11, transform.M12,
                transform.M21, transform.M22,
                transform.M41, transform.M42);
            this.Begin(mt3x2);
        }

        private void Begin(SharpDX.Matrix3x2 transform)
        {
            this.context = D2DFactory.Instance.GetContext(this.GraphicsDevice);
            if (this.context == null)
            {
                throw new Exception("Create D2D context failed.");
            }
            this.context.D2DRenderTarget.Transform = transform;
            this.context.D2DRenderTarget.BeginDraw();
            this.IsBeginEndPair = true;
        }

        public void PushClip(Rectangle clipRect)
        {
            this.context.D2DRenderTarget.PushAxisAlignedClip(clipRect.XnaToDxRect(), SharpDX.Direct2D1.AntialiasMode.PerPrimitive);
        }

        public void PopClip()
        {
            this.context.D2DRenderTarget.PopAxisAlignedClip();
        }

        public void DrawString(D2DFont font, string text, Vector2 position, Color color)
        {
            font.DrawText(this.context, text, position, color);
        }

        public void DrawString(D2DFont font, string text, Vector2 position, Vector2 size, Color color)
        {
            font.DrawText(this.context, text, position, size, color);
        }

        public void DrawLine(Vector2 point0, Vector2 point1, float width, Color color)
        {
            var rt = this.context.D2DRenderTarget;
            rt.DrawLine(new SharpDX.Vector2(point0.X, point0.Y),
                new SharpDX.Vector2(point1.X, point1.Y),
                this.context.GetBrush(color),
                width);
        }

        public void DrawRectangle(Rectangle rectangle, Color color)
        {
            var rt = this.context.D2DRenderTarget;
            rt.DrawRectangle(rectangle.XnaToDxRect(), this.context.GetBrush(color));
        }

        public void FillRectangle(Rectangle rectangle, Color color)
        {
            var rt = this.context.D2DRenderTarget;
            rt.FillRectangle(rectangle.XnaToDxRect(), this.context.GetBrush(color));
        }

        public void FillRoundedRectangle(Rectangle rectangle, float cornerRadius, Color color)
        {
            var rt = this.context.D2DRenderTarget;
            var rRect = new SharpDX.Direct2D1.RoundedRectangle()
            {
                RadiusX = cornerRadius,
                RadiusY = cornerRadius,
                Rect = rectangle.XnaToDxRect()
            };
            rt.FillRoundedRectangle(rRect, this.context.GetBrush(color));
        }

        public void End()
        {
            this.context.D2DRenderTarget.EndDraw();
            this.IsBeginEndPair = false;
        }
    }
}

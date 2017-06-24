using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    public class Camera
    {
        public Camera(GraphicsDeviceManager graphics)
        {
            this.graphics = graphics;
        }

        GraphicsDeviceManager graphics;
        Rectangle worldRect;
        Vector2 center;
        int displayMode;
        bool useWorldRect;

        public GraphicsDeviceManager Graphics
        {
            get { return graphics; }
            set { graphics = value; }
        }

        /// <summary>
        /// 获取或设置摄像机中心对应的世界坐标。
        /// </summary>
        public Vector2 Center
        {
            get { return center; }
            set { center = value; }
        }

        /// <summary>
        /// 获取摄像机宽度。
        /// </summary>
        public int Width
        {
            get { return graphics.PreferredBackBufferWidth; }
        }

        /// <summary>
        /// 获取摄像机高度。
        /// </summary>
        public int Height
        {
            get { return graphics.PreferredBackBufferHeight; }
        }

        /// <summary>
        /// 获取摄像机矩形的世界坐标。
        /// </summary>
        public Rectangle ClipRect
        {
            get
            {
                if (useWorldRect)
                    return worldRect;
                else
                    return new Rectangle((int)center.X - Width / 2, (int)center.Y - Height / 2, Width, Height);
            }
        }

        /// <summary>
        /// 获取摄像机左上角对应的世界坐标。
        /// </summary>
        public Vector2 Origin
        {
            get
            {
                Rectangle rect = ClipRect;
                return new Vector2(rect.X, rect.Y);
            }
        }

        public Rectangle WorldRect
        {
            get { return worldRect; }
            set { worldRect = value; }
        }

        public int DisplayMode
        {
            get { return displayMode; }
            set
            {
                displayMode = value;
                ChangeDisplayMode();
            }
        }

        public bool UseWorldRect
        {
            get { return useWorldRect; }
            set { useWorldRect = value; }
        }

        private void ChangeDisplayMode()
        {
            switch (this.displayMode)
            {
                case 0:
                    graphics.PreferredBackBufferWidth = 800;
                    graphics.PreferredBackBufferHeight = 600;
                    break;
                case 1:
                    graphics.PreferredBackBufferWidth = 1024;
                    graphics.PreferredBackBufferHeight = 768;
                    break;
                case 2:
                    graphics.PreferredBackBufferWidth = 1366;
                    graphics.PreferredBackBufferHeight = 768;
                    break; 
                case 3:
                    graphics.PreferredBackBufferWidth = graphics.GraphicsDevice.DisplayMode.Width;
                    graphics.PreferredBackBufferHeight = graphics.GraphicsDevice.DisplayMode.Height;
                    break;
                default:
                    goto case 0;
            }
            graphics.ApplyChanges();
        }

        public void AdjustToWorldRect()
        {
            if (this.useWorldRect)
                return;

            if (this.Width > worldRect.Width)
            {
                this.center.X = worldRect.Center.X;
            }
            else
            {
                this.center.X = MathHelper.Clamp(this.center.X,
                    worldRect.Left + this.Width / 2,
                    worldRect.Right - this.Width / 2);
            }

            if (this.Height > worldRect.Height)
            {
                this.center.Y = worldRect.Center.Y;
            }
            else
            {
                this.center.Y = MathHelper.Clamp(this.center.Y,
                    worldRect.Top + this.Height / 2,
                    worldRect.Bottom - this.Height / 2);
            }
        }

        public Rectangle MeasureDrawingRect(int width, int height, Vector2 position, Vector2 origin, bool flipX)
        {
            Rectangle drawingRect;
            drawingRect.Width = width;
            drawingRect.Height = height;
            if (flipX)
            {
                drawingRect.X = (int)(position.X + origin.X - width);
                drawingRect.Y = (int)(position.Y - origin.Y);
            }
            else
            {
                drawingRect.X = (int)(position.X - origin.X);
                drawingRect.Y = (int)(position.Y - origin.Y);
            }
            return drawingRect;
        }

        public bool CheckSpriteVisible(RenderFrame frame, Vector2 position, bool flip)
        {
            Rectangle drawingRect;
            return CheckSpriteVisible(frame, position, flip, out drawingRect);
        }

        public bool CheckSpriteVisible(RenderFrame frame, Vector2 position, bool flip, out Rectangle drawingRect)
        {
            if (frame == null || frame.Texture == null)
            {
                drawingRect = Rectangle.Empty;
                return false;
            }
            drawingRect = MeasureDrawingRect(
                frame.Texture.Width,
                frame.Texture.Height,
                position,
                frame.Origin,
                flip);
            return this.ClipRect.Intersects(drawingRect);
        }

        public Point CameraToWorld(Point cameraPoint)
        {
            cameraPoint.X += this.ClipRect.X;
            cameraPoint.Y += this.ClipRect.Y;
            return cameraPoint;
        }
    }
}

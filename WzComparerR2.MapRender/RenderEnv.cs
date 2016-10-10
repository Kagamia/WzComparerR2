using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Rendering;

namespace WzComparerR2.MapRender
{
    public class RenderEnv : IDisposable
    {
        public RenderEnv(Game game, GraphicsDeviceManager graphics)
        {
            this.device = graphics.GraphicsDevice;
            this.camera = new Camera(graphics);
            this.sprite = new SpriteBatchEx(this.device);
            this.input = new InputState(game);
            this.fonts = new MapRenderFonts(this.device);
        }

        Camera camera;
        SpriteBatchEx sprite;
        InputState input;
        GraphicsDevice device;
        MapRenderFonts fonts;

        public Camera Camera
        {
            get { return camera; }
        }

        public SpriteBatchEx Sprite
        {
            get { return sprite; }
        }

        public InputState Input
        {
            get { return input; }
        }

        public MapRenderFonts Fonts
        {
            get { return fonts; }
        }

        public GraphicsDevice GraphicsDevice
        {
            get { return device; }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.sprite.Dispose();
                this.fonts.Dispose();
            }
        }
    }
}

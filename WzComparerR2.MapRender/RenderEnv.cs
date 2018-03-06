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
            this.GraphicsDevice = graphics.GraphicsDevice;
            this.Camera = new Camera(graphics);
            this.Sprite = new SpriteBatchEx(this.GraphicsDevice);
            this.D2DRenderer = new D2DRenderer(this.GraphicsDevice);
            this.Input = new InputState(game);
            this.Fonts = new MapRenderFonts();
        }

        public Camera Camera { get; private set; }
        public SpriteBatchEx Sprite { get; private set; }
        public D2DRenderer D2DRenderer { get; private set; }
        public InputState Input { get; private set; }
        public GraphicsDevice GraphicsDevice { get; private set; }
        public MapRenderFonts Fonts { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Sprite.Dispose();
                this.Fonts.Dispose();
            }
        }
    }
}

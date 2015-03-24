using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace WzComparerR2.MapRender
{
    class Frm1 : Game
    {
        public Frm1()
        {
            graphics = new GraphicsDeviceManager(this);
        }

        GraphicsDeviceManager graphics;
        SpriteBatchEx sb;
        XnaFont font;
        Texture2D t2d;
        

        protected override void Initialize()
        {
            base.Initialize();
            sb = new SpriteBatchEx(this.GraphicsDevice);
            font = new XnaFont(this.GraphicsDevice, "宋体", 32f);
            t2d = Texture2D.FromFile(this.GraphicsDevice, @"D:\Image\cnloli背景\067 萌狼⑨.jpg");
            mt = Matrix.Identity;
            this.IsMouseVisible = true;
        }

        Matrix mt;
        MouseState lastMouse;
        protected override void Update(GameTime gameTime)
        {
            MouseState mouse = Mouse.GetState();

            if (mouse.ScrollWheelValue != lastMouse.ScrollWheelValue)
            {
                float delta = (mouse.ScrollWheelValue - lastMouse.ScrollWheelValue) / 120f;
                mt.M11 *= (1 + delta * 0.1f);
                mt.M22 *= (1 + delta * 0.1f);
            }
            else if (mouse.LeftButton == ButtonState.Pressed)
            {
                mt.M41 += (mouse.X - lastMouse.X);
                mt.M42 += (mouse.Y - lastMouse.Y);
            }

            lastMouse = mouse;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            sb.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, mt);
            sb.Draw(t2d, new Rectangle(0, 0, t2d.Width, t2d.Height), Color.White);
            sb.End();

            base.Draw(gameTime);
        }
    }
}

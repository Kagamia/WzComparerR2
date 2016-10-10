using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender.Patches
{
    public class BackPatch : RenderPatch
    {
        public BackPatch()
        {
        }

        int cx;
        int cy;
        int rx;
        int ry;
        int screenMode;
        TileMode tileMode;

        // RenderArgs
        Vector2 origin2;
        Rectangle tileRect;

        public int Cx
        {
            get { return cx; }
            set { cx = value; }
        }

        public int Cy
        {
            get { return cy; }
            set { cy = value; }
        }

        public int Rx
        {
            get { return rx; }
            set { rx = value; }
        }

        public int Ry
        {
            get { return ry; }
            set { ry = value; }
        }

        public int ScreenMode
        {
            get { return screenMode; }
            set { screenMode = value; }
        }

        public TileMode TileMode
        {
            get { return tileMode; }
            set { tileMode = value; }
        }

        public override void Update(GameTime gameTime, RenderEnv env)
        {
            base.Update(gameTime, env);


            if (this.RenderArgs.CurrentIndex < 0)
                return;
            RenderFrame frame = this.Frames[this.RenderArgs.CurrentIndex];

            //计算背景的二次偏移
            int cx = (this.cx == 0 ? frame.Texture.Width : this.cx),
                cy = (this.cy == 0 ? frame.Texture.Height : this.cy);
            Vector2 origin2 = new Vector2();

            double ms = gameTime.TotalGameTime.TotalSeconds;

            if ((this.TileMode & TileMode.ScrollHorizontial) != 0)
            {
                origin2.X = (float)((this.rx * 5 * ms) % cx);// +this.Camera.Center.X * (100 - Math.Abs(this.rx)) / 100;
            }
            else
            {
                origin2.X = env.Camera.Center.X * (100 + this.rx) / 100;
            }

            if ((this.TileMode & TileMode.ScrollVertical) != 0)
            {
                origin2.Y = (float)((this.ry * 5 * ms) % cy);// +this.Camera.Center.Y * (100 - Math.Abs(this.ry)) / 100;
            }
            else
            {
                //origin2.Y = env.Camera.Center.Y * (100 + this.ry) / 100;
                origin2.Y = (env.Camera.Center.Y) * (100 + this.ry) / 100; //屏幕大小适配补丁
            }

            origin2.Y += (env.Camera.Height - 600);
            this.origin2 = origin2;
            

            //计算平铺绘制参数
            if (this.TileMode != TileMode.None)
            {
                this.RenderArgs.Culled = false;
                Rectangle originRect = env.Camera.MeasureDrawingRect(frame.Texture.Width, frame.Texture.Height, this.Position + origin2, frame.Origin, this.Flip);
                int l, t, r, b;
                if ((this.TileMode & TileMode.Horizontal) != 0)
                {
                    l = (int)Math.Floor((double)(env.Camera.ClipRect.Left - originRect.X) / cx);
                    r = (int)Math.Ceiling((double)(env.Camera.ClipRect.Right - originRect.X) / cx);
                }
                else
                {
                    l = 0;
                    r = 1;
                }
                if ((this.TileMode & TileMode.Vertical) != 0)
                {
                    t = (int)Math.Floor((double)(env.Camera.ClipRect.Top - originRect.Y) / cy);
                    b = (int)Math.Ceiling((double)(env.Camera.ClipRect.Bottom - originRect.Y) / cy);
                }
                else
                {
                    t = 0;
                    b = 1;
                }
                this.tileRect = new Rectangle(l, t, r - l, b - t);
            }
            else
            {
                this.RenderArgs.Culled = !env.Camera.CheckSpriteVisible(frame, this.Position + this.origin2, this.Flip);
                this.tileRect = new Rectangle(0, 0, 1, 1);
            }

            if (this.screenMode != 0)
            {
                this.RenderArgs.Culled |= (this.screenMode != env.Camera.DisplayMode + 1);
            }

            this.RenderArgs.DisplayRectangle = Rectangle.Empty;
        }

        public override void Draw(GameTime gameTime, RenderEnv env)
        {
            if (this.RenderArgs.CurrentIndex < 0)
            {
                return;
            }
            RenderFrame frame = this.Frames[this.RenderArgs.CurrentIndex];
            float curPer = this.RenderArgs.CurrentPercent;
            float alpha = (frame.A0 * (1 - curPer) + frame.A1 * curPer) / 255.0f * this.Alpha / 255.0f;

            Vector2 pos = this.Position + this.origin2 - env.Camera.Origin;
            pos = new Vector2((float)Math.Floor(pos.X), (float)Math.Floor(pos.Y));

            int cx = (this.cx == 0 ? frame.Texture.Width : this.cx),
                cy = (this.cy == 0 ? frame.Texture.Height : this.cy);
            for (int j = this.tileRect.Top; j < this.tileRect.Bottom; j++)
            {
                for (int i = this.tileRect.Left; i < this.tileRect.Right; i++)
                {
                    Vector2 pos2 = pos + new Vector2(i * cx, j * cy);

                    env.Sprite.Draw(frame.Texture,
                       pos2,
                       null,
                       new Color(Color.White, alpha),
                       0f,
                       this.Flip? new Vector2(frame.Texture.Width - frame.Origin.X, frame.Origin.Y) : frame.Origin,
                       1f,
                       this.Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                       0f);
                }
            }
        }
    }
}

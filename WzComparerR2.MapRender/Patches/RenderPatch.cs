#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender.Patches
{
    public class RenderPatch : IDisposable
    {
        public RenderPatch()
        {
            zIndex = new int[8];
            flip = false;
            renderArgs = new RenderArgs();
            alpha = 255;
        }

        string name;
        Vector2 position;
        bool flip;
        int[] zIndex;
        RenderAnimate frames;
        RenderObjectType objectType;
        TimeSpan playStateChangedTime;
        RenderArgs renderArgs;
        int alpha;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }

        public bool Flip
        {
            get { return flip; }
            set { flip = value; }
        }

        public int[] ZIndex
        {
            get { return zIndex; }
        }

        public RenderAnimate Frames
        {
            get { return frames; }
            set { frames = value; }
        }

        public virtual RenderObjectType ObjectType
        {
            get { return objectType; }
            set { objectType = value; }
        }

        public TimeSpan PlayStateChangedTime
        {
            get { return playStateChangedTime; }
            set { playStateChangedTime = value; }
        }

        public RenderArgs RenderArgs
        {
            get { return renderArgs; }
        }

        public int Alpha
        {
            get { return alpha; }
            set { alpha = value; }
        }

        public int UpdateCurrentFrameIndex(TimeSpan totalTime)
        {
            float framePlayed;
            return UpdateCurrentFrameIndex(totalTime, out framePlayed);
        }

        public virtual int UpdateCurrentFrameIndex(TimeSpan totalTime, out float framePlayed)
        {
            framePlayed = 0f;
            if (this.frames == null || this.frames.Length == 0)
            {
                return -1;
            }
            int[] timeLine = this.frames.TimeLine;
            if (timeLine[timeLine.Length - 1] <= 0)
            {
                return 0;
            }

            double ms = (totalTime.TotalMilliseconds - playStateChangedTime.TotalMilliseconds);
            int startTimeLine = 1;
            if (this.frames.Repeat <= 0 || this.frames.Repeat >= this.frames.Length) //使用全帧绘制
            {
                ms %= timeLine[timeLine.Length - 1];
            }
            else
            {
                if (ms < timeLine[timeLine.Length - 1]) //首次绘制
                {
                    ms %= timeLine[timeLine.Length - 1];
                }
                else //循环段
                {
                    startTimeLine = frames.Repeat + 1;
                    ms -= timeLine[timeLine.Length - 1];
                    int newDelay = timeLine[timeLine.Length - 1] - timeLine[frames.Repeat];
                    if (newDelay <= 0)
                        return frames.Repeat;
                    ms = ms % newDelay + timeLine[frames.Repeat];
                }
            }

            for (int i = startTimeLine; i < timeLine.Length; i++)
            {
                if (ms < timeLine[i])
                {
                    framePlayed = (float)((ms - timeLine[i - 1]) / (timeLine[i] - timeLine[i - 1]));
                    return i - 1;
                }
            }
            return frames.Repeat;
        }

        public virtual void Update(GameTime gameTime, RenderEnv env)
        {
            float played;
            int index = this.UpdateCurrentFrameIndex(gameTime.TotalGameTime, out played);
            if (index > -1)
            {
                //获取当前帧绘制参数
                RenderFrame frame = this.Frames[index];
                this.RenderArgs.CurrentIndex = index;
                this.RenderArgs.CurrentPercent = played;
                Rectangle rect;
                this.RenderArgs.Culled = !env.Camera.CheckSpriteVisible(frame, this.position, this.flip, out rect);
                this.renderArgs.DisplayRectangle = rect;
            }
            else
            {
                this.RenderArgs.CurrentIndex = -1;
                this.RenderArgs.CurrentPercent = 0f;
                this.RenderArgs.Culled = true;
            }
        }

        public virtual void Draw(GameTime gameTime, RenderEnv env)
        {
            if (this.RenderArgs.CurrentIndex < 0)
            {
                return;
            }

            RenderFrame frame = this.Frames[this.RenderArgs.CurrentIndex];
            Vector2 cameraOrigin = env.Camera.Origin;

            float curPer = this.RenderArgs.CurrentPercent;
            float alpha = (frame.A0 * (1 - curPer) + frame.A1 * curPer) / 255.0f * this.alpha / 255.0f;

            env.Sprite.Draw(frame.Texture,
               this.Position - cameraOrigin,
               null,
               new Color(Color.White, alpha),
               0f,
               this.flip ? new Vector2(frame.Texture.Width - frame.Origin.X, frame.Origin.Y) : frame.Origin,
               1f,
               this.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
               0f);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Frames != null && this.Frames.Length > 0)
                {
                    foreach (RenderFrame frame in this.Frames)
                    {
                        if (!frame.Texture.IsDisposed)
                        {
                            frame.Texture.Dispose();
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " + this.name;
        }
    }
}
#endif
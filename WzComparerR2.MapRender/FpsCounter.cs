using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    public class FpsCounter : DrawableGameComponent
    {
        public FpsCounter(Game game) : base(game)
        {
            this.CountInterval = TimeSpan.FromMilliseconds(1000);
            this.swUpdate = new Stopwatch();
            this.swDraw = new Stopwatch();
        }

        public TimeSpan CountInterval { get; set; }
        public double UpdatePerSec { get; private set; }
        public double DrawPerSec { get; private set; }
        public bool UseStopwatch { get; set; }

        private TimeSpan lastUpdate;
        private int updateCount;
        private Stopwatch swUpdate;

        private TimeSpan lastDraw;
        private int drawCount;
        private Stopwatch swDraw;

        public override void Update(GameTime gameTime)
        {
            this.updateCount++;

            if (UseStopwatch)
            {
                if (!this.swUpdate.IsRunning)
                {
                    this.swUpdate.Start();
                    return;
                }
                TimeSpan totalElapsed = this.swUpdate.Elapsed;
                if (totalElapsed >= CountInterval)
                {
                    this.swUpdate.Reset();
                    this.UpdatePerSec = this.updateCount / totalElapsed.TotalSeconds;
                    this.updateCount = 0;
                    this.swUpdate.Start();
                }
            }
            else
            {
                TimeSpan totalElapsed = gameTime.TotalGameTime - lastUpdate;
                if (totalElapsed >= this.CountInterval)
                {
                    this.UpdatePerSec = this.updateCount / totalElapsed.TotalSeconds;
                    this.updateCount = 0;
                    this.lastUpdate = gameTime.TotalGameTime;
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            this.drawCount++;
            if (UseStopwatch)
            {
                if (!this.swDraw.IsRunning)
                {
                    this.swDraw.Start();
                    return;
                }
                TimeSpan totalElapsed = this.swDraw.Elapsed;
                if (totalElapsed >= CountInterval)
                {
                    this.swDraw.Reset();
                    this.DrawPerSec = this.drawCount / totalElapsed.TotalSeconds;
                    this.drawCount = 0;
                    this.swDraw.Start();
                }
            }
            else
            {
                TimeSpan totalElapsed = gameTime.TotalGameTime - lastDraw;
                if (totalElapsed >= this.CountInterval)
                {
                    this.DrawPerSec = this.drawCount / totalElapsed.TotalSeconds;
                    this.drawCount = 0;
                    this.lastDraw = gameTime.TotalGameTime;
                }
            }
        }
    }
}
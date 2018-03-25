using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    public class Particle
    {
        public Vector2 Pos;
        public float Angle;
        public Color StartColor;
        public Color EndColor;
        public float StartSize;
        public float EndSize;
        public float StartSpin;
        public float EndSpin;
        public float Life;

        //gravity
        public Vector2 Dir;
        public float RadialAcc;
        public float TangentialAcc;

        //radius
        public float StartRadius;
        public float EndRadius;
        public float RotatePerSecond;

        //lifecycle
        public float Time;
        public float NormalizedTime;

        public void Reset()
        {
            this.Pos = Vector2.Zero;
            this.StartColor = new Color();
            this.EndColor = new Color();
            this.StartSize = 0;
            this.EndSize = 0;
            this.StartSpin = 0;
            this.EndSpin = 0;
            this.Life = 0;

            this.Dir = Vector2.Zero;
            this.RadialAcc = 0;
            this.TangentialAcc = 0;

            this.StartRadius = 0;
            this.EndRadius = 0;
            this.RotatePerSecond = 0;

            this.Time = 0;
            this.NormalizedTime = 0;
        }
    }
}

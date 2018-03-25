using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    public class ParticleEmitter
    {
        public ParticleEmitter(ParticleSystem particleSystem)
        {
            this.Owner = particleSystem;
        }

        public ParticleSystem Owner { get; private set; }

        //初始速度方向
        public float Angle { get; set; }
        public float AngleVar { get; set; }
        //颜色
        public Color StartColor { get; set; }
        public Color StartColorVar { get; set; }
        public Color EndColor { get; set; }
        public Color EndColorVar { get; set; }
        //大小
        public float StartSize { get; set; }
        public float StartSizeVar { get; set; }
        public float EndSize { get; set; }
        public float EndSizeVar { get; set; }
        //位置
        public Vector2 Pos { get; set; }
        public Vector2 PosVar { get; set; }
        //旋转
        public float StartSpin { get; set; }
        public float StartSpinVar { get; set; }
        public float EndSpin { get; set; }
        public float EndSpinVar { get; set; }

        //gravity相关
        public float Speed { get; set; }
        public float SpeedVar { get; set; }
        public float RadialAccel { get; set; }
        public float RadialAccelVar { get; set; }
        public float TangentialAccel { get; set; }
        public float TangentialAccelVar { get; set; }
        public bool RotationIsDir { get; set; }

        //radius相关
        public float StartRadius { get; set; }
        public float StartRadiusVar { get; set; }
        public float EndRadius { get; set; }
        public float EndRadiusVar { get; set; }
        public float RotatePerSecond { get; set; }
        public float RotatePerSecondVar { get; set; }

        //life
        public float Life { get; set; }
        public float LifeVar { get; set; }

        //发射频率
        public float EmissionRate { get; set; } = 200;

        public Particle Emit()
        {
            var particle = this.CreateParticle();
            this.InitParticle(particle);
            return particle;
        }

        private Particle CreateParticle()
        {
            Particle particle;
            if (this.Owner.ParticlePool.Count > 0)
            {
                particle = this.Owner.ParticlePool.Pop();
                particle.Reset();
            }
            else
            {
                particle = new Particle();
            }
            return particle;
        }

        private void InitParticle(Particle particle)
        {
            var r = this.Owner.Rand;
            //计算位置 大小 颜色 自旋
            particle.Pos = r.NextVar(this.Pos, this.PosVar);
            particle.StartSize = r.NextVar(this.StartSize, this.StartSizeVar, true);
            particle.EndSize = r.NextVar(this.EndSize, this.EndSizeVar, true);
            particle.StartColor = r.NextVar(this.StartColor, this.StartColorVar);
            particle.EndColor = r.NextVar(this.EndColor, this.EndColorVar);
            particle.StartSpin = r.NextVar(this.StartSpin, this.StartSpinVar);
            particle.EndSpin = r.NextVar(this.EndSpin, this.EndSpinVar);

            //计算速度方向
            var speed = r.NextVar(this.Speed, this.SpeedVar);
            var rotAngle = r.NextVar(this.Angle, this.AngleVar);
            var rot = MathHelper.ToRadians(rotAngle);
            var dir = new Vector2((float)Math.Cos(rot), -(float)Math.Sin(rot));
            particle.Dir = dir * speed;
            if (this.RotationIsDir)
            {
                var deltaSpin = particle.EndSpin - particle.StartSpin;
                particle.StartSpin = rotAngle;
                particle.EndSize = rotAngle + deltaSpin;
            }

            //计算圆周运动
            particle.StartRadius = r.NextVar(this.StartRadius, this.StartRadiusVar, true);
            particle.EndRadius = r.NextVar(this.EndRadius, this.EndRadiusVar, true);
            particle.RotatePerSecond = r.NextVar(this.RotatePerSecond, this.RotatePerSecondVar, true);
            particle.Angle = rotAngle;

            //初始化加速度
            particle.RadialAcc = r.NextVar(this.RadialAccel, this.RadialAccelVar);
            particle.TangentialAcc = r.NextVar(this.TangentialAccel, this.TangentialAccelVar);

            //生命周期
            particle.Life = r.NextVar(this.Life, this.LifeVar, true);
        }
    }
}

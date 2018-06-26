using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Animation;

namespace WzComparerR2.MapRender
{
    public class ParticleSystem
    {
        public ParticleSystem(IRandom random)
        {
            this.Rand = random;
            this.ParticlePool = new Stack<Particle>();
            this.Groups = new List<ParticleGroup>();
        }

        public IRandom Rand { get; private set; }
        public Stack<Particle> ParticlePool { get; private set; }
        public ParticleEmitter Emitter { get; private set; }
        public List<ParticleGroup> Groups { get; private set; }

        public int ParticleCount { get; set; }
        public float Duration { get; set; }
        public Vector2 Gravity { get; set; }
        public Frame Texture { get; set; }
        public bool OpacityModifyRGB { get; set; }
        public ParticleBlendFunc BlendFuncSrc { get; set; }
        public ParticleBlendFunc BlendFuncDst { get; set; }

        private AlphaPoint[] alphaPoints;

        public void LoadDescription(ParticleDesc desc)
        {
            this.Texture = desc.Texture;
            this.ParticleCount = desc.TotalParticle;
            this.Duration = desc.Duration;
            this.OpacityModifyRGB = desc.OpacityModifyRGB;
            if (desc.Gravity != null)
            {
                this.Gravity = new Vector2(desc.Gravity.X, desc.Gravity.Y);
            }
            var alphaPointList = new List<AlphaPoint>();
            if (desc.MiddlePoint0 > 0)
            {
                alphaPointList.Add(new AlphaPoint(desc.MiddlePoint0 / 100f, desc.MiddlePointAlpha0));
            }
            if (desc.MiddlePoint1 > 0)
            {
                alphaPointList.Add(new AlphaPoint(desc.MiddlePoint1 / 100f, desc.MiddlePointAlpha1));
            }
            this.alphaPoints = alphaPointList.ToArray();
            this.BlendFuncSrc = desc.BlendFuncSrc;
            this.BlendFuncDst = desc.BlendFuncDst;
            this.CreateEmitter(desc);
        }

        private void CreateEmitter(ParticleDesc desc)
        {
            var emitter = new ParticleEmitter(this);
            emitter.Angle = desc.Angle;
            emitter.AngleVar = desc.AngleVar;
            emitter.StartColor = desc.StartColor;
            emitter.StartColorVar = desc.StartColorVar;
            emitter.EndColor = desc.EndColor;
            emitter.EndColorVar = desc.EndColorVar;
            emitter.StartSize = desc.StartSize;
            emitter.StartSizeVar = desc.StartSizeVar;
            emitter.EndSize = desc.EndSize;
            emitter.EndSizeVar = desc.EndSizeVar;
            emitter.Pos = new Vector2(desc.PosX, desc.PosY);
            emitter.PosVar = new Vector2(desc.PosVarX, desc.PosVarY);
            emitter.StartSpin = desc.StartSpin;
            emitter.StartSpinVar = desc.StartSpinVar;
            emitter.EndSpin = desc.EndSpin;
            emitter.EndSpinVar = desc.EndSpinVar;
            if (desc.Gravity != null)
            {
                emitter.Speed = desc.Gravity.Speed;
                emitter.SpeedVar = desc.Gravity.SpeedVar;
                emitter.RadialAccel = desc.Gravity.RadialAccel;
                emitter.RadialAccelVar = desc.Gravity.RadialAccelVar;
                emitter.TangentialAccel = desc.Gravity.TangentialAccel;
                emitter.TangentialAccelVar = desc.Gravity.TangentialAccelVar;
                emitter.RotationIsDir = desc.Gravity.RotationIsDir;
            }
            if (desc.Radius != null)
            {

            }
            emitter.Life = desc.Life;
            emitter.LifeVar = desc.LifeVar;

            //计算发射速率
            if (desc.Duration > 0)
            {
                emitter.EmissionRate = desc.TotalParticle / Math.Min(desc.Duration, 1);
            }
            else
            {
                float lifeMin = Math.Max(0, desc.Life - desc.LifeVar);
                float lifeMax = Math.Max(0, desc.Life + desc.LifeVar);
                var life = (lifeMin + lifeMax) / 2;
                if (life <= 0)
                {
                    life = 1;
                }
                emitter.EmissionRate = desc.TotalParticle / life;
            }

            this.Emitter = emitter;
        }

        public ParticleGroup CreateGroup(string name = null)
        {
            var pGroup = new ParticleGroup(this);
            pGroup.Name = name;
            pGroup.Particles.Capacity = this.ParticleCount;
            return pGroup;
        }

        public void Update(TimeSpan elapsed)
        {
            foreach (var pGroup in this.Groups)
            {
                this.UpdateGroup(pGroup, elapsed);
            }
        }

        public void UpdateGroup(ParticleGroup pGroup, TimeSpan elapsed)
        {
            var time = (float)elapsed.TotalSeconds;

            if (pGroup.IsActive)
            {
                if (pGroup.Particles.Count < this.ParticleCount) //生成粒子
                {
                    float emitCount = this.Emitter.EmissionRate * time;
                    var count = (int)emitCount + (this.Rand.NextPercent(emitCount % 1) ? 1 : 0);
                    count = Math.Min(count, this.ParticleCount - pGroup.Particles.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var p = this.Emitter.Emit();
                        if (p.Life <= 0) //fix bug
                        {
                            continue;
                        }
                        pGroup.Particles.Add(p);
                    }
                }

                //计算时间
                if (this.Duration > 0)
                {
                    pGroup.Time += time;
                    if (pGroup.Time > this.Duration)
                    {
                        pGroup.IsActive = false;
                    }
                }
            }

            //更新所有粒子
            if (pGroup.Particles.Count > 0)
            {
                pGroup.Particles.ForEach(p =>
                {
                    p.Time += time;
                    p.NormalizedTime = p.Life <= 0 ? 0 : (p.Time / p.Life);
                    if (p.NormalizedTime >= 1)
                    {
                        return;
                    }

                    //更新粒子
                    var accDir = p.Pos;
                    if (accDir != Vector2.Zero) //计算方向
                    {
                        accDir.Normalize();
                    }
                    var radial = accDir * p.RadialAcc; //法线加速度矢量
                    //var tangent = new Vector2(-accDir.Y, accDir.X) * p.TangentialAcc; //切线加速度矢量
                    var tangent = Vector2.Zero; //tangent not works in the game?
                    var acc = this.Gravity + radial + tangent; //总加速度矢量
                    p.Dir += acc * time; //计算加速度
                    p.Pos += p.Dir * time; //计算位置

                    //计算旋转
                    p.Angle += p.RotatePerSecond * time;
                    var rad = MathHelper.Lerp(p.StartRadius, p.EndRadius, p.NormalizedTime);
                    if (rad > 0)
                    {
                        var radian = MathHelper.ToRadians(p.Angle);
                        accDir = new Vector2((float)Math.Cos(radian), (float)Math.Sin(radian));
                        p.Pos += accDir * rad * time;
                    }
                });

                //回收粒子
                pGroup.Particles.RemoveAll(p =>
                {
                    if (p.NormalizedTime >= 1)
                    {
                        this.CollectParticle(p);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
            }
        }

        private void CollectParticle(Particle particle)
        {
            this.ParticlePool.Push(particle);
        }

        public void Draw(SpriteBatch sprite, Vector2 position)
        {
            foreach (var pGroup in this.Groups)
            {
                this.DrawGroup(sprite, pGroup, position);
            }
        }

        public void DrawGroup(SpriteBatch sprite, ParticleGroup pGroup, Vector2 position)
        {
            if (this.Texture?.Texture == null)
            {
                return;
            }

            var textureSize = this.Texture.AtlasRect?.Size.ToVector2() ?? new Vector2(this.Texture.Texture.Width, this.Texture.Texture.Height);
            var rad = textureSize.Length() / (float)Math.Sqrt(2);
            var origin = this.Texture.Origin.ToVector2();
            position += pGroup.Position;

            foreach (var p in pGroup.Particles)
            {
                var color = Lerp(p.StartColor, p.EndColor, p.NormalizedTime);
                var size = MathHelper.Lerp(p.StartSize, p.EndSize, p.NormalizedTime);
                var scale = size / rad;
                var rot = MathHelper.ToRadians(MathHelper.Lerp(p.StartSpin, p.EndSpin, p.NormalizedTime));
                //重新计算alpha
                if (this.alphaPoints.Length > 0)
                {
                    color.A = (byte)CalcAlphaFromPoints(p);
                }

                if (this.OpacityModifyRGB)
                {
                    if (color.R == 0 && color.G == 0 && color.B == 0)
                    {
                        color.R = color.G = color.B = color.A;
                    }
                    else
                    {
                        //float alpha = color.A / 255.0f; ;
                        //color.R = (byte)(color.R * alpha);
                        //color.G = (byte)(color.G * alpha);
                        //color.B = (byte)(color.B * alpha);
                    }
                }

                var pos = p.Pos + position;
                sprite.Draw(this.Texture.Texture, pos, this.Texture.AtlasRect, color, rot, origin, scale, SpriteEffects.None, 0);
            }
        }

        private int CalcAlphaFromPoints(Particle p)
        {
            int? alpha = null;
            var time = p.NormalizedTime;
            if (time < this.alphaPoints[0].Time)
            {
                alpha = Lerp(p.StartColor.A, this.alphaPoints[0].Alpha,
                    0, this.alphaPoints[0].Time,
                    time);
            }
            for (int i = 1; alpha == null && i < this.alphaPoints.Length; i++)
            {
                if (time < this.alphaPoints[i].Time)
                {
                    alpha = Lerp(this.alphaPoints[i - 1].Alpha, this.alphaPoints[i].Alpha,
                        this.alphaPoints[i - 1].Time, this.alphaPoints[i].Time,
                        time);
                    break;
                }
            }
            if (alpha == null)
            {
                alpha = Lerp(this.alphaPoints[this.alphaPoints.Length - 1].Alpha, p.EndColor.A,
                  this.alphaPoints[this.alphaPoints.Length - 1].Time, 1,
                  time);
            }
            return alpha.Value;
        }

        private Color Lerp(Color color1, Color color2, float amount)
        {
            return new Color(
                (byte)MathHelper.Lerp(color1.R, color2.R, amount),
                (byte)MathHelper.Lerp(color1.G, color2.G, amount),
                (byte)MathHelper.Lerp(color1.B, color2.B, amount),
                (byte)MathHelper.Lerp(color1.A, color2.A, amount)
                );
        }

        private int Lerp(int int1, int int2, float from, float to, float value)
        {
            if (from == to)
            {
                return int2;
            }

            var amount = (value - from) / (to - from);
            return (int)MathHelper.Lerp(int1, int2, amount);
        }

        private struct AlphaPoint
        {
            public AlphaPoint(float time, int alpha)
            {
                this.Time = time;
                this.Alpha = alpha;
            }

            public float Time;
            public int Alpha;
        }
    }

    public class ParticleGroup
    {
        public ParticleGroup(ParticleSystem owner)
        {
            this.Owner = owner;
            this.Particles = new List<Particle>();
        }

        public ParticleSystem Owner { get; private set; }
        public string Name { get; set; }
        public List<Particle> Particles { get; private set; }
        public Vector2 Position { get; set; }
        public float Time { get; set; }
        public bool IsActive { get; set; }

        public void Active()
        {
            this.IsActive = true;
            this.Time = 0;
        }
    }
}

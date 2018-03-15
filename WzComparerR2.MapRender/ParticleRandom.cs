using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender
{
    class ParticleRandom : IRandom
    {
        public ParticleRandom() : this(new Random())
        {

        }

        public ParticleRandom(Random random)
        {
            this.Random = random;
        }

        public Random Random { get; private set; }

        private double NextMinOneToOne()
        {
            return this.Random.NextDouble() * 2 - 1;
        }

        public float NextVar(float baseValue, float varRange, bool nonNegative = false)
        {
            if (varRange == 0)
            {
                return baseValue;
            }

            var val = baseValue + (float)this.NextMinOneToOne() * varRange;
            if (nonNegative && val < 0)
            {
                val = 0;
            }
            return val;
        }

        public int NextVar(int baseValue, int varRange, bool nonNegative = false)
        {
            if (varRange == 0)
            {
                return baseValue;
            }
            var val = this.Random.Next(baseValue - varRange, baseValue + varRange + 1);
            if (nonNegative && val < 0)
            {
                val = 0;
            }
            return val;
        }

        public Vector2 NextVar(Vector2 baseValue, Vector2 varRange)
        {
            var x = this.NextVar(baseValue.X, varRange.X, false);
            var y = this.NextVar(baseValue.Y, varRange.Y, false);
            return new Vector2(x, y);
        }

        public Color NextVar(Color baseValue, Color varRange)
        {
            var baseColorVec = baseValue.ToVector4();
            var varRangeVec = varRange.ToVector4();
            var r = this.NextVar(baseColorVec.X, varRangeVec.X, false);
            var g = this.NextVar(baseColorVec.Y, varRangeVec.Y, false);
            var b = this.NextVar(baseColorVec.Z, varRangeVec.Z, false);
            var a = this.NextVar(baseColorVec.W, varRangeVec.W, false);
            return new Color(r, g, b, a);
        }

        public int Next(int maxValue)
        {
            return this.Random.Next(maxValue);
        }

        public bool NextPercent(float percent)
        {
            return this.Random.NextDouble() < percent;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Animation
{
    public struct ModelBound
    {
        public float minX, minY, maxX, maxY;

        public bool IsEmpty
        {
            get { return minX >= maxX || minY >= maxY; }
        }

        public void Update(float[] vertices, int count)
        {
            int i = 0;
            if (count % 4 != 0)
            {
                if (vertices[0] > maxX) maxX = vertices[0];
                if (vertices[0] < minX) minX = vertices[0];
                if (vertices[1] > maxY) maxY = vertices[1];
                if (vertices[1] < minY) minY = vertices[1];
                i += 2;
            }

            while (i < count)
            {
                if (vertices[i] > vertices[i + 2])
                {
                    if (vertices[i] > maxX) maxX = vertices[i];
                    if (vertices[i + 2] < minX) minX = vertices[i + 2];
                }
                else
                {
                    if (vertices[i + 2] > maxX) maxX = vertices[i + 2];
                    if (vertices[i] < minX) minX = vertices[i];
                }

                if (vertices[i + 1] > vertices[i + 3])
                {
                    if (vertices[i + 1] > maxY) maxY = vertices[i + 1];
                    if (vertices[i + 3] < minY) minY = vertices[i + 3];
                }
                else
                {
                    if (vertices[i + 3] > maxY) maxY = vertices[i + 3];
                    if (vertices[i + 1] < minY) minY = vertices[i + 1];
                }

                i += 4;
            }
        }

        public Rectangle GetBound()
        {
            if (IsEmpty)
            {
                return new Rectangle();
            }

            return new Rectangle((int)Math.Round(minX),
                (int)Math.Round(minY),
                (int)Math.Round(maxX - minX),
                (int)Math.Round(maxY - minY));
        }

        public static ModelBound Empty
        {
            get
            {
                var b = new ModelBound();
                b.minX = b.minY = float.MaxValue;
                b.maxX = b.maxY = float.MinValue;
                return b;
            }
        }
    }
}

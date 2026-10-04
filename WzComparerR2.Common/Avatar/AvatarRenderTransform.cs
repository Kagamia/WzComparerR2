using System;
using System.Drawing;
using System.Numerics;

namespace WzComparerR2.Avatar
{
    public sealed class AvatarRenderTransform
    {
        public static AvatarRenderTransform Identity { get; } = new AvatarRenderTransform(0, PointF.Empty, PointF.Empty, false, 0);

        public AvatarRenderTransform(int rotationDegrees, PointF rotationPivot, PointF translation, bool flipX, float flipAxisX)
        {
            ValidateFinite(rotationPivot.X, nameof(rotationPivot));
            ValidateFinite(rotationPivot.Y, nameof(rotationPivot));
            ValidateFinite(translation.X, nameof(translation));
            ValidateFinite(translation.Y, nameof(translation));
            ValidateFinite(flipAxisX, nameof(flipAxisX));

            this.RotationDegrees = rotationDegrees;
            this.RotationPivot = rotationPivot;
            this.Translation = translation;
            this.FlipX = flipX;
            this.FlipAxisX = flipAxisX;

            Matrix3x2 matrix = Matrix3x2.CreateTranslation(-rotationPivot.X, -rotationPivot.Y)
                * CreateRotation(rotationDegrees)
                * Matrix3x2.CreateTranslation(rotationPivot.X + translation.X, rotationPivot.Y + translation.Y);
            if (flipX)
                matrix *= new Matrix3x2(-1, 0, 0, 1, 2 * flipAxisX, 0);

            ValidateFinite(matrix.M31, nameof(translation));
            ValidateFinite(matrix.M32, nameof(translation));
            this.Matrix = matrix;
        }

        public int RotationDegrees { get; }
        public PointF RotationPivot { get; }
        public PointF Translation { get; }
        public bool FlipX { get; }
        public float FlipAxisX { get; }
        public Matrix3x2 Matrix { get; }

        public PointF TransformPoint(PointF point)
        {
            Vector2 result = Vector2.Transform(new Vector2(point.X, point.Y), this.Matrix);
            return new PointF(result.X, result.Y);
        }

        internal static int GetRightAngleRotation(int degrees)
        {
            return degrees == 90 || degrees == 180 || degrees == 270 ? degrees : 0;
        }

        private static Matrix3x2 CreateRotation(int degrees)
        {
            int normalized = degrees % 360;
            if (normalized < 0)
                normalized += 360;

            switch (normalized)
            {
                case 0: return Matrix3x2.Identity;
                case 90: return new Matrix3x2(0, 1, -1, 0, 0, 0);
                case 180: return new Matrix3x2(-1, 0, 0, -1, 0, 0);
                case 270: return new Matrix3x2(0, -1, 1, 0, 0, 0);
                default: return Matrix3x2.CreateRotation((float)(normalized * Math.PI / 180));
            }
        }

        private static void ValidateFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
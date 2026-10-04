using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;

namespace WzComparerR2.Avatar
{
    public sealed class AvatarRenderer
    {
        public BitmapOrigin Render(AvatarLayout layout)
        {
            if (layout == null)
            {
                return new BitmapOrigin();
            }

            var groups = new Dictionary<int, AvatarLayoutTransformGroup>();
            foreach (AvatarLayoutTransformGroup group in layout.TransformGroups)
                groups.Add(group.Id, group);

            var layers = new List<RenderLayer>();
            Rectangle bounds = Rectangle.Empty;
            foreach (AvatarLayoutLayer layer in layout.Layers)
            {
                if (!layer.Visible || layer.GetBounds().IsEmpty)
                    continue;

                AvatarRenderTransform transform = AvatarRenderTransform.Identity;
                if (layer.TransformGroupId is int groupId)
                {
                    if (!groups.TryGetValue(groupId, out AvatarLayoutTransformGroup group))
                        throw new InvalidOperationException("Avatar transform group not found: " + groupId);
                    transform = group.Transform;
                }

                int rotation = AvatarRenderTransform.GetRightAngleRotation(transform.RotationDegrees);
                if (rotation != transform.RotationDegrees)
                {
                    transform = new AvatarRenderTransform(rotation, transform.RotationPivot, transform.Translation,
                        transform.FlipX, transform.FlipAxisX);
                }
                Rectangle transformedBounds = TransformBounds(layer.GetBounds(), transform.Matrix);
                var flip = (RotateFlipType)((rotation / 90) + (transform.FlipX ? 4 : 0));
                layers.Add(new RenderLayer(layer, transformedBounds, flip));
                bounds = bounds.IsEmpty ? transformedBounds : UnionBounds(bounds, transformedBounds);
            }

            if (bounds.IsEmpty)
            {
                return new BitmapOrigin();
            }

            var origin = new Point(checked(-bounds.X), checked(-bounds.Y));
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    graphics.Clear(Color.Transparent);

                    foreach (RenderLayer layer in layers)
                    {
                        // Decoded source bitmaps are temporary; the caller owns the WZ resources.
                        using (Bitmap sourceBitmap = layer.Layer.Source.Canvas.ExtractPng())
                        {
                            if (sourceBitmap == null)
                            {
                                continue;
                            }

                            var rotation = (RotateFlipType)((int)layer.Flip & 3);
                            if (rotation != RotateFlipType.RotateNoneFlipNone)
                                sourceBitmap.RotateFlip(rotation);
                            if (((int)layer.Flip & 4) != 0)
                                sourceBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);

                            graphics.DrawImageUnscaled(
                                sourceBitmap,
                                layer.Bounds.X - bounds.X,
                                layer.Bounds.Y - bounds.Y);
                        }
                    }
                }

                // Transfer the output bitmap to the caller only after rendering succeeds.
                return new BitmapOrigin(bitmap, origin);
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        private static Rectangle TransformBounds(Rectangle bounds, Matrix3x2 matrix)
        {
            Point topLeft = TransformCorner(bounds.X, bounds.Y, matrix);
            Point topRight = TransformCorner(bounds.Right, bounds.Y, matrix);
            Point bottomRight = TransformCorner(bounds.Right, bounds.Bottom, matrix);
            Point bottomLeft = TransformCorner(bounds.X, bounds.Bottom, matrix);
            int left = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomRight.X, bottomLeft.X));
            int top = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomRight.Y, bottomLeft.Y));
            int maxX = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomRight.X, bottomLeft.X));
            int maxY = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomRight.Y, bottomLeft.Y));
            return new Rectangle(left, top, maxX - left, maxY - top);
        }

        private static Point TransformCorner(double horizontal, double vertical, Matrix3x2 matrix)
        {
            return new Point(ToPixel(horizontal * matrix.M11 + vertical * matrix.M21 + matrix.M31),
                ToPixel(horizontal * matrix.M12 + vertical * matrix.M22 + matrix.M32));
        }

        private static int ToPixel(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value != Math.Truncate(value))
                throw new NotSupportedException("Avatar rendering requires pixel-aligned transformed coordinates.");
            return (int)value;
        }

        private static Rectangle UnionBounds(Rectangle first, Rectangle second)
        {
            int left = Math.Min(first.X, second.X);
            int top = Math.Min(first.Y, second.Y);
            int right = Math.Max(first.X + first.Width, second.X + second.Width);
            int bottom = Math.Max(first.Y + first.Height, second.Y + second.Height);
            return new Rectangle(left, top, right - left, bottom - top);
        }

        private sealed class RenderLayer
        {
            public RenderLayer(AvatarLayoutLayer layer, Rectangle bounds, RotateFlipType flip)
            {
                this.Layer = layer;
                this.Bounds = bounds;
                this.Flip = flip;
            }

            public AvatarLayoutLayer Layer { get; }
            public Rectangle Bounds { get; }
            public RotateFlipType Flip { get; }
        }
    }
}

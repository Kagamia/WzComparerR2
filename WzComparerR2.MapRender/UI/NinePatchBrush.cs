using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Renderers;
using Microsoft.Xna.Framework;

namespace WzComparerR2.MapRender.UI
{
    class NinePatchBrush : Brush
    {
        public NinePatchBrush()
        {
        }

        public INinePatchResource<TextureBase> Resource
        {
            get { return (INinePatchResource<TextureBase>)GetValue(ResourceProperty); }
            set { SetValue(ResourceProperty, value); }
        }

        public override void Draw(TextureBase texture, Renderer renderer, double elapsedGameTime, PointF position, Size renderSize, float opacity)
        {
            var blocks = UIGraphics.LayoutNinePatch(this.Resource, new Point((int)renderSize.Width, (int)renderSize.Height));

            foreach (var block in blocks)
            {
                if (block.Texture != null && block.Rectangle.Width > 0 && block.Rectangle.Height > 0)
                {
                    PointF pos = new PointF(block.Rectangle.X + position.X, block.Rectangle.Y + position.Y);
                    Size size = new Size(block.Rectangle.Width, block.Rectangle.Height);
                    ColorW color = new ColorW(1f, 1f, 1f, this.Opacity);
                    renderer.Draw(block.Texture, pos, size, color, false);
                }
            }
        }

        public override void DrawGeometry(GeometryBuffer buffer, TextureBase texture, Renderer renderer, double elapsedGameTime, PointF position, float opacity)
        {
            throw new NotImplementedException();
        }

        public static readonly DependencyProperty ResourceProperty = DependencyProperty.Register("Resource", typeof(INinePatchResource<TextureBase>), typeof(NinePatchBrush), new FrameworkPropertyMetadata(null));
    }
}

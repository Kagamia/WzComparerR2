using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Renderers;
using Microsoft.Xna.Framework;
using TextureBlock = WzComparerR2.MapRender.UI.UIGraphics.RenderBlock<EmptyKeys.UserInterface.Media.TextureBase>;

namespace WzComparerR2.MapRender.UI
{
    class MessageBoxBackgroundBrush : Brush
    {
        public static readonly DependencyProperty ResourceProperty = DependencyProperty.Register("Resource", typeof(MessageBoxBackgroundResource), typeof(MessageBoxBackgroundBrush), new FrameworkPropertyMetadata(null));

        public MessageBoxBackgroundBrush()
        {

        }

        public MessageBoxBackgroundResource Resource
        {
            get { return (MessageBoxBackgroundResource)this.GetValue(ResourceProperty); }
            set { this.SetValue(ResourceProperty, value); }
        }

        public override void Draw(TextureBase texture, Renderer renderer, double elapsedGameTime, PointF position, Size renderSize, float opacity)
        {
            if (this.Resource == null)
            {
                return;
            }

            IList<TextureBlock> blocks;
            var boxOffset = this.GetBoxOffset();

            if (boxOffset >= 0)
            {
                blocks = this.Layout(this.Resource, new Point((int)renderSize.Width, (int)renderSize.Height), (int)boxOffset);
            }
            else //fallback
            {
                blocks = UIGraphics.LayoutTCB(this.Resource, new Point((int)renderSize.Width, (int)renderSize.Height));
            }

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

        private float GetBoxOffset()
        {
            Grid grid = this.Parent as Grid;
            if (grid != null)
            {
                ContentPresenter pnlContent = VisualTreeHelper.Instance.FindElementByName(grid, "PART_WindowContent") as ContentPresenter;
                if (pnlContent != null)
                {
                    var grid2 = pnlContent.Content as Grid;
                    if (grid2 != null)
                    {
                        StackPanel pnlButtons = grid2.Children.OfType<StackPanel>().FirstOrDefault();
                        if (pnlButtons != null)
                        {
                            return pnlButtons.VisualPosition.Y - grid.VisualPosition.Y;
                        }
                    }
                }
            }
            return float.NaN;
        }

        private IList<TextureBlock> Layout(MessageBoxBackgroundResource res, Point size, int boxOffset)
        {
            var blocks = new List<TextureBlock>(6);
            Point t = res.GetSize(res.T);
            Point c = res.GetSize(res.C);
            Point cBox = res.GetSize(res.C_Box);
            Point box = res.GetSize(res.Box);
            Point sBox = res.GetSize(res.S_Box);

            //计算框线
            int[] y = new int[6] {
                0,
                t.Y,
                boxOffset - cBox.Y,
                boxOffset,
                size.Y - sBox.Y,
                size.Y
            };

            //绘制上
            blocks.Add(new TextureBlock(res.T, new Rectangle(0, y[0], size.X, y[1] - y[0])));
            //绘制中
            blocks.Add(new TextureBlock(res.C, new Rectangle(0, y[1], size.X, y[2] - y[1])));
            //绘制box
            blocks.Add(new TextureBlock(res.C_Box, new Rectangle(0, y[2], size.X, y[3] - y[2])));
            blocks.Add(new TextureBlock(res.Box, new Rectangle(0, y[3], size.X, y[4] - y[3])));
            blocks.Add(new TextureBlock(res.S_Box, new Rectangle(0, y[4], size.X, y[5] - y[4])));

            return blocks;
        }

        public override void DrawGeometry(GeometryBuffer buffer, TextureBase texture, Renderer renderer, double elapsedGameTime, PointF position, float opacity)
        {
            throw new NotImplementedException();
        }
    }

    class MessageBoxBackgroundResource : INinePatchResource<TextureBase>
    {
        public TextureBase T { get; set; }
        public TextureBase C { get; set; }
        public TextureBase C_Box { get; set; }
        public TextureBase Box { get; set; }
        public TextureBase S_Box { get; set; }
        public TextureBase S { get; set; }

        TextureBase INinePatchResource<TextureBase>.NW { get { return null; } }
        TextureBase INinePatchResource<TextureBase>.N { get { return this.T; } }
        TextureBase INinePatchResource<TextureBase>.NE { get { return null; } }
        TextureBase INinePatchResource<TextureBase>.W { get { return null; } }
        TextureBase INinePatchResource<TextureBase>.C { get { return this.C; } }
        TextureBase INinePatchResource<TextureBase>.E { get { return null; } }
        TextureBase INinePatchResource<TextureBase>.SW { get { return null; } }
        TextureBase INinePatchResource<TextureBase>.S { get { return this.S; } }
        TextureBase INinePatchResource<TextureBase>.SE { get { return null; } }

        public Point GetSize(TextureBase texture)
        {
            return new Point(texture.Width, texture.Height);
        }
    }
}

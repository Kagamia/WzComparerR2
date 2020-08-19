using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Renderers;
using EmptyKeys.UserInterface.Media.Imaging;
using Color = Microsoft.Xna.Framework.Color;

namespace WzComparerR2.MapRender.UI
{
    class UIDanmaku : WindowEx
    {
        public UIDanmaku()
        {
            this.Bullets = new BulletCollection(this);
        }

        public BulletCollection Bullets { get; private set; }

        private Canvas danmakuContainer;
        private List<List<TextBlock>> bulletFlow = new List<List<TextBlock>>();

        protected override void InitializeComponents()
        {
            this.IsHitTestVisible = false;

            Canvas canvas = new Canvas();
            canvas.SetBinding(Canvas.WidthProperty, new Binding(Window.WidthProperty) { Source = this });
            canvas.SetBinding(Canvas.HeightProperty, new Binding(Window.HeightProperty) { Source = this });
            canvas.LayoutUpdated += Canvas_LayoutUpdated;
            canvas.FontFamily = new FontFamily("微软雅黑");
            canvas.FontSize = 24;
            canvas.FontStyle = FontStyle.Regular;

            this.Content = canvas;
            this.danmakuContainer = canvas;

            FontManager.Instance.AddFont(canvas.FontFamily.Source, canvas.FontSize, canvas.FontStyle);
            base.InitializeComponents();
        }

        protected override void OnDraw(Renderer spriterenderer, double elapsedGameTime, float opacity)
        {
            base.OnDraw(spriterenderer, elapsedGameTime, opacity);
            this.Update(TimeSpan.FromMilliseconds(elapsedGameTime));
        }

        private void Canvas_LayoutUpdated(object sender, EventArgs e)
        {
            
        }

        private void Update(TimeSpan elapsed)
        {
            List<TextBlock> preRemoved = null;

            foreach (TextBlock textBlock in this.danmakuContainer.Children)
            {
                if (textBlock.ActualWidth > 0)
                {
                    float left = Canvas.GetLeft(textBlock);
                    if (left + textBlock.ActualWidth <= 0)
                    {
                        (preRemoved ?? (preRemoved = new List<TextBlock>())).Add(textBlock);
                    }
                    else
                    {
                        float speed = (this.Width + textBlock.ActualWidth) / 5;
                        left -= (float)(speed * elapsed.TotalSeconds);
                        Canvas.SetLeft(textBlock, left);
                    }
                }
            }

            if (preRemoved != null)
            {
                foreach (var textBlock in preRemoved)
                {
                    var bullet = textBlock.DataContext as Bullet;
                    this.danmakuContainer.Children.Remove(textBlock);
                    this.Bullets.Remove(bullet);
                }
            }
        }

        private void AddBullet(Bullet bullet)
        {
            var textBlock = new TextBlock()
            {
                Foreground = new SolidColorBrush(new ColorW(bullet.ForeColor.PackedValue)),
                Text = bullet.Text
            };
           
            this.danmakuContainer.Children.Add(textBlock);
            //寻找位置
            int line = 0;
            for (; line < this.bulletFlow.Count; line++)
            {
                var row = this.bulletFlow[line];
                if (row.Count <= 0)
                {
                    break;
                }
                var tb = row[row.Count - 1];
                //这里应该计算成剩余时间 暂时偷懒。
                if (tb.ActualWidth > 0 && Canvas.GetLeft(tb) + tb.ActualWidth + 50 < this.Width)
                {
                    break;
                }
            }
            while (line >= this.bulletFlow.Count)
            {
                this.bulletFlow.Add(new List<TextBlock>());
            }
            this.bulletFlow[line].Add(textBlock);

            Canvas.SetLeft(textBlock, this.Width);
            var font = FontManager.Instance.GetFont(this.danmakuContainer.FontFamily.Source,
                this.danmakuContainer.FontSize,
                this.danmakuContainer.FontStyle);
            Canvas.SetTop(textBlock, line * font.LineSpacing);
        }

        private void RemoveBullet(Bullet bullet)
        {
            var textBlock = this.danmakuContainer.Children.OfType<TextBlock>()
                .FirstOrDefault(text => text.Tag == bullet);
            if (textBlock != null)
            {
                this.danmakuContainer.Children.Remove(textBlock);
            }
        }

        public class Bullet
        {
            public Bullet() : this(null)
            {
            }

            public Bullet(string text) : this(text, Color.White)
            {
            }

            public Bullet(string text, Color foreColor)
            {
                this.Text = text;
                this.ForeColor = foreColor;
            }

            public string Text { get; set; }
            public Color ForeColor { get; set; }
            internal int Line { get; set; }
        }

        public class BulletCollection : Collection<Bullet>
        {
            public BulletCollection(UIDanmaku owner)
            {
                this.owner = owner;
            }

            private readonly UIDanmaku owner;

            protected override void InsertItem(int index, Bullet item)
            {
                base.InsertItem(index, item);
                this.owner.AddBullet(item);
            }

            protected override void SetItem(int index, Bullet item)
            {
                throw new NotSupportedException();
            }

            protected override void RemoveItem(int index)
            {
                Bullet item = this[index];
                base.RemoveItem(index);
                this.owner.RemoveBullet(item);
            }

            protected override void ClearItems()
            {
                base.ClearItems();
            }
        }
    }
}

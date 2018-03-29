using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Renderers;
using EmptyKeys.UserInterface.Media.Imaging;

namespace WzComparerR2.MapRender.UI
{
    class UITopBar : WindowEx
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(UITopBar), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty IsShortModeProperty = DependencyProperty.Register("IsShortMode", typeof(bool), typeof(UITopBar), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty PaddingLeftProperty = DependencyProperty.Register("PaddingLeft", typeof(float), typeof(UITopBar), new FrameworkPropertyMetadata(0));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public bool IsShortMode
        {
            get { return (bool)GetValue(IsShortModeProperty); }
            set { SetValue(TextProperty, IsShortModeProperty); }
        }

        public float PaddingLeft
        {
            get { return (float)GetValue(PaddingLeftProperty); }
            set { SetValue(TextProperty, PaddingLeftProperty); }
        }

        protected override void InitializeComponents()
        {
            Border border = new Border();
            border.Background = new SolidColorBrush(new ColorW(0, 0, 0, 128));
            this.Content = border;

            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            border.Child = panel;

            Border blank = new Border();
            blank.SetBinding(Border.VisibilityProperty, new Binding(IsShortModeProperty) { Source = this, Converter = UIHelper.CreateConverter((bool o) => o ? Visibility.Visible : Visibility.Collapsed) });
            blank.SetBinding(Border.WidthProperty, new Binding(PaddingLeftProperty) { Source = this });
            blank.SetBinding(Border.HeightProperty, new Binding(HeightProperty) { Source = this });
            panel.Children.Add(blank);

            TextBlock textBlock = new TextBlock();
            textBlock.Foreground = Brushes.Cyan;
            textBlock.Margin = new Thickness(6, 0, 0, 0);
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;

            textBlock.SetBinding(TextBlock.TextProperty, new Binding(TextProperty) { Source = this });
            panel.Children.Add(textBlock);

            this.Height = 16;
            this.IsHitTestVisible = false;
            base.InitializeComponents();
        }
    }
}

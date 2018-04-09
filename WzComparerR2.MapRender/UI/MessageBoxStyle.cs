using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Themes;

namespace WzComparerR2.MapRender.UI
{
    static class MessageBoxStyle
    {
        public static Style CreateMessageBoxStyle()
        {
            Style style = new Style(typeof(Window));
            style.Setters.Add(new Setter(Control.TemplateProperty, CreateMessageBoxControlTemplate()));
            return style;
        }

        public static ControlTemplate CreateMessageBoxControlTemplate()
        {
            ControlTemplate template = new ControlTemplate(typeof(Window), CreateMessageBoxControlFunc);
            return template;
        }

        private static UIElement CreateMessageBoxControlFunc(UIElement parent)
        {
            Grid screenBg = new Grid();
            screenBg.HorizontalAlignment = HorizontalAlignment.Center;
            screenBg.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            screenBg.SetBinding(Control.BackgroundProperty, new Binding(Control.BackgroundProperty) { Source = parent });
            screenBg.Parent = parent;
            screenBg.Foreground = EmptyKeys.UserInterface.Media.Brushes.White;

            Grid grid = new Grid();
            grid.Width = 260;
            grid.SetResourceReference(Control.BackgroundProperty, MapRenderResourceKey.MessageBoxBackgroundBrush);
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            screenBg.Children.Add(grid);
            Grid.SetColumn(grid, 0);

            Border border = new Border();
            grid.Children.Add(border);
            Grid.SetRow(border, 0);

            ContentPresenter pnlTitle = CommonHelpers.CreateContentPresenter(parent, "Title");
            pnlTitle.Height = 26;
            pnlTitle.Margin = new Thickness(0, 4, 0, 0);
            pnlTitle.HorizontalAlignment = HorizontalAlignment.Center;
            pnlTitle.VerticalAlignment = VerticalAlignment.Center;
            pnlTitle.IsHitTestVisible = false;
            pnlTitle.Name = "PART_WindowTitle";
            border.Child = pnlTitle;

            ContentPresenter pnlContent = CommonHelpers.CreateContentPresenter(parent, "Content");
            pnlContent.Margin = new Thickness(20, 8, 20, 0);
            pnlContent.Name = "PART_WindowContent";
            grid.Children.Add(pnlContent);
            Grid.SetRow(pnlContent, 1);

            Window msgBox = parent as Window;
            if (msgBox != null)
            {
                msgBox.Visible += MsgBox_Visible;
                msgBox.SizeChanged += MsgBox_SizeChanged;
                var style = MapRenderButtonStyle.CreateMapRenderButtonStyle();
                style.TargetType = typeof(Button);
                msgBox.Resources[typeof(Button)] = style;
            }
            return screenBg;
        }

        private static void MsgBox_Visible(object sender, RoutedEventArgs e)
        {
            var wnd = sender as Window;
            if (wnd != null)
            {
                var grid = wnd.Content as Grid;
                if (grid != null)
                {
                    var textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
                    if (textBlock != null)
                    {
                        textBlock.Margin = new Thickness(0, 0, 0, 8);
                        if (textBlock.Text != null && textBlock.Font != null)
                        {
                            textBlock.TextWrapping = TextWrapping.NoWrap;
                            var size = textBlock.Font.MeasureString(textBlock.Text, new Size(220, 0));
                            if (size.Height > textBlock.ActualHeight)
                            {
                                textBlock.Height = size.Height;
                            }
                        }
                    }
                    var stackPanel = grid.Children.OfType<StackPanel>().FirstOrDefault();
                    if (stackPanel != null)
                    {
                        stackPanel.HorizontalAlignment = HorizontalAlignment.Right;
                        foreach (var btn in stackPanel.Children.OfType<Button>())
                        {
                            btn.Margin = new Thickness(2, 0, 0, 6);
                        }
                    }
                }
                SetWindowAlignCenter(wnd);
            }
        }

        private static void TextBlock_SizeChanged(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBlock;
            if (tb != null && tb.Text != null && tb.ActualWidth > 0)
            {
                var size = tb.Font.MeasureString(tb.Text, new Size(tb.ActualWidth, tb.ActualHeight));
                if (size.Height > tb.ActualHeight)
                {
                    tb.Height = size.Height;
                }
            }
        }

        private static void MsgBox_SizeChanged(object sender, RoutedEventArgs e)
        {
            SetWindowAlignCenter(sender as Window);
        }

        private static void SetWindowAlignCenter(Window wnd)
        {
            if (wnd != null && wnd.DesiredSize.Height > 0)
            {
                wnd.Top = (int)(Engine.Instance.Renderer.GetViewport().Height - wnd.DesiredSize.Height) / 2;
            }
        }
    }
}

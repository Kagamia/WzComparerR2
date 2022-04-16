using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Xna.Framework;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Input;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Controls.Primitives;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Themes;
using EmptyKeys.UserInterface.Media.Imaging;
using MRes = WzComparerR2.MapRender.Properties.Resources;

namespace WzComparerR2.MapRender.UI
{
    class UIChatBox : WindowEx
    {
        public UIChatBox()
        {

        }

        public TextBoxEx TextBoxChat { get; private set; }
        private ScrollViewer scrollView;
        private StackPanel pnlMessage;
        private bool isResizing;
        private PointF startPoint;

        private static readonly string Part_Resize = "UIChatBox_ResizeBorder";

        protected override void InitializeComponents()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1f, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(28) });
            grid.SetBinding(Canvas.WidthProperty, new Binding(UIChatBox.WidthProperty) { Source = this });
            grid.SetBinding(Canvas.HeightProperty, new Binding(UIChatBox.HeightProperty) { Source = this });
            this.Content = grid;

            var border1 = new Border();
            border1.Name = Part_Resize;
            border1.IsHitTestVisible = true;
            border1.Background = new TCBBrush() { Resource = GetBackgroundResource() };
            Grid.SetRow(border1, 0);
            grid.Children.Add(border1);

            var stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;
            stackPanel.Margin = new Thickness(0);
            this.pnlMessage = stackPanel;

            var scrollViewer = new ScrollViewer();
            scrollViewer.Style = CreateScrollViewerStyle();
            scrollViewer.Margin = new Thickness(8, 10, 8, 4);
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scrollViewer.Content = stackPanel;
            border1.Child = scrollViewer;
            this.scrollView = scrollViewer;

            var border2 = new Border();
            border2.Background = new ImageBrush() { ImageSource = new BitmapImage() { TextureAsset = nameof(MRes.StatusBar3_img_chat_ingame_input_layer_backgrnd) } };
            Grid.SetRow(border2, 1);
            grid.Children.Add(border2);

            var textBox = new TextBoxEx();
            textBox.Background = new ImageBrush() { ImageSource = new BitmapImage() { TextureAsset = nameof(MRes.StatusBar3_img_chat_ingame_input_layer_chatEnter) } };
            textBox.SelectionBrush = Brushes.Blue;
            textBox.CaretBrush = Brushes.White;
            textBox.IMEEnabled = true;
            textBox.IsTabStop = false;
            textBox.BorderThickness = new Thickness(0);
            textBox.Width = 471;
            textBox.Height = 20;
            border2.Child = textBox;
            this.TextBoxChat = textBox;

            this.Width = 574;
            this.Height = 80;
            this.MinHeight = 80;
            ImageManager.Instance.AddImage(nameof(MRes.StatusBar3_img_chat_ingame_input_layer_backgrnd));
            ImageManager.Instance.AddImage(nameof(MRes.StatusBar3_img_chat_ingame_input_layer_chatEnter));
            base.InitializeComponents();
        }

        public void AppendTextNormal(string msgText)
        {
            this.AppendMessage(msgText, Color.White, Color.Transparent);
        }

        public void AppendTextSystem(string msgText)
        {
            this.AppendMessage(msgText, new Color(255, 170, 170), Color.Transparent);
        }

        public void AppendTextInfo(string msgText)
        {
            this.AppendMessage(msgText, new Color(187, 187, 187), Color.Transparent);
        }

        public void AppendTextHelp(string msgText)
        {
            this.AppendMessage(msgText, new Color(255, 255, 0), Color.Transparent);
        }

        public void AppendMessage(string msgText, Color foreColor, Color backColor)
        {
            var border = new Border();
            border.Background = new SolidColorBrush(new ColorW(backColor.PackedValue));

            var textBlock = new TextBlock();
            textBlock.Text = msgText;
            textBlock.Foreground = new SolidColorBrush(new ColorW(foreColor.PackedValue));
            textBlock.Margin = new Thickness(0, 1, 0, 1);
            border.Child = textBlock;

            this.pnlMessage.Children.Add(border);

            if (textBlock.Font != null)
            {
                UIElement scrollBar = VisualTreeHelper.Instance.FindElementByName(this.scrollView, "PART_VerticalScrollBar");
                var desiredSize = new Size(this.scrollView.ActualWidth - scrollBar.ActualWidth, 0);
                var textSize = textBlock.Font.MeasureString(msgText, desiredSize);
                if (textSize.Width > 0)
                {
                    textBlock.Width = Math.Max(desiredSize.Width, textSize.Width);
                }
                if (textSize.Height > 0)
                {
                    textBlock.Height = textSize.Height;
                }
            }

            while (this.pnlMessage.Children.Count > 500)
            {
                this.pnlMessage.Children.RemoveAt(0);
            }

            if (this.scrollView.ExtentHeight - (this.scrollView.VerticalOffset + this.scrollView.ActualHeight) < 20)
            {
                this.scrollView.ScrollToBottom();
            }
        }

        protected override void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == EmptyKeys.UserInterface.Input.MouseButton.Left)
            {
                UIElement elem = e.Source as UIElement;
                if (elem != null && elem.Name == Part_Resize && !isResizing)
                {
                    var point = e.GetPosition(this);
                    if (point.Y < 8) //top
                    {
                        isResizing = true;
                        this.startPoint = e.GetPosition();
                        elem.CaptureMouse();
                        this.Focus();
                        e.Handled = true;
                        return;
                    }
                }
            }
            base.OnPreviewMouseDown(sender, e);
        }

        protected override void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (this.isResizing)
            {
                var elem = e.Source as UIElement;
                if (elem != null)
                {
                    var point = e.GetPosition();
                    var dx = point.X - this.startPoint.X;
                    var dy = point.Y - this.startPoint.Y;
                    var vp = Engine.Instance.Renderer.GetViewport();
                    //resize-top
                    if (dy != 0)
                    {
                        var height = this.Height - dy;
                        if (height < this.MinHeight)
                        {
                            height = this.MinHeight;
                        }
                        if (height > this.MaxHeight)
                        {
                            height = this.MaxHeight;
                        }
                        if (height > vp.Height)
                        {
                            height = vp.Height;
                        }
                        dy = this.Height - height;
                        if (dy != 0)
                        {
                            this.Top += dy;
                            this.Height = height;
                            this.startPoint.Y += dy;
                            VisualTreeHelper.Instance.InvalidateMeasure(this);
                        }
                    }
                }
                e.Handled = true;
            }
            base.OnPreviewMouseMove(sender, e);
        }

        protected override void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isResizing)
            {
                isResizing = false;
                e.Handled = true;
                var elem = (e.Source as UIElement);
                if (elem != null)
                {
                    elem.ReleaseMouseCapture();
                }
            }
            base.OnPreviewMouseUp(sender, e);
        }

        private INinePatchResource<TextureBase> GetBackgroundResource()
        {
            var assetManager = Engine.Instance.AssetManager;

            return new EKNineFormResource()
            {
                N = assetManager.LoadTexture(null, nameof(MRes.StatusBar3_img_chat_ingame_view_max_top)),
                C = assetManager.LoadTexture(null, nameof(MRes.StatusBar3_img_chat_ingame_view_max_center)),
                S = assetManager.LoadTexture(null, nameof(MRes.StatusBar3_img_chat_ingame_view_max_bottom)),
            };
        }

        private Style CreateScrollViewerStyle()
        {
            var style = ScrollViewerStyle.CreateScrollViewerStyle();
            var templateSetter = style.Setters.FirstOrDefault(s => s.Property == Control.TemplateProperty);
            if (templateSetter != null)
            {
                var oldTemplate = templateSetter.Value as ControlTemplate;
                var funcType = typeof(Func<UIElement, UIElement>);
                var funcField = oldTemplate.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(field => field.FieldType == funcType);
                var oldMethod = funcField?.GetValue(oldTemplate) as Func<UIElement, UIElement>;
                if (oldMethod != null)
                {
                    var newMethod = new Func<UIElement, UIElement>(parent =>
                    {
                        UIElement elem = oldMethod(parent);
                        ScrollBar scrollBar = VisualTreeHelper.Instance.FindElementByName(elem, "PART_VerticalScrollBar") as ScrollBar;
                        if (scrollBar != null)
                        {
                            scrollBar.Width = 12;
                            scrollBar.MaxWidth = 12;
                            scrollBar.MinWidth = 12;
                        }
                        scrollBar = VisualTreeHelper.Instance.FindElementByName(elem, "PART_HorizontalScrollBar") as ScrollBar;
                        if (scrollBar != null)
                        {
                            scrollBar.Height = 50;
                        }
                        return elem;
                    });
                    funcField.SetValue(oldTemplate, newMethod);
                }
            }
            return style;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TextRenderer = System.Windows.Forms.TextRenderer;

using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Renderers;
using EmptyKeys.UserInterface.Media.Imaging;
using EmptyKeys.UserInterface.Input;

using WzComparerR2.WzLib;
using WzComparerR2.Common;

using Res = CharaSimResource.Resource;
using MRes = WzComparerR2.MapRender.Properties.Resources;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace WzComparerR2.MapRender.UI
{
    class UITeleport : WindowEx // base code from UIWorldmap.cs
    {
        public static readonly DependencyProperty SelectedMapIDProperty = DependencyProperty.Register("SelectedMapID", typeof(int?), typeof(UITeleport), new FrameworkPropertyMetadata(null));

        public UITeleport() : base()
        {
        }

        public ComboBox CmbMaps { get; private set; }
        public TextBlock TextStreet { get; private set; }
        public TextBlock TextMap { get; private set; }
        //public TextBlock TextMapID { get; private set; }
        public TextBlockTooltip Tooltip { get; private set; }
        public StringLinker Sl { get; set; }

        public event EventHandler<SelectedMapGoEventArgs> SelectedMapGo;

        public int? SelectedMapID
        {
            get { return (int?)this.GetValue(SelectedMapIDProperty); }
            set { this.SetValue(SelectedMapIDProperty, value); }
        }

        protected override void InitializeComponents()
        {
            var canvas = new Canvas();
            var canvasBackTexture = Engine.Instance.AssetManager.LoadTexture(null, nameof(MRes.UIWindow2_img_Teleport_customBG));
            canvas.Background = new ImageBrush() { ImageSource = new BitmapImage() { Texture = canvasBackTexture }, Stretch = Stretch.None };
            canvas.SetBinding(Canvas.WidthProperty, new Binding(UITeleport.WidthProperty) { Source = this, Mode = BindingMode.TwoWay });
            canvas.SetBinding(Canvas.HeightProperty, new Binding(UITeleport.HeightProperty) { Source = this, Mode = BindingMode.TwoWay });
            canvas.Parent = this;
            this.Content = canvas;

            Border title = new Border();
            title.Height = 24;
            title.SetBinding(Border.WidthProperty, new Binding(Border.WidthProperty) { Source = canvas });
            canvas.Children.Add(title);
            this.SetDragTarget(title);

            TextBlock textStreet = new TextBlock();
            textStreet.MaxWidth = 134 - 4 * 1;
            textStreet.MaxHeight = 12;
            textStreet.Background = Brushes.Transparent;
            textStreet.Foreground = Brushes.Black;
            textStreet.Margin = new Thickness(4, 3, 0, -3);
            Canvas.SetLeft(textStreet, 13);
            Canvas.SetTop(textStreet, 78 + 18 * 0); // 1행 배치
            canvas.Children.Add(textStreet);
            this.TextStreet = textStreet;

            TextBlock TextMap = new TextBlock();
            TextMap.MaxWidth = 134 - 4 * 1;
            TextMap.MaxHeight = 12;
            TextMap.Background = Brushes.Transparent;
            TextMap.Foreground = Brushes.Black;
            TextMap.Margin = new Thickness(4, 3, 0, -3);
            Canvas.SetLeft(TextMap, 13);
            Canvas.SetTop(TextMap, 78 + 18 * 1); // 2행 배치
            canvas.Children.Add(TextMap);
            this.TextMap = TextMap;

            ComboBox cmbMaps = new ComboBox();
            cmbMaps.Width = 134;
            cmbMaps.Height = 17 + 1 * 2;
            Canvas.SetLeft(cmbMaps, 13);
            Canvas.SetTop(cmbMaps, 78 + 18 * 2 - 1); // 3행 배치
            cmbMaps.SetBinding(ComboBox.SelectedItemProperty, new Binding(UITeleport.SelectedMapIDProperty) { Source = this, Mode = BindingMode.TwoWay });
            canvas.Children.Add(cmbMaps);
            this.CmbMaps = cmbMaps;

            /* 맵ID; 미사용
            TextBlock TextMapID = new TextBlock();
            TextMapID.Width = 134;
            TextMapID.MaxHeight = 17;
            TextMap.Background = Brushes.Transparent;
            TextMapID.Foreground = Brushes.Black;
            TextMap.Margin = new Thickness(4, 3, 0, -3);
            Canvas.SetLeft(TextMapID, 13);
            Canvas.SetTop(TextMapID, 78 + 18 * 3); // 4행 배치
            canvas.Children.Add(TextMapID);
            this.TextMapID = TextMapID;*/

            TextBlockTooltip tooltip = new TextBlockTooltip();
            tooltip.Width = 134;
            tooltip.Height = 17 + 18 * 1; // 툴팁영역 2칸 (street + map)
            Canvas.SetLeft(tooltip, 13);
            Canvas.SetTop(tooltip, 78 + 18 * 0);
            canvas.Children.Add(tooltip);
            this.Tooltip = tooltip;

            Button btnBack = new ImageButton();
            btnBack.Name = "Cancel";
            btnBack.Click += BtnBack_Click;
            btnBack.SetResourceReference(UIElement.StyleProperty, MapRenderResourceKey.MapRenderButtonStyle);
            btnBack.Focusable = false;
            Canvas.SetLeft(btnBack, 106);
            Canvas.SetTop(btnBack, 149);
            canvas.Children.Add(btnBack);

            ImageButton btnGo = new ImageButton();
            btnGo.Name = "OK";
            btnGo.Click += BtnGo_Click;
            btnGo.SetResourceReference(UIElement.StyleProperty, MapRenderResourceKey.MapRenderButtonStyle);
            btnGo.Focusable = false;
            Canvas.SetLeft(btnGo, 58);
            Canvas.SetTop(btnGo, 149);
            canvas.Children.Add(btnGo);

            ImageButton btnClose = new ImageButton();
            btnClose.Name = "Close";
            btnClose.Click += BtnClose_Click;
            btnClose.SetResourceReference(UIElement.StyleProperty, MapRenderResourceKey.MapRenderButtonStyle);
            btnClose.Focusable = false;
            Canvas.SetRight(btnClose, 7);
            Canvas.SetTop(btnClose, 5);
            canvas.Children.Add(btnClose);

            this.Width = canvasBackTexture.Width;
            this.Height = canvasBackTexture.Height;
            base.InitializeComponents();
        }

        protected override void OnPropertyChanged(DependencyProperty property)
        {
            base.OnPropertyChanged(property);

            if (property == SelectedMapIDProperty) // 콤보박스 선택 시
            {
                if (SelectedMapID != null)
                {
                    //TextMapID.Text = SelectedMapID.ToString();
                    StringResult sr = null;
                    Sl?.StringMap.TryGetValue(SelectedMapID ?? 999999999, out sr);
                    var streetSTR = sr?["streetName"] ?? "(null)";
                    var mapSTR = sr?["mapName"] ?? "(null)";
                    TextStreet.Text = GetStringResult(streetSTR);
                    TextMap.Text = GetStringResult(mapSTR);
                    Tooltip.ResetTooltip(streetSTR, mapSTR);
                }
            }
        }

        private string GetStringResult(string str) // 주어진 칸 넘는 문자열 ..처리
        {
            if (str == null) return "(null)";
            var res = str;
            var num = 0;
            var textSize = TextStreet.Font.MeasureString(res, new Size(0, 0));
            while (textSize.Width > 134 - 4 * 1)
            {
                res = str.Substring(0, str.Length - num) + "..";
                num += 1;
                textSize = TextStreet.Font.MeasureString(res, new Size(0, 0));
            }
            return res;
        }
        
        public class TextBlockTooltip : Control, ITooltipTarget // 마우스오버시 툴팁 출력 영역
        {
            public TextBlockTooltip()
            {
                itemRectList = new List<ItemRect>();
                itemRectList.Add(new ItemRect() { Rect = new Rect(-1, -1, -1, -1), Tooltip = "" });
            }

            private List<ItemRect> itemRectList;

            public void ResetTooltip(string text1, string text2)
            {
                itemRectList.Clear();
                itemRectList.Add(new ItemRect() { Rect = new Rect(0, 0, 134, 17), Tooltip = text1 });
                itemRectList.Add(new ItemRect() { Rect = new Rect(0, 0 + 18 * 1, 134, 17), Tooltip = text2 });
            }

            public object GetTooltipTarget(PointF mouseLocation)
            {
                foreach (var itemRect in itemRectList)
                {
                    if (itemRect.Rect.Contains(mouseLocation))
                    {
                        return itemRect.Tooltip;
                    }
                }
                return null;
            }
            private struct ItemRect
            {
                public Rect Rect;
                public object Tooltip;
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void BtnGo_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            this.SelectedMapGo?.Invoke(this, new SelectedMapGoEventArgs(SelectedMapID ?? 999999999));
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        public class SelectedMapGoEventArgs : EventArgs
        {
            public SelectedMapGoEventArgs(int mapID)
            {
                this.MapID = mapID;
            }

            public int MapID { get; private set; }
        }
    }
}

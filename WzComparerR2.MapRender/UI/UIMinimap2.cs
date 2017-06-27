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

using Microsoft.Xna.Framework;
using WzComparerR2.WzLib;
using Res = CharaSimResource.Resource;
using MRes = WzComparerR2.MapRender.Properties.Resources;
using MathHelper = Microsoft.Xna.Framework.MathHelper;
using System.Globalization;

namespace WzComparerR2.MapRender.UI
{
    class UIMinimap2 : WindowEx
    {
        public static readonly DependencyProperty StreetNameProperty = DependencyProperty.Register("StreetName", typeof(string), typeof(UIMinimap2), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty MapNameProperty = DependencyProperty.Register("MapName", typeof(string), typeof(UIMinimap2), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty MapMarkProperty = DependencyProperty.Register("MapMark", typeof(TextureBase), typeof(UIMinimap2), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty MinimapCanvasProperty = DependencyProperty.Register("MinimapCanvas", typeof(TextureBase), typeof(UIMinimap2), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty MapRegionProperty = DependencyProperty.Register("MapRegion", typeof(Rect), typeof(UIMinimap2), new FrameworkPropertyMetadata(new Rect()));
        public static readonly DependencyProperty CameraCenterProperty = DependencyProperty.Register("CameraCenter", typeof(PointF), typeof(UIMinimap2), new FrameworkPropertyMetadata(new PointF()));
        public static readonly DependencyProperty IconsProperty = DependencyProperty.Register("Icons", typeof(List<MapIcon>), typeof(UIMinimap2), new FrameworkPropertyMetadata(null));

        public UIMinimap2()
        {
            this.Focusable = false;
            this.Icons = new List<MapIcon>();
        }

        public string StreetName
        {
            get { return (string)GetValue(StreetNameProperty); }
            set { SetValue(StreetNameProperty, value); }
        }

        public string MapName
        {
            get { return (string)GetValue(MapNameProperty); }
            set { SetValue(MapNameProperty, value); }
        }

        public TextureBase MapMark
        {
            get { return (TextureBase)GetValue(MapMarkProperty); }
            set { SetValue(MapMarkProperty, value); }
        }

        public TextureBase MinimapCanvas
        {
            get { return (TextureBase)GetValue(MinimapCanvasProperty); }
            set { SetValue(MinimapCanvasProperty, value); }
        }

        public Rect MapRegion
        {
            get { return (Rect)GetValue(MapRegionProperty); }
            set { SetValue(MapRegionProperty, value); }
        }

        public PointF CameraCenter
        {
            get { return (PointF)GetValue(CameraCenterProperty); }
            set { SetValue(CameraCenterProperty, value); }
        }

        public List<MapIcon> Icons
        {
            get { return (List<MapIcon>)GetValue(IconsProperty); }
            private set { SetValue(IconsProperty, value); }
        }

        public MapArea MapAreaControl { get; private set;}

        private UIMinimapResource resource;
       
        private TextBlock lblStreetName;
        private TextBlock lblMapName;

        protected override void InitializeComponents()
        {
            UIMinimapResource resource = new UIMinimapResource();
            this.MinWidth = resource.NW.Width + resource.NE.Width;
            this.MinHeight = resource.NW.Height + resource.SW.Height;
            this.Width = this.MinWidth;
            this.Height = this.MinHeight;
            this.MaxWidth = 300;
            this.MaxHeight = 300;

            Canvas canvas = new Canvas();
            canvas.Background = new NinePatchBrush() { Resource = resource };
            canvas.SetBinding(Canvas.WidthProperty, new Binding(UIMinimap2.WidthProperty) { Source = this });
            canvas.SetBinding(Canvas.HeightProperty, new Binding(UIMinimap2.HeightProperty) { Source = this });
            canvas.Parent = this;
            this.Content = canvas;

            Border title = new Border();
            title.Height = 17;
            title.SetBinding(Canvas.WidthProperty, new Binding(Canvas.WidthProperty) { Source = canvas });
            canvas.Children.Add(title);
            this.SetDragTarget(title);

            Border mapAreaBorder = new Border();
            mapAreaBorder.Background = new SolidColorBrush(new ColorW(0f, 0f, 0f, 0.6f));
            Canvas.SetLeft(mapAreaBorder, resource.W.Width);
            Canvas.SetTop(mapAreaBorder, resource.N.Height);
            canvas.Children.Add(mapAreaBorder);

            MapArea mapArea = new MapArea();
            mapArea.Name = "mapArea";
            mapArea.SetBinding(MapArea.WidthProperty, new Binding(Border.WidthProperty) { Source = mapAreaBorder, Mode = BindingMode.TwoWay });
            mapArea.SetBinding(MapArea.HeightProperty, new Binding(Border.HeightProperty) { Source = mapAreaBorder, Mode = BindingMode.TwoWay });
            mapAreaBorder.Child = mapArea;
            this.SetBinding(UIMinimap2.MinimapCanvasProperty, new Binding("MinimapTexture") { Source = mapArea, Mode = BindingMode.OneWayToSource });
            this.SetBinding(UIMinimap2.MapRegionProperty, new Binding("MapRegion") { Source = mapArea, Mode = BindingMode.OneWayToSource });
            this.SetBinding(UIMinimap2.CameraCenterProperty, new Binding("CameraCenter") { Source = mapArea, Mode = BindingMode.OneWayToSource });
            this.SetBinding(UIMinimap2.IconsProperty, new Binding("Icons") { Source = mapArea, Mode = BindingMode.OneWayToSource });

            Image imgMapMark = new Image();
            imgMapMark.SetBinding(Image.SourceProperty, new Binding("MapMark") { Source = this, Converter = new TextureImageConverter() });
            Canvas.SetLeft(imgMapMark, 7);
            Canvas.SetTop(imgMapMark, 17);
            canvas.Children.Add(imgMapMark);

            TextBlock lblStreetName = new TextBlock();
            lblStreetName.Name = "lblStreetName";
            lblStreetName.FontFamily = new FontFamily("宋体");
            lblStreetName.FontSize = 12;
            lblStreetName.FontStyle = FontStyle.Bold;
            lblStreetName.Foreground = Brushes.White;
            lblStreetName.SetBinding(TextBlock.TextProperty, new Binding("StreetName") { Source = this });
            lblStreetName.DataContext = this;
            Canvas.SetLeft(lblStreetName, 48);
            Canvas.SetTop(lblStreetName, 20);
            canvas.Children.Add(lblStreetName);

            TextBlock lblMapName = new TextBlock();
            lblMapName.Name = "lblMapName";
            lblMapName.FontFamily = new FontFamily("宋体");
            lblMapName.FontSize = 12;
            lblMapName.FontStyle = FontStyle.Bold;
            lblMapName.SetBinding(TextBlock.TextProperty, new Binding("MapName") { Source = this });
            lblMapName.Foreground = Brushes.White;
            Canvas.SetLeft(lblMapName, 48);
            Canvas.SetTop(lblMapName, 34);
            canvas.Children.Add(lblMapName);

            ComboBox cb = new ComboBox();
            cb.Background = new LCRBrush() { Resource = new ComboBoxBackgroundResource() };
            cb.Width = 150;
            cb.Height = 17;
            cb.Focusable = false;
            cb.Visibility = Visibility.Hidden;
            canvas.Children.Add(cb);

            this.resource = resource;
            this.MapAreaControl = mapArea;
            this.lblStreetName = lblStreetName;
            this.lblMapName = lblMapName;

            FontManager.Instance.AddFont("宋体", 12, FontStyle.Bold);
            base.InitializeComponents();
        }

        protected override void OnPropertyChanged(DependencyProperty property)
        {
            var res = ResourceDictionary.DefaultDictionary;
            base.OnPropertyChanged(property);

            if (property == MinimapCanvasProperty)
            {
                CalcControlSize();
            }
        }

        private void CalcControlSize()
        {
            var canvas = this.MinimapCanvas;
            Size minimapSize = new Size(canvas?.Width ?? 0, canvas?.Height ?? 0);

            //计算边框
            int top = this.resource.N.Height,
                bottom = this.resource.S.Height,
                left = this.resource.W.Width,
                right = this.resource.E.Width;

            //计算实际大小
            Size desireSize = new Size(minimapSize.Width + left + right, minimapSize.Height + top + bottom);
            this.Width = MathHelper.Clamp(desireSize.Width, this.MinWidth, this.MaxWidth);
            this.Height = MathHelper.Clamp(desireSize.Height, this.MinHeight, this.MaxHeight);
            this.MapAreaControl.Width = Math.Max(0, this.Width - left - right);
            this.MapAreaControl.Height = Math.Max(0, this.Height - top - bottom);
        }

        public class MapArea : Control, ITooltipTarget
        {
            public MapArea()
            {
                iconRectCache = new List<IconRect>();
            }

            public TextureBase MinimapTexture { get; set; }
            public Rect MapRegion { get; set; }
            public PointF CameraCenter { get; set; }
            public List<MapIcon> Icons { get; set; }

            private List<IconRect> iconRectCache;

            protected override void OnDraw(Renderer spriterenderer, double elapsedGameTime, float opacity)
            {
                base.OnDraw(spriterenderer, elapsedGameTime, opacity);

                if (this.MinimapTexture == null || this.MapRegion.Width <= 0 || this.MapRegion.Height <= 0)
                {
                    return;
                }

                var pos = this.RenderPosition;
                var size = this.RenderSize;
                spriterenderer.End();
                spriterenderer.BeginClipped(new Rect(pos.X, pos.Y, size.Width, size.Height));

                Matrix transform;
                //绘制canvas
                {
                    var canvas = this.MinimapTexture;
                    var mapRegion = this.MapRegion;


                    //计算小地图显示位置
                    PointF canvasPos = new PointF();

                    if (this.Width >= canvas.Width)
                    {
                        canvasPos.X = (this.Width - canvas.Width) / 2;
                    }
                    else
                    {
                        var centerX = (CameraCenter.X - mapRegion.X) * canvas.Width / mapRegion.Width;
                        canvasPos.X = -MathHelper.Clamp(centerX - this.Width / 2, 0, canvas.Width - this.Width);
                    }
                    if (this.Height >= canvas.Height)
                    {
                        canvasPos.Y = (this.Height - canvas.Height) / 2;
                    }
                    else
                    {
                        var centerY = (CameraCenter.Y - mapRegion.Y) * canvas.Height / mapRegion.Height;
                        canvasPos.Y = -MathHelper.Clamp(centerY - this.Height / 2, 0, canvas.Height - this.Height);
                    }

                    //计算全局坐标
                    canvasPos = new PointF(pos.X + canvasPos.X, pos.Y + canvasPos.Y);

                    //计算世界坐标系到小地图坐标系的转换
                    transform = Matrix.CreateTranslation(-mapRegion.X, -mapRegion.Y, 0)
                            * Matrix.CreateScale((float)canvas.Width / mapRegion.Width, (float)canvas.Height / mapRegion.Height, 0)
                            * Matrix.CreateTranslation(canvasPos.X, canvasPos.Y, 0);

                    //绘制小地图
                    spriterenderer.Draw(canvas, canvasPos, new Size(canvas.Width, canvas.Height), new ColorW(1f, 1f, 1f, opacity), false);
                }

                /* 绘制框线
                using (var gb = spriterenderer.CreateGeometryBuffer())
                {
                    var pts = new List<PointF>();
                    pts.Add(new PointF(0, 0));
                    pts.Add(new PointF(0, 180));
                    pts.Add(new PointF(30, 180));
                    pts.Add(new PointF(30, 0));
                    pts.Add(new PointF(0, 0));
                    gb.FillColor(pts, GeometryPrimitiveType.LineStrip);

                    spriterenderer.DrawGeometryColor(gb, pos, new ColorW(255, 255, 0), opacity, 0f);
                }*/

                //绘制小图标
                Func<TextureBase, PointF, Rect> drawIconFunc = (texture, worldPos) =>
                {
                    var iconPos = Vector2.Transform(new Vector2(worldPos.X, worldPos.Y), transform);
                    var posF = new PointF(
                        Math.Round(iconPos.X - texture.Width / 2- 0),
                        Math.Round(iconPos.Y - texture.Height / 2 - 5));
                    var iconSize = new Size(texture.Width, texture.Height);
                    spriterenderer.Draw(texture, posF, iconSize, new ColorW(1f, 1f, 1f, opacity), false);
                    return new Rect(posF.X - pos.X, posF.Y - pos.Y, iconSize.Width, iconSize.Height);
                };

                iconRectCache.Clear();
                foreach (var icon in this.Icons)
                {
                    switch (icon.IconType)
                    {
                        case IconType.Portal:
                            {
                                var texture = this.FindResource("portal") as TextureBase;
                                if (texture != null)
                                {
                                    var rect = drawIconFunc(texture, icon.WorldPosition);
                                    iconRectCache.Add(new IconRect() { Rect = rect, Tooltip = icon.Tooltip });
                                }
                            }
                            break;

                        case IconType.Transport:
                            {
                                var texture = this.FindResource("transport") as TextureBase;
                                if (texture != null)
                                {
                                    var rect = drawIconFunc(texture, icon.WorldPosition);
                                    iconRectCache.Add(new IconRect() { Rect = rect, Tooltip = icon.Tooltip });
                                }
                            }
                            break;
                    }
                }

                spriterenderer.EndClipped();
                spriterenderer.Begin();
            }

            public object GetTooltipTarget(PointF mouseLocation)
            {
                foreach(var iconRect in this.iconRectCache.Reverse<IconRect>())
                {
                    if (iconRect.Rect.Contains(mouseLocation))
                    {
                        return iconRect.Tooltip;
                    }
                }
                return null;
            }

            public void LoadWzResource()
            {
                var mapHelperNode = PluginBase.PluginManager.FindWz("Map/MapHelper.img/minimap");
                Action<string> addResource = (key) =>
                {
                    Wz_Node resNode = mapHelperNode.Nodes[key];
                    var texture = UIHelper.LoadTexture(resNode);
                    if (texture != null)
                    {
                        this.Resources[key] = texture;
                    }
                };

                if (mapHelperNode != null)
                {
                    addResource("transport");
                    addResource("portal");
                }
            }

            private struct IconRect
            {
                public Rect Rect;
                public object Tooltip;
            }
        }

        public class MapIcon
        {
            public IconType IconType { get; set; }
            public PointF WorldPosition { get; set; }
            public object Tooltip { get; set; }
        }

        public enum IconType
        {
            Unknown = 0,
            Portal,
            Transport,
        }

        private sealed class UIMinimapResource : INinePatchResource<TextureBase>
        {
            public UIMinimapResource()
            {
                this.LoadResource(Engine.Instance.Renderer);
            }

            public bool HasIcon { get; set; }

            public TextureBase NW1 { get; set; }
            public TextureBase NW2 { get; set; }

            /// <summary>
            /// 左上
            /// </summary>
            public TextureBase NW
            {
                get { return this.HasIcon ? NW1 : NW2; }
            }
            /// <summary>
            /// 上。
            /// </summary>
            public TextureBase N { get; set; }
            /// <summary>
            /// 右上。5
            /// </summary>
            public TextureBase NE { get; set; }

            /// <summary>
            /// 左。
            /// </summary>
            public TextureBase W { get; set; }
            /// <summary>
            /// 中。
            /// </summary>
            public TextureBase C { get; set; }
            /// <summary>
            /// 右。
            /// </summary>
            public TextureBase E { get; set; }

            /// <summary>
            /// 左下。
            /// </summary>
            public TextureBase SW { get; set; }
            /// <summary>
            /// 下。
            /// </summary>
            public TextureBase S { get; set; }
            /// <summary>
            /// 右下。
            /// </summary>
            public TextureBase SE { get; set; }

            public Microsoft.Xna.Framework.Point GetSize(TextureBase texture)
            {
                return new Microsoft.Xna.Framework.Point(texture.Width, texture.Height);
            }

            private void LoadResource(Renderer renderer)
            {
                this.NW1 = renderer.CreateTexture(Res.UIWindow2_img_MiniMap_MaxMap_nw);
                this.NW2 = renderer.CreateTexture(Res.UIWindow2_img_MiniMap_MaxMap_nw2);
                this.N = renderer.CreateTexture(Res.UIWindow2_img_MiniMap_MaxMap_n);
                this.NE = renderer.CreateTexture(Res.UIWindow2_img_MiniMap_MaxMap_ne);
                this.W = renderer.CreateTexture(Res.UIWindow2_img_MiniMap_MaxMap_w);
                //this.C = renderer.CreateTexture(Res.UIWindow2_img_MiniMap_MaxMap_c);
                this.E = renderer.CreateTexture(Res.UIWindow2_img_MiniMap_MaxMap_e);
                this.SW = renderer.CreateTexture(Res.UIWindow2_img_MiniMap_MaxMap_sw);
                this.S = renderer.CreateTexture(Res.UIWindow2_img_MiniMap_MaxMap_s);
                this.SE = renderer.CreateTexture(Res.UIWindow2_img_MiniMap_MaxMap_se);
            }
        }

        private sealed class ComboBoxBackgroundResource : INinePatchResource<TextureBase>
        {
            public ComboBoxBackgroundResource()
            {
                this.LoadResource(Engine.Instance.Renderer);
            }

            public TextureBase Left { get; set; }
            public TextureBase Center { get; set; }
            public TextureBase Right { get; set; }

            public Microsoft.Xna.Framework.Point GetSize(TextureBase texture)
            {
                return new Microsoft.Xna.Framework.Point(texture.Width, texture.Height);
            }

            private void LoadResource(Renderer renderer)
            {
                this.Left = renderer.CreateTexture(MRes.Basic_img_ComboBox_normal_0);
                this.Center = renderer.CreateTexture(MRes.Basic_img_ComboBox_normal_1);
                this.Right = renderer.CreateTexture(MRes.Basic_img_ComboBox_normal_2);
            }

            #region Interface implementation
            TextureBase INinePatchResource<TextureBase>.C
            {
                get { return this.Center; }
            }

            TextureBase INinePatchResource<TextureBase>.E
            {
                get { return this.Right; }
            }

            TextureBase INinePatchResource<TextureBase>.W
            {
                get { return this.Left; }
            }

            TextureBase INinePatchResource<TextureBase>.N
            {
                get { throw new NotImplementedException(); }
            }

            TextureBase INinePatchResource<TextureBase>.NE
            {
                get { throw new NotImplementedException(); }
            }

            TextureBase INinePatchResource<TextureBase>.NW
            {
                get { throw new NotImplementedException(); }
            }

            TextureBase INinePatchResource<TextureBase>.S
            {
                get { throw new NotImplementedException(); }
            }

            TextureBase INinePatchResource<TextureBase>.SE
            {
                get { throw new NotImplementedException(); }
            }

            TextureBase INinePatchResource<TextureBase>.SW
            {
                get { throw new NotImplementedException(); }
            }
            #endregion
        }

        private class TextureImageConverter : IValueConverter
        {
            public object Convert(object value, Type target, object parameter, CultureInfo culture)
            {
                var texture = value as TextureBase;
                if (texture != null)
                {
                    return new BitmapImage() { Texture = texture };
                }
                return null;
            }

            public object ConvertBack(object value, Type target, object parameter, CultureInfo culture)
            {
                var image = value as BitmapImage;
                if (image != null)
                {
                    return image.Texture;
                }
                return null;
            }
        }
    }
}

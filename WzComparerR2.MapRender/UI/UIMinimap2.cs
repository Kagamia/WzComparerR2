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
        public static readonly DependencyProperty CameraViewPortProperty = DependencyProperty.Register("CameraViewPort", typeof(Rect), typeof(UIMinimap2), new FrameworkPropertyMetadata(new Rect()));
        public static readonly DependencyProperty IconsProperty = DependencyProperty.Register("Icons", typeof(List<MapIcon>), typeof(UIMinimap2), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty CameraRegionVisibleProperty = DependencyProperty.Register("CameraRegionVisible", typeof(bool), typeof(UIMinimap2), new FrameworkPropertyMetadata(false));

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

        public Rect CameraViewPort
        {
            get { return (Rect)GetValue(CameraViewPortProperty); }
            set { SetValue(CameraViewPortProperty, value); }
        }

        public List<MapIcon> Icons
        {
            get { return (List<MapIcon>)GetValue(IconsProperty); }
            private set { SetValue(IconsProperty, value); }
        }

        public bool CameraRegionVisible
        {
            get { return (bool)GetValue(CameraRegionVisibleProperty); }
            set { SetValue(CameraRegionVisibleProperty, value); }
        }

        public MapArea MapAreaControl { get; private set; }

        public bool Mirror
        {
            get { return mirror; }
            set
            {
                if (value != mirror)
                {
                    this.resource.LoadResource(Engine.Instance.AssetManager, value);
                }
                mirror = value;
            }
        }

        private UIMinimapResource resource;

        private TextBlock lblStreetName;
        private TextBlock lblMapName;

        private bool mirror;

        protected override void InitializeComponents()
        {
            UIMinimapResource resource = new UIMinimapResource();
            this.MinWidth = resource.NW.Width + resource.NE.Width;
            this.MinHeight = resource.NW.Height + resource.SW.Height;
            this.Width = this.MinWidth;
            this.Height = this.MinHeight;
            this.MaxWidth = 1000;
            this.MaxHeight = 1000;

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
            this.SetBinding(UIMinimap2.CameraViewPortProperty, new Binding("CameraViewPort") { Source = mapArea, Mode = BindingMode.OneWayToSource });
            this.SetBinding(UIMinimap2.IconsProperty, new Binding("Icons") { Source = mapArea, Mode = BindingMode.OneWayToSource });
            this.SetBinding(UIMinimap2.CameraRegionVisibleProperty, new Binding("CameraRegionVisible") { Source = mapArea, Mode = BindingMode.OneWayToSource });

            Image imgMapMark = new Image();
            imgMapMark.SetBinding(Image.SourceProperty, new Binding("MapMark") { Source = this, Converter = new TextureImageConverter() });
            Canvas.SetLeft(imgMapMark, 7);
            Canvas.SetTop(imgMapMark, 17);
            canvas.Children.Add(imgMapMark);

            TextBlock lblStreetName = new TextBlock();
            lblStreetName.Name = "lblStreetName";
            lblStreetName.FontStyle = FontStyle.Bold;
            lblStreetName.Foreground = Brushes.White;
            lblStreetName.Padding = new Thickness(0, 0, 6, 0);
            lblStreetName.SetBinding(TextBlock.TextProperty, new Binding("StreetName") { Source = this });
            lblStreetName.SetResourceReference(TextBlock.FontFamilyProperty, MapRenderResourceKey.DefaultFontFamily);
            lblStreetName.SetResourceReference(TextBlock.FontSizeProperty, MapRenderResourceKey.DefaultFontSize);
            Canvas.SetLeft(lblStreetName, 48);
            Canvas.SetTop(lblStreetName, 20);
            canvas.Children.Add(lblStreetName);

            TextBlock lblMapName = new TextBlock();
            lblMapName.Name = "lblMapName";
            lblMapName.FontStyle = FontStyle.Bold;
            lblMapName.Foreground = Brushes.White;
            lblMapName.Padding = new Thickness(0, 0, 6, 0);
            lblMapName.SetBinding(TextBlock.TextProperty, new Binding("MapName") { Source = this });
            lblMapName.SetResourceReference(TextBlock.FontFamilyProperty, MapRenderResourceKey.DefaultFontFamily);
            lblMapName.SetResourceReference(TextBlock.FontSizeProperty, MapRenderResourceKey.DefaultFontSize);
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

            FontManager.Instance.AddFont(lblStreetName.FontFamily.Source, lblStreetName.FontSize, lblStreetName.FontStyle);
            FontManager.Instance.AddFont(lblMapName.FontFamily.Source, lblMapName.FontSize, lblMapName.FontStyle);
            base.InitializeComponents();
        }

        protected override void OnPropertyChanged(DependencyProperty property)
        {
            base.OnPropertyChanged(property);

            if (property == MinimapCanvasProperty
                || property == MapNameProperty
                || property == StreetNameProperty)
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

            //计算标题
            if (this.lblStreetName.Text != null)
            {
                this.lblStreetName.Measure(new Size(double.MaxValue, double.MaxValue));
                var lblRight = Canvas.GetLeft(this.lblStreetName) + this.lblStreetName.DesiredSize.Width;
                desireSize.Width = Math.Max(desireSize.Width, lblRight);
            }
            if (this.lblMapName.Text != null)
            {
                this.lblMapName.Measure(new Size(double.MaxValue, double.MaxValue));
                var lblRight = Canvas.GetLeft(this.lblMapName) + this.lblMapName.DesiredSize.Width;
                desireSize.Width = Math.Max(desireSize.Width, lblRight);
            }

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
            public Rect CameraViewPort { get; set; }
            public List<MapIcon> Icons { get; set; }
            public bool CameraRegionVisible { get; set; }

            private List<IconRect> iconRectCache;
            private PointF canvasPosCache;

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
                Vector2 cameraPos;
                Vector2 cameraSize;
                //绘制canvas
                {
                    var canvas = this.MinimapTexture;
                    var mapRegion = this.MapRegion;

                    //计算世界坐标系到小地图坐标系的转换
                    transform = Matrix.CreateTranslation(-mapRegion.X, -mapRegion.Y, 0)
                            * Matrix.CreateScale((float)canvas.Width / mapRegion.Width, (float)canvas.Height / mapRegion.Height, 0);
                    var vp = this.CameraViewPort;
                    cameraPos = Vector2.Transform(new Vector2(vp.X, vp.Y), transform);
                    cameraSize = new Vector2(vp.Width * transform.Scale.X, vp.Height * transform.Scale.Y);

                    //计算小地图显示位置
                    PointF canvasPos = new PointF(-this.canvasPosCache.X, -this.canvasPosCache.Y);

                    if (this.Width >= canvas.Width)
                    {
                        canvasPos.X = (this.Width - canvas.Width) / 2;
                    }
                    else
                    {
                        if (cameraPos.X < canvasPos.X)
                        {
                            canvasPos.X = cameraPos.X;
                        }
                        else if (cameraPos.X + cameraSize.X > canvasPos.X + this.Width)
                        {
                            canvasPos.X = cameraPos.X + cameraSize.X - this.Width;
                        }
                        canvasPos.X = -MathHelper.Clamp(canvasPos.X, 0, canvas.Width - this.Width);
                    }
                    if (this.Height >= canvas.Height)
                    {
                        canvasPos.Y = (this.Height - canvas.Height) / 2;
                    }
                    else
                    {
                        if (cameraPos.Y < canvasPos.Y)
                        {
                            canvasPos.Y = cameraPos.Y;
                        }
                        else if (cameraPos.Y + cameraSize.Y > canvasPos.Y + this.Height)
                        {
                            canvasPos.Y = cameraPos.Y + cameraSize.Y - this.Height;
                        }
                        canvasPos.Y = -MathHelper.Clamp(canvasPos.Y, 0, canvas.Height - this.Height);
                    }

                    this.canvasPosCache = canvasPos;
                    //计算全局坐标
                    canvasPos = new PointF(pos.X + canvasPos.X, pos.Y + canvasPos.Y);


                    transform *= Matrix.CreateTranslation(canvasPos.X, canvasPos.Y, 0);

                    //绘制小地图
                    spriterenderer.Draw(canvas, canvasPos, new Size(canvas.Width, canvas.Height), new ColorW(1f, 1f, 1f, opacity), false);
                }

                /* 绘制框线*/
                if (CameraRegionVisible)
                {
                    using (var gb = spriterenderer.CreateGeometryBuffer())
                    {
                        var pts = new List<PointF>();
                        pts.Add(new PointF(cameraPos.X, cameraPos.Y));
                        pts.Add(new PointF(cameraPos.X + cameraSize.X, cameraPos.Y));
                        pts.Add(new PointF(cameraPos.X + cameraSize.X, cameraPos.Y + cameraSize.Y));
                        pts.Add(new PointF(cameraPos.X, cameraPos.Y + cameraSize.Y));
                        pts.Add(pts[0]);
                        gb.FillColor(pts, GeometryPrimitiveType.LineStrip);

                        spriterenderer.DrawGeometryColor(gb,
                            new PointF(pos.X + canvasPosCache.X, pos.Y + canvasPosCache.Y),
                            new ColorW(255, 255, 0), opacity, 0f);
                    }
                }

                //绘制小图标
                Func<TextureBase, PointF, Rect> drawIconFunc = (texture, worldPos) =>
                {
                    var iconPos = Vector2.Transform(new Vector2(worldPos.X, worldPos.Y), transform);
                    var posF = new PointF(
                        Math.Round(iconPos.X - texture.Width / 2 - 0),
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

                        case IconType.EnchantPortal:
                            {
                                var texture = this.FindResource("enchantportal") as TextureBase;
                                if (texture != null)
                                {
                                    var rect = drawIconFunc(texture, icon.WorldPosition);
                                    iconRectCache.Add(new IconRect() { Rect = rect, Tooltip = icon.Tooltip });
                                }
                            }
                            break;

                        case IconType.HiddenPortal:
                            {
                                var texture = this.FindResource("hiddenportal") as TextureBase;
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

                        case IconType.Npc:
                            {
                                var texture = this.FindResource("npc") as TextureBase;
                                if (texture != null)
                                {
                                    var rect = drawIconFunc(texture, icon.WorldPosition);
                                    iconRectCache.Add(new IconRect() { Rect = rect, Tooltip = icon.Tooltip });
                                }
                            }
                            break;

                        case IconType.EventNpc:
                            {
                                var texture = this.FindResource("eventnpc") as TextureBase;
                                if (texture != null)
                                {
                                    var rect = drawIconFunc(texture, icon.WorldPosition);
                                    iconRectCache.Add(new IconRect() { Rect = rect, Tooltip = icon.Tooltip });
                                }
                            }
                            break;

                        case IconType.Shop:
                            {
                                var texture = this.FindResource("shop") as TextureBase;
                                if (texture != null)
                                {
                                    var rect = drawIconFunc(texture, icon.WorldPosition);
                                    iconRectCache.Add(new IconRect() { Rect = rect, Tooltip = icon.Tooltip });
                                }
                            }
                            break;

                        case IconType.Trunk:
                            {
                                var texture = this.FindResource("trunk") as TextureBase;
                                if (texture != null)
                                {
                                    var rect = drawIconFunc(texture, icon.WorldPosition);
                                    iconRectCache.Add(new IconRect() { Rect = rect, Tooltip = icon.Tooltip });
                                }
                            }
                            break;

                        case IconType.ArrowUp:
                            {
                                var texture = this.FindResource("arrowup") as TextureBase;
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
                foreach (var iconRect in this.iconRectCache.Reverse<IconRect>())
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
                    addResource("enchantportal");
                    addResource("hiddenportal");
                    addResource("npc");
                    addResource("eventnpc");
                    addResource("shop");
                    addResource("trunk");
                    addResource("arrowup");
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
            EnchantPortal,
            HiddenPortal,
            Transport,
            Npc,
            EventNpc,
            Shop,
            Trunk,
            ArrowUp,
        }

        private sealed class UIMinimapResource : INinePatchResource<TextureBase>
        {
            public UIMinimapResource()
            {
                this.LoadResource(Engine.Instance.AssetManager);
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

            public void LoadResource(AssetManager assetManager, bool mirror = false)
            {
                if (!mirror)
                {
                    this.NW1 = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMap_nw));
                    this.NW2 = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMap_nw2));
                    this.N = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMap_n));
                    this.NE = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMap_ne));
                    this.W = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMap_w));
                    //this.C = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMap_c);
                    this.E = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMap_e));
                    this.SW = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMap_sw));
                    this.S = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMap_s));
                    this.SE = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMap_se));
                }
                else
                {
                    this.NW1 = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMapMirror_nw));
                    this.NW2 = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMapMirror_nw2));
                    this.N = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMapMirror_n));
                    this.NE = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMapMirror_ne));
                    this.W = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMapMirror_w));
                    //this.C = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMapMirror_c);
                    this.E = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMapMirror_e));
                    this.SW = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMapMirror_sw));
                    this.S = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMapMirror_s));
                    this.SE = assetManager.LoadTexture(null, nameof(Res.UIWindow2_img_MiniMap_MaxMapMirror_se));
                }
            }
        }

        private sealed class ComboBoxBackgroundResource : INinePatchResource<TextureBase>
        {
            public ComboBoxBackgroundResource()
            {
                this.LoadResource(Engine.Instance.AssetManager);
            }

            public TextureBase Left { get; set; }
            public TextureBase Center { get; set; }
            public TextureBase Right { get; set; }

            public Microsoft.Xna.Framework.Point GetSize(TextureBase texture)
            {
                return new Microsoft.Xna.Framework.Point(texture.Width, texture.Height);
            }

            private void LoadResource(AssetManager assetManager)
            {
                this.Left = assetManager.LoadTexture(null, nameof(MRes.Basic_img_ComboBox_normal_0));
                this.Center = assetManager.LoadTexture(null, nameof(MRes.Basic_img_ComboBox_normal_1));
                this.Right = assetManager.LoadTexture(null, nameof(MRes.Basic_img_ComboBox_normal_2));
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

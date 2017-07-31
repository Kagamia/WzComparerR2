using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Input;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Media.Effects;
using EmptyKeys.UserInterface.Themes;
using EmptyKeys.UserInterface.Data;
using Res = CharaSimResource.Resource;

namespace WzComparerR2.MapRender.UI
{
    class MapRenderUIRoot : UIRoot
    {
        public MapRenderUIRoot() : base()
        {
            InitializeComponents();

            //获取root容器
            var propInfo = typeof(Control).GetProperty("VisualTree", BindingFlags.NonPublic | BindingFlags.Instance);
            var border = propInfo?.GetValue(this) as Border;
            this.ContentControl = border?.Child as ContentPresenter;
        }

        public event EventHandler InputUpdated;
        public ContentPresenter ContentControl { get; private set; }
        public UIMinimap2 Minimap { get; private set; }
        public UIWorldMap WorldMap { get; private set; }
        public UITopBar TopBar { get; private set; }

        private void InitializeComponents()
        {
            Style style = RootStyle.CreateRootStyle();
            style.TargetType = this.GetType();
            this.Style = style;

            this.Background = null;

            var minimap = new UIMinimap2();
            minimap.Parent = this;
            this.Minimap = minimap;
            this.Windows.Add(minimap);

            var worldmap = new UIWorldMap();
            worldmap.Parent = this;
            worldmap.Visibility = Visibility.Collapsed;
            worldmap.Visible += Worldmap_Visible;
            this.WorldMap = worldmap;
            this.Windows.Add(worldmap);

            var topBar = new UITopBar();
            topBar.Parent = this;
            topBar.IsOnTop = false;
            topBar.SetBinding(UITopBar.WidthProperty, new Binding(UIRoot.WidthProperty) { Source = this });
            topBar.SetBinding(UITopBar.PaddingLeftProperty, new Binding(Window.WidthProperty) { Source = minimap });
            topBar.SetBinding(UITopBar.IsShortModeProperty, new Binding(Window.VisibilityProperty) { Source = minimap, Converter = UIHelper.CreateConverter((Visibility o) => o == Visibility.Visible) });
            this.TopBar = topBar;
            this.Windows.Add(topBar);
        }

        private void Worldmap_Visible(object sender, RoutedEventArgs e)
        {
            UIWorldMap wnd = sender as UIWorldMap;
            wnd.Left = (int)Math.Max(0, (this.Width - wnd.Width) / 2);
            wnd.Top = (int)Math.Max(0, (this.Height - wnd.Height) / 2);

            if (!wnd.IsDataLoaded)
            {
                wnd.LoadWzResource();
            }
            else
            {
                wnd.JumpToCurrentMap();
            }
        }

        public void LoadContents(object contentManager)
        {
            FontManager.Instance.AddFont("宋体", 12, FontStyle.Regular);
            FontManager.Instance.LoadFonts(contentManager);
            ImageManager.Instance.LoadImages(contentManager);
            SoundManager.Instance.LoadSounds(contentManager);
            EffectManager.Instance.LoadEffects(contentManager);
            FontManager.DefaultFont = FontManager.Instance.GetFont("宋体", 12, FontStyle.Regular);

            //加载其他资源
            this.LoadResource();
            this.Minimap.MapAreaControl.LoadWzResource();
        }

        private void LoadResource()
        {
            var renderer = Engine.Instance.Renderer;

            var tooltipBrush = new NinePatchBrush()
            {
                Resource = new EKNineFormResource()
                {
                    NW = renderer.CreateTexture(Res.UIToolTip_img_Item_Frame2_nw),
                    N = renderer.CreateTexture(Res.UIToolTip_img_Item_Frame2_n),
                    NE = renderer.CreateTexture(Res.UIToolTip_img_Item_Frame2_ne),
                    W = renderer.CreateTexture(Res.UIToolTip_img_Item_Frame2_w),
                    C = renderer.CreateTexture(Res.UIToolTip_img_Item_Frame2_c),
                    E = renderer.CreateTexture(Res.UIToolTip_img_Item_Frame2_e),
                    SW = renderer.CreateTexture(Res.UIToolTip_img_Item_Frame2_sw),
                    S = renderer.CreateTexture(Res.UIToolTip_img_Item_Frame2_s),
                    SE = renderer.CreateTexture(Res.UIToolTip_img_Item_Frame2_se),
                }
            };

            this.Resources[MapRenderResourceKey.TooltipBrush] = tooltipBrush;
        }

        public void UnloadContents()
        {
            FontManager.Instance.ClearCache();
            ImageManager.Instance.ClearCache();
            SoundManager.Instance.ClearCache();
            EffectManager.Instance.ClearCache();
            FontManager.DefaultFont = null;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var listener = e.Source as KeyInputListener;

            if (listener != null)
            {
                foreach (var binding in this.InputBindings)
                {
                    if (binding.IsRepeatEnabled && binding.Gesture.Matches(e))
                    {
                        listener.EnableRepeat();
                        break;
                    }
                }
            }
            base.OnKeyDown(e);
        }

        public new void UpdateInput(double elapsedGameTime)
        {
            base.UpdateInput(elapsedGameTime);
            this.OnInputUpdated(EventArgs.Empty);
        }

        protected virtual void OnInputUpdated(EventArgs e)
        {
            this.InputUpdated?.Invoke(this, e);
        }
    }
}

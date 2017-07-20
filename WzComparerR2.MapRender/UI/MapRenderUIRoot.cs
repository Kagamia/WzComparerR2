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

        private void InitializeComponents()
        {
            Style style = RootStyle.CreateRootStyle();
            style.TargetType = this.GetType();
            this.Style = style;

            this.Background = null;
            /*
            Canvas c = new Canvas();
            this.Button1 = new Button();
            this.Button1.Name = "button1";
            this.Button1.Content = "233";
            this.Button1.Width = 100;
            this.Button1.Height = 100;
            this.Button1.Background = Brushes.FloralWhite;
            Canvas.SetLeft(Button1, 200);

            c.Children.Add(this.Button1);

            this.Content = c;

            MyWindow wnd = new MyWindow();
            wnd.Visibility = Visibility.Visible;
            wnd.IsOnTop = true;
            wnd.Parent = this;
            wnd.Left = 10;
            wnd.Top = 10;
            */
            //this.Windows.Add(wnd);

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
            this.Minimap.MapAreaControl.LoadWzResource();
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

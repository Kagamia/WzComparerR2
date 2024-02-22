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
using MRes = WzComparerR2.MapRender.Properties.Resources;

namespace WzComparerR2.MapRender.UI
{
    class MapRenderUIRoot : UIRoot
    {
        public MapRenderUIRoot() : base()
        {
            InitGlobalResource();
            InitializeComponents();

            //获取root容器
            var propInfo = typeof(Control).GetProperty("VisualTree", BindingFlags.NonPublic | BindingFlags.Instance);
            var border = propInfo?.GetValue(this) as Border;
            this.ContentControl = border?.Child as ContentPresenter;
        }

        public event EventHandler InputUpdated;
        public ContentPresenter ContentControl { get; private set; }
        public UIMirrorFrame MirrorFrame { get; private set; }
        public UIMinimap2 Minimap { get; private set; }
        public UIWorldMap WorldMap { get; private set; }
        public UITopBar TopBar { get; private set; }
        public UIChatBox ChatBox { get; private set; }
        public UITeleport Teleport { get; private set; }

        private void InitializeComponents()
        {
            Style style = RootStyle.CreateRootStyle();
            style.TargetType = this.GetType();
            this.Style = style;
            this.Background = null;

            var mirrorFrame = new UIMirrorFrame();
            mirrorFrame.Parent = this;
            mirrorFrame.IsOnTop = false;
            mirrorFrame.Visibility = Visibility.Collapsed;
            mirrorFrame.SetBinding(UIMirrorFrame.WidthProperty, new Binding(UIRoot.WidthProperty) { Source = this });
            mirrorFrame.SetBinding(UIMirrorFrame.HeightProperty, new Binding(UIRoot.HeightProperty) { Source = this });
            this.MirrorFrame = mirrorFrame;
            this.Windows.Add(mirrorFrame);

            var minimap = new UIMinimap2();
            minimap.Parent = this;
            this.Minimap = minimap;
            this.Windows.Add(minimap);

            var worldmap = new UIWorldMap();
            worldmap.Parent = this;
            worldmap.Hide();
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

            var chatBox = new UIChatBox();
            chatBox.Parent = this;
            chatBox.SetBinding(UIChatBox.TopProperty, new Binding(HeightProperty) { Source = this, Converter = UIHelper.CreateConverter((float height) => height - chatBox.Height) });
            this.ChatBox = chatBox;
            this.Windows.Add(chatBox);

            var teleport = new UITeleport();
            teleport.Parent = this;
            teleport.Hide();
            teleport.Visible += Teleport_Visible;
            this.Teleport = teleport;
            this.Windows.Add(teleport);

            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtOK4_normal_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtOK4_mouseOver_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtOK4_pressed_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtOK4_disabled_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtNo3_normal_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtNo3_mouseOver_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtNo3_pressed_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtNo3_disabled_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtCancel4_normal_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtCancel4_mouseOver_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtCancel4_pressed_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtCancel4_disabled_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtClose3_normal_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtClose3_mouseOver_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtClose3_pressed_0));
            ImageManager.Instance.AddImage(nameof(MRes.Basic_img_BtClose3_disabled_0));
            this.Resources[CommonResourceKeys.MessageBoxWindowStyleKey] = MessageBoxStyle.CreateMessageBoxStyle();
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

        private void Teleport_Visible(object sender, RoutedEventArgs e)
        {
            UITeleport wnd = sender as UITeleport;
            wnd.Left = (int)Math.Max(0, (this.Width - wnd.Width) / 2);
            wnd.Top = (int)Math.Max(0, (this.Height - wnd.Height) / 2);
        }

        public void LoadContent(object contentManager)
        {
            //UI资源
            FontManager.DefaultFontFamily = (FontFamily)this.FindResource(MapRenderResourceKey.DefaultFontFamily);
            FontManager.DefaultFontSize = (float)this.FindResource(MapRenderResourceKey.DefaultFontSize);
            FontManager.Instance.AddFont(FontManager.DefaultFontFamily.Source, FontManager.DefaultFontSize, FontStyle.Regular);
            FontManager.Instance.LoadFonts(contentManager);
            ImageManager.Instance.LoadImages(contentManager);
            SoundManager.Instance.LoadSounds(contentManager);
            EffectManager.Instance.LoadEffects(contentManager);
            FontManager.DefaultFont = FontManager.Instance.GetFont(FontManager.DefaultFontFamily.Source, FontManager.DefaultFontSize, FontStyle.Regular);

            //其他资源
            this.LoadResource();
            this.Minimap.MapAreaControl.LoadWzResource();
        }

        private void InitGlobalResource()
        {
            //初始化字体
            var fontList = MapRenderFonts.DefaultFonts;
            var config = MapRender.Config.MapRenderConfig.Default;
            var resDict = ResourceDictionary.DefaultDictionary;

            var fontIndex = config.DefaultFontIndex;
            if (fontIndex < 0 || fontIndex >= fontList.Count)
            {
                fontIndex = 0;
            }
            
            resDict[MapRenderResourceKey.FontList] = fontList;
            resDict[MapRenderResourceKey.DefaultFontFamily] = new FontFamily(fontList[fontIndex]);
            resDict[MapRenderResourceKey.DefaultFontSize] = 12f;

            //初始化style
            resDict[MapRenderResourceKey.MapRenderButtonStyle] = MapRenderButtonStyle.CreateMapRenderButtonStyle();
            resDict[MapRenderResourceKey.TextBoxExStyle] = TextBoxEx.CreateStyle();
        }

        private void LoadResource()
        {
            var assetManager = Engine.Instance.AssetManager;

            var tooltipBrush = new NinePatchBrush()
            {
                Resource = new EKNineFormResource()
                {
                    NW = assetManager.LoadTexture(null, nameof(Res.UIToolTip_img_Item_Frame2_nw)),
                    N = assetManager.LoadTexture(null, nameof(Res.UIToolTip_img_Item_Frame2_n)),
                    NE = assetManager.LoadTexture(null, nameof(Res.UIToolTip_img_Item_Frame2_ne)),
                    W = assetManager.LoadTexture(null, nameof(Res.UIToolTip_img_Item_Frame2_w)),
                    C = assetManager.LoadTexture(null, nameof(Res.UIToolTip_img_Item_Frame2_c)),
                    E = assetManager.LoadTexture(null, nameof(Res.UIToolTip_img_Item_Frame2_e)),
                    SW = assetManager.LoadTexture(null, nameof(Res.UIToolTip_img_Item_Frame2_sw)),
                    S = assetManager.LoadTexture(null, nameof(Res.UIToolTip_img_Item_Frame2_s)),
                    SE = assetManager.LoadTexture(null, nameof(Res.UIToolTip_img_Item_Frame2_se)),
                }
            };

            var msgBoxBackgroundBrush = new MessageBoxBackgroundBrush()
            {
                Resource = new MessageBoxBackgroundResource()
                {
                    T = assetManager.LoadTexture(null, nameof(MRes.Basic_img_Notice6_t)),
                    C = assetManager.LoadTexture(null, nameof(MRes.Basic_img_Notice6_c)),
                    C_Box = assetManager.LoadTexture(null, nameof(MRes.Basic_img_Notice6_c_box)),
                    Box = assetManager.LoadTexture(null, nameof(MRes.Basic_img_Notice6_box)),
                    S_Box = assetManager.LoadTexture(null, nameof(MRes.Basic_img_Notice6_s_box)),
                    S = assetManager.LoadTexture(null, nameof(MRes.Basic_img_Notice6_s)),
                }
            };

            this.Resources[MapRenderResourceKey.TooltipBrush] = tooltipBrush;
            this.Resources[MapRenderResourceKey.MessageBoxBackgroundBrush] = msgBoxBackgroundBrush;
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
            try
            {
                base.UpdateInput(elapsedGameTime);
            }
            catch(Exception ex)
            {
            }
            this.OnInputUpdated(EventArgs.Empty);
        }

        protected virtual void OnInputUpdated(EventArgs e)
        {
            this.InputUpdated?.Invoke(this, e);
        }
    }
}

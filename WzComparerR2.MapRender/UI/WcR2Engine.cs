using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Input;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Renderers;
using EmptyKeys.UserInterface.Mvvm;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using WzComparerR2.WzLib;
using WzComparerR2.Rendering;
using WzComparerR2.MapRender;
using ContentManager = Microsoft.Xna.Framework.Content.ContentManager;
using MRes = WzComparerR2.MapRender.Properties.Resources;
using Res = CharaSimResource.Resource;

namespace WzComparerR2.MapRender.UI
{
    #region Engine
    class WcR2Engine : Engine
    {
        public WcR2Engine(GraphicsDevice graphicsDevice, int nativeScreenWidth, int nativeScreenHeight)
        {
            _renderer = new WcR2Renderer(graphicsDevice, nativeScreenWidth, nativeScreenHeight);
            _assetManager = new WcR2AssetManager();
            _audioDevice = new WcR2AudioDevice();
            _inputDevice = new MonoGameInputDevice();

            if (ServiceManager.Instance.GetService<IClipboardService>() == null)
            {
                ServiceManager.Instance.AddService<IClipboardService>(new ClipBoardService());
            }
        }

        private WcR2AssetManager _assetManager;
        private WcR2AudioDevice _audioDevice;
        private MonoGameInputDevice _inputDevice;
        private WcR2Renderer _renderer;

        public override AssetManager AssetManager
        {
            get { return _assetManager; }
        }

        public override AudioDevice AudioDevice
        {
            get { return _audioDevice; }
        }

        public override InputDeviceBase InputDevice
        {
            get { return _inputDevice; }
        }

        public override Renderer Renderer
        {
            get { return _renderer; }
        }

        public static void FixEKBugs()
        {
            InitialInputManager();
            FixBorderTexture();
            FixDefaultTheme();
        }

        public static void Unload()
        {
            if (Engine.instance?.InputDevice?.KeyboardState != null)
            {
                InputManager.Current.ClearFocus();
                Engine.instance = null;

                FieldInfo inputManagerCurrentField = typeof(InputManager)
                    .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                    .FirstOrDefault(field => field.FieldType == typeof(InputManager));
                inputManagerCurrentField.SetValue(null, (InputManager)null);
            }
          
            VisualTreeHelper.Instance.ClearParentCache();
            typeof(MessageBox).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(field => field.FieldType == typeof(MessageBox))
                .SetValue(null, Activator.CreateInstance(typeof(MessageBox), true));
            typeof(DragDrop).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(field => field.FieldType == typeof(DragDrop))
                .SetValue(null, Activator.CreateInstance(typeof(DragDrop), true));
        }

        public static void InitialInputManager()
        {
            //重置inputManager
            {
                var type = typeof(InputManager);
                var fieldInfo = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                    .FirstOrDefault(_field => _field.FieldType == typeof(InputManager));

                var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.CreateInstance | BindingFlags.NonPublic, Type.DefaultBinder, new Type[0], null);

                if (fieldInfo != null && ctor != null)
                {
                    var instance = ctor.Invoke(new object[0]);
                    fieldInfo.SetValue(null, instance);
                }
            }
            //重置keyboardState
            {
                var type = typeof(Keyboard);
                var propertyInfo = type.GetProperty(nameof(Keyboard.Modifiers), BindingFlags.Static | BindingFlags.Public);

                if (propertyInfo != null)
                {
                    propertyInfo.GetSetMethod(true).Invoke(null, new object[] { ModifierKeys.None });
                }
            }
        }

        private static void FixBorderTexture()
        {
            var type = typeof(EmptyKeys.UserInterface.Controls.Border);
            var propSetFunc = typeof(DependencyProperty).GetProperty(nameof(DependencyProperty.DefaultValue)).GetSetMethod(true);
            var defaultValue = Engine.instance.Renderer.CreateTexture(1, 1, false, false);
            bool isUsed = false;
            {
                var field = type.GetField("BackgroundTextureProperty", BindingFlags.Static | BindingFlags.NonPublic);
                var oldVal = field.GetValue(null) as DependencyProperty;
                if (oldVal != null)
                {
                    oldVal.DefaultMetadata.DefaultValue = defaultValue;
                    propSetFunc.Invoke(oldVal, new[] { defaultValue });
                    isUsed = true;
                }
            }
            {
                var field = type.GetField("BorderTextureProperty", BindingFlags.Static | BindingFlags.NonPublic);
                var oldVal = field.GetValue(null) as DependencyProperty;
                if (oldVal != null)
                {
                    oldVal.DefaultMetadata.DefaultValue = defaultValue;
                    propSetFunc.Invoke(oldVal, new[] { defaultValue });
                    isUsed = true;
                }
            }

            if (!isUsed)
            {
                defaultValue.Dispose();
            }
            else
            {
                defaultValue.GenerateOneToOne();
            }
        }

        private static void FixDefaultTheme()
        {
            var type = typeof(EmptyKeys.UserInterface.Themes.EmptyKeysTheme);
            var fieldType = typeof(ResourceDictionary);
            var field = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.FieldType == fieldType);
            if (field != null)
            {
                ResourceDictionary value = field.GetValue(null) as ResourceDictionary;
                if (value != null)
                {
                    var texture = value.Values.OfType<ImageBrush>().FirstOrDefault()?.ImageSource?.Texture?.GetNativeTexture() as Texture2D;
                    if (texture != null && texture.IsDisposed)
                    {
                        value = new ResourceDictionary();
                        field.SetValue(null, value);
                        EmptyKeys.UserInterface.Themes.EmptyKeysTheme.CreateColorsAndBrushes();
                        EmptyKeys.UserInterface.Themes.CommonHelpers.CreateStyles(value);
                        EmptyKeys.UserInterface.Themes.CommonHelpers.CreateLocalizationResources(value);
                        ResourceDictionary.DefaultDictionary = null;
                    }
                }
            }
        }
    }

    class WcR2AudioDevice : AudioDevice
    {
        public override SoundBase CreateSound(object nativeSound)
        {
            return new WcR2Sound(nativeSound);
        }
    }

    class WcR2AssetManager : MonoGameAssetManager
    {
        public ContentManager DefaultContentManager { get; set; }

        public override FontBase LoadFont(object contentManager, string file)
        {
            var cm = (contentManager as WcR2ContentManager) ?? this.DefaultContentManager;
            if (cm != null)
            {
                var wcR2Font = cm.Load<IWcR2Font>(file);
                return Engine.Instance.Renderer.CreateFont(wcR2Font);
            }

            return base.LoadFont(contentManager, file);
        }

        /// <remarks>
        /// 用于<see cref="ImageManager.LoadImages"/>。
        /// </remarks>
        public override TextureBase LoadTexture(object contentManager, string file)
        {
            var cm = (contentManager as WcR2ContentManager) ?? this.DefaultContentManager;
            if (cm != null)
            {
                var texture = cm.Load<Texture2D>(file);
                if (texture != null)
                {
                    return Engine.Instance.Renderer.CreateTexture(texture);
                }
            }

            return base.LoadTexture(contentManager, file);
        }
    }
    #endregion

    #region Resources implementation
    class WcR2Font : FontBase
    {
        public WcR2Font(object nativeFont) : base(nativeFont)
        {
            this.NativeFont = nativeFont as IWcR2Font;
            if (this.NativeFont == null)
            {
                throw new ArgumentException("nativeFont not implements IWcR2Font.");
            }
        }

        public IWcR2Font NativeFont { get; private set; }

        public override char? DefaultCharacter
        {
            get { return null; }
        }

        public override FontEffectType EffectType
        {
            get { return FontEffectType.None; }
        }

        public override int LineSpacing
        {
            get { return (int)NativeFont.LineHeight; }
        }

        public override float Spacing
        {
            get { return 0; }
            set { throw new NotImplementedException(); }
        }

        public override object GetNativeFont()
        {
            return this.NativeFont;
        }

        public override Size MeasureString(StringBuilder text, float dpiScaleX, float dpiScaleY)
        {
            var size = NativeFont.MeasureString(text);
            return new Size(size.X, size.Y);
        }

        public override Size MeasureString(string text, float dpiScaleX, float dpiScaleY)
        {
            var size = NativeFont.MeasureString(text);
            return new Size(size.X, size.Y);
        }
    }

    class WcR2Sound : SoundBase
    {
        public WcR2Sound(object nativeSound) : base(nativeSound)
        {
            this.music = nativeSound as Music;
        }

        private Music music;

        public override SoundState State
        {
            get
            {
                switch (music.State)
                {
                    case Music.PlayState.Stopped: return SoundState.Stopped;
                    case Music.PlayState.Playing: return SoundState.Playing;
                    case Music.PlayState.Paused: return SoundState.Paused;
                    default: return SoundState.Stopped;
                }
            }
        }

        public override float Volume
        {
            get { return music.Volume; }
            set { music.Volume = value; }
        }

        public override void Pause()
        {
            music.Pause();
        }

        public override void Play()
        {
            music.Play();
        }

        public override void Stop()
        {
            music.Stop();
        }
    }
    #endregion

    class WcR2ContentManager : ContentManager
    {
        public WcR2ContentManager(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        private GraphicsDevice GraphicsDevice
        {
            get
            {
                return this.ServiceProvider.GetService<IGraphicsDeviceService>().GraphicsDevice;
            }
        }

        public bool UseD2DFont { get; set; }

        public override T Load<T>(string assetName)
        {
            if (assetName == "DirectionalBlurShader")
            {
                return default(T);
            }
            if (typeof(T) == typeof(IWcR2Font))
            {
                object value;
                if (!LoadedAssets.TryGetValue(assetName, out value))
                {
                    value = LoadXnaFont(assetName);
                    if (value != null)
                    {
                        LoadedAssets[assetName] = value;
                    }
                }
                return (T)value;
            }
            else if (typeof(T) == typeof(Texture2D))
            {
                object value;
                if (!LoadedAssets.TryGetValue(assetName, out value))
                {
                    var bitmap = MRes.ResourceManager.GetObject(assetName) as System.Drawing.Bitmap;
                    if (bitmap == null)
                    {
                        var obj = Res.ResourceManager.GetObject(assetName);
                        bitmap = Res.ResourceManager.GetObject(assetName) as System.Drawing.Bitmap;
                    }
                    if (bitmap != null)
                    {
                        value = bitmap.ToTexture(this.GraphicsDevice);
                    }
                    else //寻找wz
                    {
                        var png = PluginBase.PluginManager.FindWz(assetName).GetValueEx<Wz_Png>(null);
                        if (png != null)
                        {
                            value = png.ToTexture(this.GraphicsDevice);
                        }
                    }

                    if (value != null)
                    {
                        LoadedAssets[assetName] = value;
                    }
                }
                return (T)value;
            }
            return base.Load<T>(assetName);
        }

        private IWcR2Font LoadXnaFont(string assetName)
        {
            string[] fontDesc = assetName.Split(new[] { ',' }, 3);
            string familyName = fontDesc[0];
            float size;
            System.Drawing.FontStyle fStyle;
            if (float.TryParse(fontDesc[1], out size)
                && Enum.TryParse(fontDesc[2], out fStyle))
            {
                if (this.UseD2DFont)
                {
                    var d2dFont = new D2DFont(familyName, size,
                        (fStyle & System.Drawing.FontStyle.Bold) != 0,
                        (fStyle & System.Drawing.FontStyle.Italic) != 0
                        );
                    return new D2DFontAdapter(d2dFont);
                }
                else
                {
                    var baseFont = new System.Drawing.Font(familyName, size, fStyle, System.Drawing.GraphicsUnit.Pixel);
                    var xnaFont = new XnaFont(GraphicsDevice, baseFont);
                    return new XnaFontAdapter(xnaFont);
                }
            }
            else
            {
                return null;
            }
        }

        public override void Unload()
        {
            foreach (var kv in this.LoadedAssets)
            {
                IDisposable disposable = kv.Value as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            base.Unload();
        }
    }

    class ClipBoardService : IClipboardService
    {
        public string GetText()
        {
            var text = string.Empty;
            var thread = new Thread(() =>
            {
                text = System.Windows.Forms.Clipboard.GetText();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return text;
        }

        public void SetText(string text)
        {
            var thread = new Thread(() =>
            {
                System.Windows.Forms.Clipboard.SetText(text);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }

    static class Extensions
    {
        public static void AddFont(this FontManager fontManager, string familyName, float size, FontStyle style)
        {
            System.Drawing.FontStyle fStyle = System.Drawing.FontStyle.Regular;
            if ((style & FontStyle.Bold) != 0)
                fStyle |= System.Drawing.FontStyle.Bold;
            if ((style & FontStyle.Italic) != 0)
                fStyle |= System.Drawing.FontStyle.Italic;
            string assetName = MapRenderFonts.GetFontResourceKey(familyName, size, fStyle);
            fontManager.AddFont(familyName, size, style, assetName);
        }

        public static Rect ToRect(this Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static PointF ToPointF(this WzComparerR2.WzLib.Wz_Vector vector)
        {
            return vector == null ? new PointF() : new PointF(vector.X, vector.Y);
        }

        public static Size MeasureString(this FontBase font, string text, Size layoutSize)
        {
            var wcR2Font = (font.GetNativeFont() as IWcR2Font)?.BaseFont;
            if (wcR2Font != null)
            {
                if (wcR2Font is XnaFont)
                {
                    var xnaFont = (XnaFont)wcR2Font;
                    var size = xnaFont.MeasureString(text, new Vector2(layoutSize.Width, layoutSize.Height));
                    return new Size(size.X, size.Y);
                }
                else if (wcR2Font is D2DFont)
                {
                    var d2dFont = (D2DFont)wcR2Font;
                    var size = d2dFont.MeasureString(text, new Vector2(layoutSize.Width, layoutSize.Height));
                    return new Size(size.X, size.Y);
                }
            }
            return font.MeasureString(text, 1, 1);
        }
    }
}

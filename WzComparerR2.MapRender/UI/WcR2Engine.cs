using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Input;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using WzComparerR2.Rendering;
using WzComparerR2.MapRender;
using ContentManager = Microsoft.Xna.Framework.Content.ContentManager;
using Res = WzComparerR2.MapRender.Properties.Resources;

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
            InputManager.Current.FocusedElement = null;
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
        public override FontBase LoadFont(object contentManager, string file)
        {
            var cm = contentManager as WcR2ContentManager;
            if (cm != null)
            {
                var nativeFont = cm.Load<XnaFont>(file);
                return Engine.Instance.Renderer.CreateFont(nativeFont);
            }

            return base.LoadFont(contentManager, file);
        }

        /// <remarks>
        /// 用于<see cref="ImageManager.LoadImages"/>。
        /// </remarks>
        public override TextureBase LoadTexture(object contentManager, string file)
        {
            var cm = contentManager as WcR2ContentManager;
            if (cm != null)
            {
                var bitmap = Res.ResourceManager.GetObject(file) as System.Drawing.Bitmap;
                if (bitmap != null)
                {
                    return Engine.Instance.Renderer.CreateTexture(bitmap);
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
            this.NativeFont = nativeFont as XnaFont;
        }

        public XnaFont NativeFont { get; private set; }

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
            get { return NativeFont.Height; }
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
            var size = NativeFont.MeasureString(text, 0, text.Length);
            return new Size(size.X, size.Y);
        }

        public override Size MeasureString(string text, float dpiScaleX, float dpiScaleY)
        {
            var size = NativeFont.MeasureString(text, 0, text.Length);
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
                return ((IGraphicsDeviceService)this.ServiceProvider.GetService(typeof(IGraphicsDeviceService))).GraphicsDevice;
            }
        }

        public override T Load<T>(string assetName)
        {
            if (typeof(T) == typeof(XnaFont))
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
            return base.Load<T>(assetName);
        }

        private XnaFont LoadXnaFont(string assetName)
        {
            string[] fontDesc = assetName.Split(',');
            string familyName = fontDesc[0];
            float size;
            FontStyle style;
            if (float.TryParse(fontDesc[1], out size)
                && Enum.TryParse(fontDesc[2], out style))
            {
                System.Drawing.FontStyle fStyle = System.Drawing.FontStyle.Regular;
                switch (style)
                {
                    case FontStyle.Regular: fStyle = System.Drawing.FontStyle.Regular; break;
                    case FontStyle.Bold: fStyle = System.Drawing.FontStyle.Bold; break;
                    case FontStyle.Italic: fStyle = System.Drawing.FontStyle.Italic; break;
                }
                var baseFont = new System.Drawing.Font(familyName, size, fStyle, System.Drawing.GraphicsUnit.Pixel);
                return new XnaFont(GraphicsDevice, baseFont);
            }
            else
            {
                return null;
            }
        }
    }

    static class Extensions
    {
        public static void AddFont(this FontManager fontManager, string familyName, float size, FontStyle style)
        {
            string assetName = string.Join(",", familyName, size, style);
            fontManager.AddFont(familyName, size, style, assetName);
        }

        public static Rect ToRect(this Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}

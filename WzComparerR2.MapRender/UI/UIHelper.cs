using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using WzComparerR2.Rendering;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Input;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Data;
using Microsoft.Xna.Framework.Graphics;
using System.Globalization;

namespace WzComparerR2.MapRender.UI
{
    static class UIHelper
    {
        public static IDisposable RegisterClickEvent<T>(UIElement control, Func<UIElement, PointF, T> getItemFunc, Action<T> onClick)
        {
            var holder = new ClickEventHolder<T>(control)
            {
                GetItemFunc = getItemFunc,
                ClickFunc = onClick,
            };
            holder.Register();
            return holder;
        }

        public static TextureBase LoadTexture(Wz_Node node)
        {
            node = node?.GetLinkedSourceNode(PluginManager.FindWz);
            var png = node.GetValueEx<Wz_Png>(null);
            if (png != null)
            {
                return Engine.Instance.AssetManager.LoadTexture(null, node.FullPathToFile);
            }
            else
            {
                return null;
            }
        }

        public static HitMap CreateHitMap(Texture2D texture)
        {
            HitMap hitMap = null;
            byte[] colorData;
            bool[] rowHit;
            switch (texture.Format)
            {
                case SurfaceFormat.Color:
                case SurfaceFormat.Bgra32:
                    hitMap = new HitMap(texture.Width, texture.Height);
                    colorData = new byte[texture.Width * texture.Height * 4];
                    rowHit = new bool[texture.Width];
                    texture.GetData(colorData);
                    for (int y = 0; y < texture.Height; y++)
                    {
                        int rowStart = y * texture.Width * 4;
                        for (int i = 0; i < rowHit.Length; i++)
                        {
                            rowHit[i] = colorData[rowStart + i * 4 + 3] != 0;
                        }
                        hitMap.SetRow(y, rowHit);
                    }
                    break;

                case SurfaceFormat.Bgra4444:
                    hitMap = new HitMap(texture.Width, texture.Height);
                    colorData = new byte[texture.Width * texture.Height * 2];
                    rowHit = new bool[texture.Width];
                    texture.GetData(colorData);
                    for (int y = 0; y < texture.Height; y++)
                    {
                        int rowStart = y * texture.Width * 2;
                        for (int i = 0; i < rowHit.Length; i++)
                        {
                            rowHit[i] = colorData[rowStart + i * 2 + 1] >> 4 != 0;
                        }
                        hitMap.SetRow(y, rowHit);
                    }
                    break;

                default:
                    hitMap = new HitMap(true);
                    break;
            }
            return hitMap;
        }

        public static IValueConverter CreateConverter(Func<object, object> convertFunc)
        {
            return new CustomConverter(convertFunc);
        }

        public static IValueConverter CreateConverter<TIn, TOut>(Func<TIn, TOut> convertFunc)
        {
            var wrapFunc = new Func<object, object>(o =>
            {
                return (convertFunc != null && o is TIn) ? (object)convertFunc((TIn)o) : null;
            });
            return new CustomConverter(wrapFunc);
        }

        class ClickEventHolder<T> : IDisposable
        {
            public ClickEventHolder(UIElement control)
            {
                this.Control = control;
            }

            public UIElement Control { get; private set; }
            public Func<UIElement, PointF, T> GetItemFunc { get; set; }
            public Action<T> ClickFunc { get; set; }

            private T item;

            public void Register()
            {
                this.Control.MouseDown += this.OnMouseDown;
                this.Control.MouseUp += this.OnMouseUp;
            }

            public void Deregister()
            {
                this.Control.MouseDown -= this.OnMouseDown;
                this.Control.MouseUp -= this.OnMouseUp;
            }

            private void OnMouseDown(object sender, MouseButtonEventArgs e)
            {
                if (GetItemFunc != null && e.ChangedButton == EmptyKeys.UserInterface.Input.MouseButton.Left)
                {
                    this.item = GetItemFunc.Invoke(this.Control, e.GetPosition(this.Control));
                }
            }

            private void OnMouseUp(object sender, MouseButtonEventArgs e)
            {
                if (GetItemFunc != null && e.ChangedButton == EmptyKeys.UserInterface.Input.MouseButton.Left)
                {
                    T item = GetItemFunc.Invoke(this.Control, e.GetPosition(this.Control));
                    if (item != null && object.Equals(item, this.item))
                    {
                        this.ClickFunc?.Invoke(item);
                    }
                    this.item = default(T);
                }
            }

            void IDisposable.Dispose()
            {
                this.Deregister();
            }
        }

        class CustomConverter : IValueConverter
        {
            public CustomConverter(Func<object, object> convertFunc)
            {
                this.convertFunc = convertFunc;
            }

            private Func<object, object> convertFunc;

            public object Convert(object value, Type target, object parameter, CultureInfo culture)
            {
                return convertFunc?.Invoke(value);
            }

            public object ConvertBack(object value, Type target, object parameter, CultureInfo culture)
            {
                return convertFunc?.Invoke(value);
            }
        }
    }
}

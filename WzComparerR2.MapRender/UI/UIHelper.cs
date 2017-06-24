using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Input;

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
    }
}

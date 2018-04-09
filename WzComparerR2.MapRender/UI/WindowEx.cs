using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Input;

namespace WzComparerR2.MapRender.UI
{
    class WindowEx : Window
    {
        public WindowEx()
        {
            InitializeComponents();
        }

        protected virtual void InitializeComponents()
        {
            this.Template = new ControlTemplate(CreateControls);
        }

        private UIElement CreateControls(UIElement parent)
        {
            ContentPresenter p = new ContentPresenter();
            p.Parent = parent;
            p.SetBinding(ContentPresenter.ContentProperty, new Binding() { Source = this, SourceDependencyProperty = Window.ContentProperty });
            return p;
        }

        protected void SetDragTarget(UIElement element)
        {
            element.Name = "PART_WindowTitleBorder";
        }

        protected override void OnPropertyChanged(DependencyProperty property)
        {
            base.OnPropertyChanged(property);

            if (property == VisibilityProperty)
            {
                if (this.Visibility == Visibility.Visible)
                {
                    this.Focus();
                }
            }
        }

        protected override void OnGotFocus(object sender, RoutedEventArgs e)
        {
            base.OnGotFocus(sender, e);

            if (this.IsOnTop)
            {
                this.BringToFront();
            }
        }

        protected override void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(sender, e);

            if (this.IsOnTop)
            {
                this.Focus();
            }
        }

        public void Toggle()
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }

        public void BringToFront()
        {
            var root = this.Parent as UIRoot;
            if (root != null)
            {
                root.Windows.Remove(this);
                root.Windows.Add(this);
            }
        }

        public void Show()
        {
            this.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }
    }
}

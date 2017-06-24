using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Data;

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
            this.DataContext = this;
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

        public void Toggle()
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.Visibility = Visibility.Visible;
            }
        }
    }
}

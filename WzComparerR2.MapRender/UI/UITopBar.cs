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

namespace WzComparerR2.MapRender.UI
{
    class UITopBar : WindowEx
    {
        protected override void InitializeComponents()
        {
            Border border = new Border();
            border.Background = new SolidColorBrush(new ColorW(0, 0, 0, 192));
            this.Content = border;

            this.Height = 16;
            this.IsHitTestVisible = false;
            base.InitializeComponents();
        }
    }
}

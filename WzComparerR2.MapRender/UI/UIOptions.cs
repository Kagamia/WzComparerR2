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
    class UIOptions : WindowEx
    {
        public UIOptions()
        {
        }

        protected override void InitializeComponents()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(20) });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            this.Content = grid;

            Border border = new Border();
            border.Background = Brushes.White;
            grid.Children.Add(border);
            Grid.SetRow(border, 0);
            Grid.SetColumn(border, 0);
            this.SetDragTarget(border);


            Canvas panel = new Canvas();
            panel.Background = Brushes.Gray;
            grid.Children.Add(panel);
            Grid.SetRow(panel, 1);
            Grid.SetColumn(panel, 0);


            this.IsOnTop = true;
            this.Focusable = true;
            this.Width = 300;
            this.Height = 300;
            base.InitializeComponents();
        }
    }
}

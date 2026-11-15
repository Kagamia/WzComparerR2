using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.Animation;
using WzComparerR2.MapRender.Patches2;

namespace WzComparerR2.MapRender.UI
{
    class UISpineSelector : WindowEx
    {
        public UISpineSelector()
        {

        }

        private IReadOnlyList<BackItem> Back { get; set; }
        private IReadOnlyList<IReadOnlyList<ObjItem>> Obj { get; set; }
        private List<ComboBox> BackCmbs { get; set; }
        private List<List<ComboBox>> ObjCmbs { get; set; }
        private List<Button> buttons { get; set; }

        protected override void InitializeComponents()
        {
            this.ResetCmbs();

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.SetBinding(BackgroundProperty, new Binding(Control.BackgroundProperty) { Source = this });
            this.Content = grid;

            TextBlock title = new TextBlock();
            title.Text = "Spine Animation Selector";
            title.IsHitTestVisible = false;
            title.Foreground = Brushes.Gold;
            title.HorizontalAlignment = HorizontalAlignment.Center;
            title.VerticalAlignment = VerticalAlignment.Center;

            Border header = new Border();
            header.Child = title;
            grid.Children.Add(header);
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, 0);
            this.SetDragTarget(header);

            TabControl tabControl = new TabControl();
            tabControl.Resources.Add(typeof(TabItem), GetTabItemStyle());
            tabControl.Margin = new Thickness(5, 0, 5, 0);
            tabControl.TabStripPlacement = Dock.Left;
            grid.Children.Add(tabControl);
            Grid.SetRow(tabControl, 1);
            Grid.SetColumn(tabControl, 0);

            Button btnOK = new Button();
            btnOK.Width = 50;
            btnOK.Height = 20;
            btnOK.Margin = new Thickness(5);
            btnOK.Content = "OK";
            btnOK.Click += BtnOK_Click;

            Button btnReset = new Button();
            btnReset.Width = 50;
            btnReset.Height = 20;
            btnReset.Margin = new Thickness(5);
            btnReset.Content = "Reset";
            btnReset.Click += BtnReset_Click;

            Button btnCancel = new Button();
            btnCancel.Width = 50;
            btnCancel.Height = 20;
            btnCancel.Margin = new Thickness(5);
            btnCancel.Content = "Cancel";
            btnCancel.Click += BtnCancel_Click;

            StackPanel footerPanel = new StackPanel();
            footerPanel.HorizontalAlignment = HorizontalAlignment.Center;
            footerPanel.VerticalAlignment = VerticalAlignment.Center;
            footerPanel.Orientation = Orientation.Horizontal;
            footerPanel.Children.Add(btnOK);
            footerPanel.Children.Add(btnReset);
            footerPanel.Children.Add(btnCancel);
            this.buttons = new List<Button> { btnOK, btnReset, btnCancel };

            Border footer = new Border();
            footer.Child = footerPanel;
            grid.Children.Add(footer);
            Grid.SetRow(footer, 2);
            Grid.SetColumn(footer, 0);

            this.Width = 600;
            this.Height = 400;
            this.SetResourceReference(BackgroundProperty, MapRenderResourceKey.TooltipBrush);
            base.InitializeComponents();
        }

        public void LoadTabContents(IReadOnlyList<BackItem> back, IReadOnlyList<IReadOnlyList<ObjItem>> obj)
        {
            this.Back = back;
            this.Obj = obj;
            this.ResetCmbs();

            TabControl tabControl = (TabControl)(this.Content as Grid).Children.OfType<TabControl>().FirstOrDefault();
            tabControl.ItemsSource = null;

            List<TabItem> tabItems = new List<TabItem>();
            TabItem tab_back = new TabItem();
            tab_back.Header = "Back";
            tab_back.Content = this.GetTabContent(-1);
            tabItems.Add(tab_back);

            for (int i = 0; i <= 7; i++)
            {
                TabItem tab_i = new TabItem();
                tab_i.Header = $"{i}";
                tab_i.Content = this.GetTabContent(i);
                tabItems.Add(tab_i);
            }

            tabControl.ItemsSource = tabItems;
            tabControl.SelectedIndex = -1;
            tabControl.SelectedIndex = 0;

            this.EnableButtons();
        }

        public void ClearContents()
        {
            this.Hide();
            this.DisableButtons();

            this.Back = null;
            this.Obj = null;
            this.ResetCmbs();

            TabControl tabControl = (TabControl)(this.Content as Grid).Children.OfType<TabControl>().FirstOrDefault();
            tabControl.ItemsSource = null;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            this.ApplyAnimation();
            this.Hide();
            this.DisableButtons();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            this.DisableButtons();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Back.Count; i++)
            {
                var animator = Back[i].View.Animator as ISpineAnimator;
                var spineName = Back[i].SpineAni;
                if (spineName != null)
                    animator.SelectedAnimationName = spineName;
                else
                    animator.SelectedAnimationIndex = -1;
            }
            for (int layer = 0; layer <= 7; layer++)
            {
                for (int i = 0; i < Obj[layer].Count; i++)
                {
                    var animator = Obj[layer][i].View.Animator as ISpineAnimator;
                    var spineName = Obj[layer][i].SpineAni;
                    if (spineName != null)
                        animator.SelectedAnimationName = spineName;
                    else
                        animator.SelectedAnimationIndex = -1;
                }
            }

            this.Hide();
            this.DisableButtons();
        }

        private UIElement GetTabContent(int i)
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(350, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            int row = 0;
            if (i == -1)
            {
                foreach (var item in Back)
                {
                    grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40, GridUnitType.Pixel) });
                    TextBlock lbl = new TextBlock();
                    lbl.VerticalAlignment = VerticalAlignment.Center;
                    lbl.Text = $"[{item.Name}]\n{item.BS}/{item.No}";
                    lbl.Foreground = Brushes.Yellow;
                    Grid.SetRow(lbl, row);
                    Grid.SetColumn(lbl, 0);
                    grid.Children.Add(lbl);

                    ComboBox cmb = new ComboBox();
                    var src = new List<string>() { "" };
                    src.AddRange((item.View.Animator as ISpineAnimator).Animations);
                    cmb.ItemsSource = src;
                    cmb.SelectedIndex = (item.View.Animator as ISpineAnimator).SelectedAnimationIndex + 1;
                    cmb.MinWidth = 100;
                    cmb.Height = 24;
                    Grid.SetRow(cmb, row++);
                    Grid.SetColumn(cmb, 1);
                    grid.Children.Add(cmb);

                    this.BackCmbs.Add(cmb);
                }
            }
            else
            {
                var filteredObj = Obj[i];
                foreach (var item in filteredObj)
                {
                    grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40, GridUnitType.Pixel) });
                    TextBlock lbl = new TextBlock();
                    lbl.VerticalAlignment = VerticalAlignment.Center;
                    lbl.Text = $"[{item.Name}]\n{item.OS}/{item.L0}/{item.L1}/{item.L2}";
                    lbl.Foreground = Brushes.Yellow;
                    Grid.SetRow(lbl, row);
                    Grid.SetColumn(lbl, 0);
                    grid.Children.Add(lbl);

                    ComboBox cmb = new ComboBox();
                    var src = new List<string>() { "" };
                    src.AddRange((item.View.Animator as ISpineAnimator).Animations);
                    cmb.ItemsSource = src;
                    cmb.SelectedIndex = (item.View.Animator as ISpineAnimator).SelectedAnimationIndex + 1;
                    cmb.MinWidth = 100;
                    cmb.Height = 24;
                    Grid.SetRow(cmb, row++);
                    Grid.SetColumn(cmb, 1);
                    grid.Children.Add(cmb);

                    this.ObjCmbs[i].Add(cmb);
                }
            }

            ScrollViewer viewer = new ScrollViewer();
            viewer.Content = grid;
            return viewer;
        }

        private void ApplyAnimation()
        {
            for (int i = 0; i < Back.Count; i++)
            {
                var animator = Back[i].View.Animator as ISpineAnimator;
                var cmb = BackCmbs[i];
                animator.SelectedAnimationIndex = cmb.SelectedIndex - 1;
            }
            for (int layer = 0; layer <= 7; layer++)
            {
                for (int i = 0; i < Obj[layer].Count; i++)
                {
                    var animator = Obj[layer][i].View.Animator as ISpineAnimator;
                    var cmb = ObjCmbs[layer][i];
                    animator.SelectedAnimationIndex = cmb.SelectedIndex - 1;
                }
            }
        }

        private void ResetCmbs()
        {
            this.BackCmbs = new List<ComboBox>();

            this.ObjCmbs = new List<List<ComboBox>>();
            for (int i = 0; i <= 7; i++)
            {
                this.ObjCmbs.Add(new List<ComboBox>());
            }
        }

        private void DisableButtons()
        {
            foreach (var button in buttons)
            {
                button.IsEnabled = false;
            }
        }

        private void EnableButtons()
        {
            foreach (var button in buttons)
            {
                button.IsEnabled = true;
            }
        }

        private Style GetTabItemStyle()
        {
            var style = EmptyKeys.UserInterface.Themes.TabControlStyle.CreateTabItemStyle();
            var templateSetter = style.Setters.FirstOrDefault(s => s.Property == Control.TemplateProperty);
            if (templateSetter != null)
            {
                var oldTemplate = templateSetter.Value as ControlTemplate;
                var funcType = typeof(Func<UIElement, UIElement>);
                var funcField = oldTemplate.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(field => field.FieldType == funcType);
                var oldMethod = funcField?.GetValue(oldTemplate) as Func<UIElement, UIElement>;
                if (oldMethod != null)
                {
                    var newMethod = new Func<UIElement, UIElement>(parent =>
                    {
                        UIElement elem = oldMethod(parent);
                        ContentPresenter presenter = (elem as Border)?.Child as ContentPresenter;
                        if (presenter != null)
                        {
                            presenter.Margin = new Thickness(6, 1, 6, 1);
                        }
                        return elem;
                    });
                    funcField.SetValue(oldTemplate, newMethod);
                }
            }

            return style;
        }
    }
}

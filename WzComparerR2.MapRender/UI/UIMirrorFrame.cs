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

    class UIMirrorFrame : WindowEx
    {
        private BitmapImage mirrorFrame800;
        private BitmapImage mirrorFrame1024;
        private BitmapImage mirrorFrame1366;

        protected override void InitializeComponents()
        {
            mirrorFrame800 = new BitmapImage() { Texture = Engine.Instance.AssetManager.LoadTexture(null, nameof(Properties.Resources.UIWindow3_img_mirrorFrame_800)) };
            mirrorFrame1024 = new BitmapImage() { Texture = Engine.Instance.AssetManager.LoadTexture(null, nameof(Properties.Resources.UIWindow3_img_mirrorFrame_1024)) };
            mirrorFrame1366 = new BitmapImage() { Texture = Engine.Instance.AssetManager.LoadTexture(null, nameof(Properties.Resources.UIWindow3_img_mirrorFrame_1366)) };

            Image image = new Image();
            image.SetBinding(Image.WidthProperty, new Binding(UIMirrorFrame.WidthProperty) { Source = this });
            image.SetBinding(Image.HeightProperty, new Binding(UIMirrorFrame.HeightProperty) { Source = this });
            image.SetBinding(Image.SourceProperty, new Binding(UIMirrorFrame.WidthProperty) { Source = this, Converter = UIHelper.CreateConverter((float w) => w == 800 ? mirrorFrame800 : (w == 1024 ? mirrorFrame1024 : (w == 1366 ? mirrorFrame1366 : null))) });
            this.Content = image;

            this.IsHitTestVisible = false;
            base.InitializeComponents();
        }
    }
}

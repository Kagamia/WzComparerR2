using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Themes;
using EmptyKeys.UserInterface.Media.Imaging;
using MRes = WzComparerR2.MapRender.Properties.Resources;

namespace WzComparerR2.MapRender.UI
{
    static class MapRenderButtonStyle
    {
        public static Style CreateMapRenderButtonStyle()
        {
            var style = ImageButtonStyle.CreateImageButtonStyle();

            //btnOK
            var trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "OK"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("OK"));
            style.Triggers.Add(trigger);
            //btnYes
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "Yes"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("Yes"));
            style.Triggers.Add(trigger);
            //btnNo
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "No"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("No"));
            style.Triggers.Add(trigger);
            //btnCancel
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "Cancel"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("Cancel"));
            style.Triggers.Add(trigger);
            //btnClose
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "Close"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("Close"));
            style.Triggers.Add(trigger);

            return style;
        }

        public static Setter[] GetMapRenderButtonSetters(string name)
        {
            switch (name)
            {
                case "OK":
                case "Yes":
                    return CreateButtonSetters(42, 16,
                        nameof(MRes.Basic_img_BtOK4_normal_0),
                        nameof(MRes.Basic_img_BtOK4_mouseOver_0),
                        nameof(MRes.Basic_img_BtOK4_pressed_0),
                        nameof(MRes.Basic_img_BtOK4_disabled_0));

                case "No":
                    return CreateButtonSetters(42, 16,
                        nameof(MRes.Basic_img_BtNo3_normal_0),
                        nameof(MRes.Basic_img_BtNo3_mouseOver_0),
                        nameof(MRes.Basic_img_BtNo3_pressed_0),
                        nameof(MRes.Basic_img_BtNo3_disabled_0));

                case "Cancel":
                    return CreateButtonSetters(42, 16,
                        nameof(MRes.Basic_img_BtCancel4_normal_0),
                        nameof(MRes.Basic_img_BtCancel4_mouseOver_0),
                        nameof(MRes.Basic_img_BtCancel4_pressed_0),
                        nameof(MRes.Basic_img_BtCancel4_disabled_0));

                case "Close":
                    return CreateButtonSetters(13, 13,
                         nameof(MRes.Basic_img_BtClose3_normal_0),
                         nameof(MRes.Basic_img_BtClose3_mouseOver_0),
                         nameof(MRes.Basic_img_BtClose3_pressed_0),
                         nameof(MRes.Basic_img_BtClose3_disabled_0));

                default:
                    return null;
            }
        }

        private static Setter[] CreateButtonSetters(float width, float height,
            string normalAsset, string mouseOverAsset, string pressedAsset, string disabledAsset)
        {
            return new Setter[]
            {
                new Setter(UIElement.WidthProperty, width),
                new Setter(UIElement.HeightProperty, height),
                new Setter(ImageButton.ImageNormalProperty, new BitmapImage(){ TextureAsset=normalAsset }),
                new Setter(ImageButton.ImageHoverProperty, new BitmapImage(){ TextureAsset=mouseOverAsset }),
                new Setter(ImageButton.ImagePressedProperty, new BitmapImage(){ TextureAsset=pressedAsset }),
                new Setter(ImageButton.ImageDisabledProperty, new BitmapImage(){ TextureAsset=disabledAsset }),
            };
        }
    }
}

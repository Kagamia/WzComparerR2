using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;

namespace WzComparerR2.Avatar
{
    public class Entry : PluginEntry
    {
        public Entry(PluginContext context)
            : base(context)
        {
        }

        private ButtonItem btnSetting;
        private ComboBoxItem cmbAction;

        protected override void OnLoad()
        {
            var bar = this.Context.AddRibbonBar("Modules", "Avatar");
            ItemContainer container1 = new ItemContainer();
            container1.Orientation = eOrientation.Vertical;
            bar.Items.Add(container1);

            ItemContainer container1_1 = new ItemContainer();
            container1_1.Orientation = eOrientation.Horizontal;

            cmbAction = new ComboBoxItem();
            cmbAction.ComboWidth = 150;
            cmbAction.DropDownWidth = 200;
            cmbAction.DropDownHeight = 150;
            container1_1.SubItems.Add(cmbAction);

            bar.Items.Add(container1_1);



            ItemContainer container1_2 = new ItemContainer();
            container1_2.Orientation = eOrientation.Horizontal;

            btnSetting = new ButtonItem("", "纸娃娃");
            btnSetting.Click += btnSetting_Click;
            container1_2.SubItems.Add(btnSetting);

            bar.Items.Add(container1_2);
        }

        void btnSetting_Click(object sender, EventArgs e)
        {
            AvatarCanvas canvas = new AvatarCanvas();
            canvas.LoadZ();
            canvas.LoadActions();
            canvas.LoadEmotions();
            
            cmbAction.Items.Clear();
            foreach (var action in canvas.Actions)
            {
                ComboItem cmbItem = new ComboItem(action.Name);
                switch (action.Level)
                {
                    case 0:
                        cmbItem.FontStyle = System.Drawing.FontStyle.Bold;
                        cmbItem.ForeColor = Color.Indigo;
                        break;

                    case 1:
                        cmbItem.ForeColor = Color.Indigo;
                        break;
                }
                cmbAction.Items.Add(cmbItem);
            }

            canvas.ActionName = "stand1";
            canvas.EmotionName = "default";
            canvas.TamingActionName = "stand1";
            AddPart(canvas, "Character\\00002000.img");
            AddPart(canvas, "Character\\00012000.img");
            AddPart(canvas, "Character\\Face\\00020000.img");
            AddPart(canvas, "Character\\Hair\\00030000.img");
            var bone = canvas.CreateFrame(0, 0, 0);
            var bo = canvas.Render(bone);
            bo.Bitmap.Save("D:\\b.png");
        }

        void AddPart(AvatarCanvas canvas, string imgPath)
        {
            Wz_Node imgNode = PluginManager.FindWz(imgPath);
            if (imgNode != null)
            {
                canvas.AddPart(imgNode);
            }
        }
    }
}

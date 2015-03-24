using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;

namespace WzComparerR2
{
    public partial class FrmGifSetting : DevComponents.DotNetBar.Office2007Form
    {

        public FrmGifSetting()
        {
            InitializeComponent();
            initSelection();
        }

        private void initSelection()
        {
            comboBoxEx1.SelectedIndex = 0;
        }

        public Color SelectedColor
        {
            get
            {
                if (checkBoxX1.Checked)
                {
                    return Color.FromArgb(0x00, this.colorPickerButton1.SelectedColor);
                }
                else
                {
                    return Color.FromArgb(0xff, this.colorPickerButton1.SelectedColor);
                }
            }
            set
            {
                checkBoxX1.Checked = (value.A != 0xff);
                this.colorPickerButton1.SelectedColor = Color.FromArgb(0xff, value);
            }
        }

        public int MinAlphaMixed
        {
            get
            {
                return slider1.Value;
            }
            set
            {
                value = Math.Max(slider1.Minimum, Math.Min(slider1.Maximum, value));
                slider1.Value = value;
            }
        }

        public int SelectedEncoder
        {
            get
            {
                return comboBoxEx1.SelectedIndex;
            }
            set
            {
                value = Math.Max(0, Math.Min(comboBoxEx1.Items.Count - 1, value));
                comboBoxEx1.SelectedIndex = value;
            }
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonX2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void slider1_ValueChanged(object sender, EventArgs e)
        {
            slider1.Text = slider1.Value.ToString();
        }
    }
}
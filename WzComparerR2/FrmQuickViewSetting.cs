using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.Editors;

namespace WzComparerR2
{
    public partial class FrmQuickViewSetting : DevComponents.DotNetBar.Office2007Form
    {
        public FrmQuickViewSetting()
        {
            InitializeComponent();
            this.comboBoxEx1.SelectedIndex = 0;
            this.comboBoxEx2.SelectedIndex = 0;
        }

        public bool SkillShowID
        {
            get { return checkBoxX1.Checked; }
            set { checkBoxX1.Checked = value; }
        }

        public bool SkillShowActionDelay
        {
            get { return checkBoxX2.Checked; }
            set { checkBoxX2.Checked = value; }
        }

        public DefaultLevel SkillDefaultLevel
        {
            get { return (DefaultLevel)comboBoxEx1.SelectedIndex; }
            set { comboBoxEx1.SelectedIndex = (int)value; }
        }

        public int SkillLevelInterval
        {
            get { return Convert.ToInt32(((ComboItem)comboBoxEx2.SelectedItem).Text); }
            set
            {
                for (int i = 0; i < comboBoxEx2.Items.Count; i++)
                {
                    if (value <= Convert.ToInt32(((ComboItem)comboBoxEx2.Items[i]).Text))
                    {
                        comboBoxEx2.SelectedIndex = i;
                        return;
                    }
                }
                comboBoxEx2.SelectedIndex = comboBoxEx2.Items.Count - 1;
            }
        }

        public bool GearShowID
        {
            get { return checkBoxX3.Checked; }
            set { checkBoxX3.Checked = value; }
        }

        public bool GearShowWeaponSpeed
        {
            get { return checkBoxX4.Checked; }
            set { checkBoxX4.Checked = value; }
        }

        public bool ItemShowID
        {
            get { return checkBoxX5.Checked; }
            set { checkBoxX5.Checked = value; }
        }

        public bool GearShowLevelOrSealed
        {
            get { return checkBoxX6.Checked; }
            set { checkBoxX6.Checked = value; }
        }

        public bool RecipeShowID
        {
            get { return checkBoxX7.Checked; }
            set { checkBoxX7.Checked = value; }
        }

        public bool ItemLinkRecipeInfo
        {
            get { return checkBoxX8.Checked; }
            set { checkBoxX8.Checked = value; }
        }

        public bool ItemLinkRecipeItem
        {
            get { return checkBoxX9.Checked; }
            set { checkBoxX9.Checked = value; }
        }
    }

    public enum DefaultLevel
    {
        Level0 = 0,
        Level1 = 1,
        LevelMax = 2,
        LevelMaxWithCO = 3,
    }
}

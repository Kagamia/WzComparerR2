using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using DevComponents.Editors;
using WzComparerR2.Config;


namespace WzComparerR2
{
    public partial class FrmQuickViewSetting : DevComponents.DotNetBar.Office2007Form
    {
        public FrmQuickViewSetting()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font(new FontFamily("Microsoft Sans Serif"), 8f);
#endif
            this.comboBoxEx1.SelectedIndex = 0;
            this.comboBoxEx2.SelectedIndex = 0;
        }

        [Link]
        public bool Skill_ShowProperties
        {
            get { return checkBoxX10.Checked; }
            set { checkBoxX10.Checked = value; }
        }

        [Link]
        public bool Skill_ShowID
        {
            get { return checkBoxX1.Checked; }
            set { checkBoxX1.Checked = value; }
        }

        [Link]
        public bool Skill_ShowDelay
        {
            get { return checkBoxX2.Checked; }
            set { checkBoxX2.Checked = value; }
        }

        [Link]
        public DefaultLevel Skill_DefaultLevel
        {
            get { return (DefaultLevel)comboBoxEx1.SelectedIndex; }
            set { comboBoxEx1.SelectedIndex = (int)value; }
        }

        [Link]
        public int Skill_IntervalLevel
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

        [Link]
        public bool Skill_DisplayCooltimeMSAsSec
        {
            get { return checkBoxX13.Checked; }
            set { checkBoxX13.Checked = value; }
        }

        [Link]
        public bool Skill_DisplayPermyriadAsPercent
        {
            get { return checkBoxX14.Checked; }
            set { checkBoxX14.Checked = value; }
        }

        [Link]
        public bool Skill_IgnoreEvalError
        {
            get { return checkBoxX15.Checked; }
            set { checkBoxX15.Checked = value; }
        }

        [Link]
        public bool Gear_ShowID
        {
            get { return checkBoxX3.Checked; }
            set { checkBoxX3.Checked = value; }
        }

        [Link]
        public bool Gear_ShowWeaponSpeed
        {
            get { return checkBoxX4.Checked; }
            set { checkBoxX4.Checked = value; }
        }

        [Link]
        public bool Item_ShowID
        {
            get { return checkBoxX5.Checked; }
            set { checkBoxX5.Checked = value; }
        }

        [Link]
        public bool Gear_ShowLevelOrSealed
        {
            get { return checkBoxX6.Checked; }
            set { checkBoxX6.Checked = value; }
        }

        [Link]
        public bool Gear_ShowMedalTag
        {
            get { return checkBoxX11.Checked; }
            set { checkBoxX11.Checked = value; }
        }


        [Link]
        public bool Recipe_ShowID
        {
            get { return checkBoxX7.Checked; }
            set { checkBoxX7.Checked = value; }
        }

        [Link]
        public bool Item_LinkRecipeInfo
        {
            get { return checkBoxX8.Checked; }
            set { checkBoxX8.Checked = value; }
        }

        [Link]
        public bool Item_LinkRecipeItem
        {
            get { return checkBoxX9.Checked; }
            set { checkBoxX9.Checked = value; }
        }

        [Link]
        public bool Item_ShowNickTag
        {
            get { return checkBoxX12.Checked; }
            set { checkBoxX12.Checked = value; }
        }

        public void Load(CharaSimConfig config)
        {
            var linkProp = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetCustomAttributes(typeof(LinkAttribute), false).Length > 0);

            foreach (var prop in linkProp)
            {
                string[] path = prop.Name.Split('_');
                try
                {
                    var configGroup = config.GetType().GetProperty(path[0]).GetValue(config, null);
                    var configPropInfo = configGroup.GetType().GetProperty(path[1]);
                    var value = configPropInfo.GetGetMethod().Invoke(configGroup, null);
                    prop.GetSetMethod().Invoke(this, new object[] { value });
                }
                catch { }
            }
        }

        public void Save(CharaSimConfig config)
        {
            var linkProp = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetCustomAttributes(typeof(LinkAttribute), false).Length > 0);

            foreach (var prop in linkProp)
            {
                string[] path = prop.Name.Split('_');
                try
                {
                    var configGroup = config.GetType().GetProperty(path[0]).GetValue(config, null);
                    var configPropInfo = configGroup.GetType().GetProperty(path[1]);
                    var value = prop.GetGetMethod().Invoke(this, null);
                    configPropInfo.GetSetMethod().Invoke(configGroup, new object[] { value });
                }
                catch { }
            }
        }

        private sealed class LinkAttribute : Attribute
        {
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

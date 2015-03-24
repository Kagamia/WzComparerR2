using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using DevComponents.DotNetBar;

namespace WzComparerR2.PluginBase
{
    public class PluginContext
    {
        internal PluginContext(PluginContextProvider contextProvider)
        {
            this.contextProvider = contextProvider;
        }

        private PluginContextProvider contextProvider;

        public Form MainForm
        {
            get { return this.contextProvider.MainForm; }
        }

        public Wz_Node SelectedNode1
        {
            get { return this.contextProvider.SelectedNode1; }
        }

        public Wz_Node SelectedNode2
        {
            get { return this.contextProvider.SelectedNode2; }
        }

        public Wz_Node SelectedNode3
        {
            get { return this.contextProvider.SelectedNode3; }
        }

        public StringLinker DefaultStringLinker
        {
            get { return this.contextProvider.DefaultStringLinker; }
        }

        public void AddRibbonBar(string tabName, RibbonBar ribbonBar)
        {
            RibbonControl ribbonCtrl = null;
            foreach (Control ctrl in this.MainForm.Controls)
            {
                if (ctrl is RibbonControl)
                {
                    ribbonCtrl = (RibbonControl)ctrl;
                    break;
                }
            }

            if (ribbonCtrl == null)
            {
                throw new Exception("无法找到RibbonContainer。");
            }

            RibbonPanel ribbonPanel = null;
            RibbonTabItem tabItem;
            foreach (BaseItem item in ribbonCtrl.Items)
            {
                if ((tabItem = item as RibbonTabItem) != null
                    && string.Equals(Convert.ToString(tabItem.Tag), tabName, StringComparison.CurrentCultureIgnoreCase))
                {
                    ribbonPanel = tabItem.Panel;
                    break;
                }
            }

            if (ribbonPanel == null)
            {
                throw new Exception("无法找到RibbonPanel。");
            }

            Control lastBar = ribbonPanel.Controls[0];
            ribbonBar.Location = new System.Drawing.Point(lastBar.Location.X + lastBar.Width, lastBar.Location.Y);
            ribbonBar.Size = new System.Drawing.Size(Math.Max(1,ribbonBar.Width) , lastBar.Height);
            ribbonPanel.SuspendLayout();
            ribbonPanel.Controls.Add(ribbonBar);
            ribbonPanel.Controls.SetChildIndex(ribbonBar, 0);
            ribbonPanel.ResumeLayout(false);
        }

        public RibbonBar AddRibbonBar(string tabName, string barText)
        {
            RibbonBar bar = new RibbonBar();
            bar.Text = barText;
            AddRibbonBar(tabName, bar);
            return bar;
        }
    }
}

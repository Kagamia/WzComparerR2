using System;
using System.Collections.Generic;
using System.Text;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using System.Windows.Forms;

namespace WzComparerR2.MonsterCard
{
    public class Entry : PluginEntry
    {
        public Entry(PluginContext context)
            : base(context)
        {
        }

        protected override void OnLoad()
        {
            var f = new UI.MonsterCardForm();
            f.PluginEntry = this;
            var tabCtrl = f.GetTabPanel();
            Context.AddTab(f.Text, tabCtrl);
            Context.SelectedNode1Changed += f.OnSelectedNode1Changed;
            Context.WzClosing += f.OnWzClosing;
            this.Tab = tabCtrl.TabItem;
            this.form = f;
            Context.MainForm.Resize += MainForm_ResizeEnd;
        }

        
        public SuperTabItem Tab { get; private set; }

        private FormWindowState state;
        private UI.MonsterCardForm form;

        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            Form mainForm = sender as Form;
            if (mainForm != null && mainForm.WindowState != state)
            {
                state = mainForm.WindowState;
                if (state != FormWindowState.Minimized)
                {
                    this.form.OnResize();
                }
            }
        }
    }
}

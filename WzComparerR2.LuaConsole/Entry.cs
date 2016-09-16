using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.PluginBase;
using DevComponents.DotNetBar;

namespace WzComparerR2.LuaConsole
{
    public class Entry : PluginEntry
    {
        public Entry(PluginContext context)
            : base(context)
        {
            Instance = this;
        }

        internal static Entry Instance { get; private set; }

        protected override void OnLoad()
        {
            var bar = this.Context.AddRibbonBar("Tools", "控制台");
            ButtonItem btnItem = new ButtonItem("", "Lua控制台");

            btnItem.Click += btnItem_Click;
            bar.Items.Add(btnItem);
        }

        FrmConsole frm;

        void btnItem_Click(object sender, EventArgs e)
        {
            if (frm == null || frm.IsDisposed)
            {
                frm = new FrmConsole();
                frm.Owner = Context.MainForm;
                
            }
            frm.Show();
            frm.Focus();
        }

        protected override void OnUnload()
        {
            base.OnUnload();
        }
    }
}
